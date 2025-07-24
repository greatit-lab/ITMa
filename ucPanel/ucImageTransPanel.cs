//ucPanel\ucImageTransPanel.cs
using ITM_Agent.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ITM_Agent.ucPanel
{
    public partial class ucImageTransPanel : UserControl
    {
        private static readonly HashSet<string> mergedBaseNames = new HashSet<string>();  // 중복 병합 방지
        private readonly LogManager logManager;
        private readonly PdfMergeManager pdfMergeManager;
        private readonly SettingsManager settingsManager;
        private readonly ucConfigurationPanel configPanel;

        // Folder 감시용
        private FileSystemWatcher imageWatcher;

        // “_#1_”이 없는 파일 중, 이름 끝이 "_숫자"인 파일을 감지 후 대기 시간을 두고 PDF 병합
        private readonly Dictionary<string, DateTime> changedFiles = new Dictionary<string, DateTime>();
        private readonly object changedFilesLock = new object();
        private System.Threading.Timer checkTimer;

        // 실행 중 여부 (MainForm의 btn_Run으로 제어)
        private bool isRunning = false;

        public ucImageTransPanel(SettingsManager settingsManager, ucConfigurationPanel configPanel)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.configPanel = configPanel ?? throw new ArgumentNullException(nameof(configPanel));
            InitializeComponent();

            logManager = new LogManager(AppDomain.CurrentDomain.BaseDirectory); 
            pdfMergeManager = new PdfMergeManager(AppDomain.CurrentDomain.BaseDirectory, logManager);

            logManager.LogEvent("[ucImageTransPanel] Initialized");

            // 기존 이벤트
            btn_SetFolder.Click += btn_SetFolder_Click;
            btn_FolderClear.Click += btn_FolderClear_Click;
            btn_SetTime.Click += btn_SetTime_Click;
            btn_TimeClear.Click += btn_TimeClear_Click;
            btn_SelectOutputFolder.Click += btn_SelectOutputFolder_Click;

            // UI 초기화
            LoadFolders();
            LoadRegexFolderPaths();
            LoadWaitTimes();
            LoadOutputFolder();
        }

        #region ====== MainForm에서 실행/중지 제어 ======

        public void UpdateStatusOnRun(bool runState)
        {
            isRunning = runState;

            // UI 제어
            btn_SetFolder.Enabled = !runState;
            btn_FolderClear.Enabled = !runState;
            btn_SetTime.Enabled = !runState;
            btn_TimeClear.Enabled = !runState;
            btn_SelectOutputFolder.Enabled = !runState;
            cb_TargetImageFolder.Enabled = !runState;
            cb_WaitTime.Enabled = !runState;

            // 감시 시작/중지
            if (isRunning)
            {
                StartWatchingFolder();
            }
            else
            {
                StopWatchingFolder();
            }

            logManager.LogEvent($"[ucImageTransPanel] Status updated to {(runState ? "Running" : "Stopped")}");
        }

        private void StartWatchingFolder()
        {
            StopWatchingFolder(); // 중복 방지

            // 설정된 폴더 가져오기
            string targetFolder = settingsManager.GetValueFromSection("ImageTrans", "Target");
            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
            {
                logManager.LogError("[ucImageTransPanel] Target folder not set or does not exist - cannot watch.");
                return;
            }

            imageWatcher = new FileSystemWatcher()
            {
                Path = targetFolder,
                Filter = "*.*", 
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                IncludeSubdirectories = false
            };

            imageWatcher.Renamed += OnImageFileChanged;
            imageWatcher.Changed += OnImageFileChanged;
            imageWatcher.Created += OnImageFileChanged;

            imageWatcher.EnableRaisingEvents = true;

            logManager.LogEvent($"[ucImageTransPanel] StartWatchingFolder - Folder: {targetFolder}");
        }

        private void StopWatchingFolder()
        {
            if (imageWatcher != null)
            {
                imageWatcher.EnableRaisingEvents = false;
                imageWatcher.Dispose();
                imageWatcher = null;
            }

            checkTimer?.Dispose();
            checkTimer = null;

            lock (changedFilesLock)
            {
                changedFiles.Clear();
            }
        }

        #endregion

        #region ====== FileSystemWatcher 이벤트 + Timer ======

        /// <summary>
        /// 파일 변경 이벤트에서 “_#1_ 이 없는지”를 확인 후, 이름 끝이 "_숫자"인지 체크
        /// </summary>
        private void OnImageFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!isRunning) return;
            
            // 폴더 / 임시파일 등 스킵
            if (!File.Exists(e.FullPath)) return;
            
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(e.FullPath);
            
            // 1) _#1_ 이 들어있으면 무시
            if (fileNameWithoutExt.Contains("_#1_"))
            {
                if (settingsManager.IsDebugMode)
                {
                    logManager.LogDebug(
                        $"[ucImageTransPanel] Skip file (contains _#1_): {e.FullPath}"
                    );
                }
                return;
            }

            // 2) 이름 끝에 "_숫자" 형태가 있는지 체크
            //    예) ABC_1, ABC_2, ABC_10 ...
            //    ^(?<basename>.+)_(?<page>\d+)$ 패턴
            Regex pattern = new Regex(@"^(?<basename>.+)_(?<page>\d+)$");
            var match = pattern.Match(fileNameWithoutExt);
            if (!match.Success)
            {
                // 패턴 미일치 → 무시
                return;
            }

            // 매칭된 파일만 Dictionary 기록
            lock (changedFilesLock)
            {
                changedFiles[e.FullPath] = DateTime.Now;
            }

            if (checkTimer == null)
            {
                checkTimer = new System.Threading.Timer(_ => CheckFilesAfterWait(), null, 1000, 1000);
            }

            logManager.LogEvent($"[ucImageTransPanel] Detected valid file: {e.FullPath}");
        }

        private void CheckFilesAfterWait()
        {
            if (!isRunning) return;

            int waitSec = GetWaitSeconds();
            if (waitSec <= 0) return;

            var now = DateTime.Now;
            var toProcess = new List<string>();

            lock (changedFilesLock)
            {
                var snapshot = changedFiles.ToList();
                foreach (var kv in snapshot)
                {
                    double diff = (now - kv.Value).TotalSeconds;
                    if (diff >= waitSec)
                    {
                        // 충분히 대기 시간이 지난 파일
                        toProcess.Add(kv.Key);
                    }
                }
            }

            if (toProcess.Count > 0)
            {
                foreach (var filePath in toProcess)
                {
                    try
                    {
                        MergeImagesForBaseName(filePath);
                    }
                    catch (Exception ex)
                    {
                        logManager.LogError($"[ucImageTransPanel] Merge error for file {filePath}: {ex.Message}");
                    }
                }

                // 처리 후 Dictionary에서 제거
                lock (changedFilesLock)
                {
                    foreach (var fp in toProcess)
                    {
                        changedFiles.Remove(fp);
                    }

                    if (changedFiles.Count == 0 && checkTimer != null)
                    {
                        checkTimer.Dispose();
                        checkTimer = null;
                    }
                }
            }
        }

        private int GetWaitSeconds()
        {
            // cb_WaitTime 또는 settingsManager "ImageTrans/Wait" 설정값
            string waitStr = settingsManager.GetValueFromSection("ImageTrans", "Wait");
            if (cb_WaitTime.InvokeRequired)
            {
                cb_WaitTime.Invoke(new MethodInvoker(delegate
                {
                    if (cb_WaitTime.SelectedItem is string sel)
                    {
                        waitStr = sel;
                    }
                }));
            }
            else
            {
                if (cb_WaitTime.SelectedItem is string sel)
                {
                    waitStr = sel;
                }
            }
        
            if (int.TryParse(waitStr, out int ws))
            {
                return ws;
            }
            return 30; // 기본 30초
        }
        #endregion

        private void MergeImagesForBaseName(string filePath)
        {
            // 0) baseName 파싱 (파일이 이미 삭제돼도 이름만으로 가능)
            string fnNoExt = Path.GetFileNameWithoutExtension(filePath);
            var m0 = Regex.Match(fnNoExt, @"^(?<base>.+)_(?<page>\d+)$");
            if (!m0.Success) return;                        // 패턴 미일치 → 무시
        
            string baseName = m0.Groups["base"].Value;      // 예: ABC
            string folder   = Path.GetDirectoryName(filePath);
        
            // 0-1) 이미 병합된 baseName이면 SKIP
            lock (mergedBaseNames)
            {
                if (mergedBaseNames.Contains(baseName))
                {
                    if (settingsManager.IsDebugMode)
                        logManager.LogDebug($"[ucImageTransPanel] Skip duplicate merge: {baseName}");
                    return;
                }
                mergedBaseNames.Add(baseName);
            }
        
            // 1) 병합 대상 이미지 수집 (존재 파일만)
            string[] exts = { ".jpg", ".jpeg", ".png", ".tif", ".tiff" };
            var imgList = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                                   .Where(p => exts.Contains(Path.GetExtension(p).ToLower()))
                                   .Select(p =>
                                   {
                                       var m = Regex.Match(Path.GetFileNameWithoutExtension(p),
                                                           $"^{Regex.Escape(baseName)}_(?<pg>\\d+)$",
                                                           RegexOptions.IgnoreCase);
                                       return (path: p, ok: m.Success,
                                               page: m.Success && int.TryParse(m.Groups["pg"].Value, out int n) ? n : -1);
                                   })
                                   .Where(x => x.ok)
                                   .OrderBy(x => x.page)
                                   .Select(x => x.path)
                                   .ToList();
        
            if (imgList.Count == 0)
            {
                if (settingsManager.IsDebugMode)
                    logManager.LogDebug($"[ucImageTransPanel] No images found for base '{baseName}' (이미 삭제되었을 수 있음).");
                return;                                     // 이미지가 전부 삭제된 경우에도 Error 로그 남기지 않음
            }
        
            // 2) PDF 출력 경로 확인
            string outputFolder = settingsManager.GetValueFromSection("ImageTrans", "SaveFolder");
            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
            {
                logManager.LogError("[ucImageTransPanel] Invalid output folder - cannot create PDF");
                return;
            }
            string outputPdfPath = Path.Combine(outputFolder, $"{baseName}.pdf");
        
            // 3) PDF 병합 실행 (MergeImagesToPdf 내부에서 이미지 삭제)
            pdfMergeManager.MergeImagesToPdf(imgList, outputPdfPath);
        
            logManager.LogEvent($"[ucImageTransPanel] Created PDF for baseName '{baseName}': {outputPdfPath}");
        }

        #region ====== 기존 UI/설정 메서드 ======

        private void btn_SelectOutputFolder_Click(object sender, EventArgs e)
        {
            logManager.LogEvent("[ucImageTransPanel] Select output folder initiated");

            string baseFolder = configPanel.BaseFolderPath;
            if (string.IsNullOrEmpty(baseFolder) || !Directory.Exists(baseFolder))
            {
                logManager.LogError("[ucImageTransPanel] Base folder not set or invalid");
                MessageBox.Show("기준 폴더(Base Folder)가 설정되지 않았습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = baseFolder;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolder = folderDialog.SelectedPath;
                    lb_ImageSaveFolder.Text = selectedFolder;
                    settingsManager.SetValueToSection("ImageTrans", "SaveFolder", selectedFolder);

                    logManager.LogEvent($"[ucImageTransPanel] Output folder set: {selectedFolder}");
                    MessageBox.Show("출력 폴더가 설정되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btn_SetFolder_Click(object sender, EventArgs e)
        {
            logManager.LogEvent("[ucImageTransPanel] Set target folder initiated");

            if (cb_TargetImageFolder.SelectedItem is string selectedFolder)
            {
                settingsManager.SetValueToSection("ImageTrans", "Target", selectedFolder);
                logManager.LogEvent($"[ucImageTransPanel] Target folder set: {selectedFolder}");
                MessageBox.Show($"폴더가 설정되었습니다: {selectedFolder}", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                logManager.LogError("[ucImageTransPanel] No target folder selected");
                MessageBox.Show("폴더를 선택하세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btn_FolderClear_Click(object sender, EventArgs e)
        {
            logManager.LogEvent("[ucImageTransPanel] Clearing target folder");

            if (cb_TargetImageFolder.SelectedItem != null)
            {
                cb_TargetImageFolder.SelectedIndex = -1;
                settingsManager.RemoveSection("ImageTrans");

                logManager.LogEvent("[ucImageTransPanel] Target folder cleared");
                MessageBox.Show("폴더 설정이 초기화되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                logManager.LogError("[ucImageTransPanel] No target folder selected to clear");
                MessageBox.Show("선택된 폴더가 없습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void LoadRegexFolderPaths()
        {
            cb_TargetImageFolder.Items.Clear();
            var regexFolders = configPanel.GetRegexList();
            cb_TargetImageFolder.Items.AddRange(regexFolders.ToArray());

            string selectedPath = settingsManager.GetValueFromSection("ImageTrans", "Target");
            if (!string.IsNullOrEmpty(selectedPath) && cb_TargetImageFolder.Items.Contains(selectedPath))
            {
                cb_TargetImageFolder.SelectedItem = selectedPath;
            }
            else
            {
                cb_TargetImageFolder.SelectedIndex = -1;
            }
            logManager.LogEvent("[ucImageTransPanel] Regex folder paths loaded");
        }

        private void LoadFolders()
        {
            cb_TargetImageFolder.Items.Clear();
            var folders = settingsManager.GetFoldersFromSection("[TargetFolders]");
            cb_TargetImageFolder.Items.AddRange(folders.ToArray());

            logManager.LogEvent("[ucImageTransPanel] Target folders loaded");
        }

        public void LoadWaitTimes()
        {
            cb_WaitTime.Items.Clear();
            cb_WaitTime.Items.AddRange(new object[] { "30", "60", "120", "180", "240", "300" });
            cb_WaitTime.SelectedIndex = -1;

            string savedWaitTime = settingsManager.GetValueFromSection("ImageTrans", "Wait");
            if (!string.IsNullOrEmpty(savedWaitTime) && cb_WaitTime.Items.Contains(savedWaitTime))
            {
                cb_WaitTime.SelectedItem = savedWaitTime;
            }
            logManager.LogEvent("[ucImageTransPanel] Wait times loaded");
        }

        private void btn_SetTime_Click(object sender, EventArgs e)
        {
            logManager.LogEvent("[ucImageTransPanel] Setting wait time");

            if (cb_WaitTime.SelectedItem is string selectedWaitTime && int.TryParse(selectedWaitTime, out int waitTime))
            {
                settingsManager.SetValueToSection("ImageTrans", "Wait", selectedWaitTime);
                logManager.LogEvent($"[ucImageTransPanel] Wait time set: {waitTime} seconds");
                MessageBox.Show($"대기 시간이 {waitTime}초로 설정되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                logManager.LogError("[ucImageTransPanel] Invalid wait time selected");
                MessageBox.Show("대기 시간을 선택하세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btn_TimeClear_Click(object sender, EventArgs e)
        {
            logManager.LogEvent("[ucImageTransPanel] Clearing wait time");

            if (cb_WaitTime.SelectedItem != null)
            {
                cb_WaitTime.SelectedIndex = -1;
                settingsManager.SetValueToSection("ImageTrans", "Wait", string.Empty);

                logManager.LogEvent("[ucImageTransPanel] Wait time cleared");
                MessageBox.Show("대기 시간이 초기화되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                logManager.LogError("[ucImageTransPanel] No wait time selected to clear");
                MessageBox.Show("선택된 대기 시간이 없습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadOutputFolder()
        {
            string outputFolder = settingsManager.GetValueFromSection("ImageTrans", "SaveFolder");

            if (!string.IsNullOrEmpty(outputFolder) && Directory.Exists(outputFolder))
            {
                lb_ImageSaveFolder.Text = outputFolder;
            }
            else
            {
                lb_ImageSaveFolder.Text = "Output folder not set or does not exist.";
            }
            logManager.LogEvent("[ucImageTransPanel] Output folder loaded");
        }

        public void RefreshUI()
        {
            LoadRegexFolderPaths();
            LoadWaitTimes();
            logManager.LogEvent("[ucImageTransPanel] UI refreshed");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopWatchingFolder();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
