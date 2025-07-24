// ucPanel\ucUploadPanel.cs
using ITM_Agent.Plugins;
using ITM_Agent.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ITM_Agent.ucPanel
{
    public partial class ucUploadPanel : UserControl
    {
        private readonly ConcurrentQueue<string> uploadQueue = new ConcurrentQueue<string>();
        private readonly CancellationTokenSource ctsUpload = new CancellationTokenSource();
        
        // 외부에서 주입받는 참조
        private ucConfigurationPanel configPanel;
        private ucPluginPanel pluginPanel;
        private SettingsManager settingsManager;
        private LogManager logManager;
        private readonly ucOverrideNamesPanel overridePanel;

        // 업로드 대상 폴더 감시용 FileSystemWatcher
        private FileSystemWatcher uploadFolderWatcher;
        private FileSystemWatcher preAlignFolderWatcher;    // Pre-Align 폴더 감시용

        private const string UploadSection       = "UploadSetting"; // 공통 섹션
        private const string UploadKey_WaferFlat = "WaferFlat";     // Wafer-Flat 전용 Key
        private const string UploadKey_PreAlign  = "PreAlign";      // Pre-Align 전용 Key
        private const string KeyFolder = "WaferFlatFolder";  // 폴더 키
        private const string KeyPlugin = "FilePlugin";  // 플러그인 키
        
        public ucUploadPanel(ucConfigurationPanel configPanel, ucPluginPanel pluginPanel, SettingsManager settingsManager, ucOverrideNamesPanel ovPanel)
        {
            InitializeComponent();

            // ① 의존성 주입
            this.configPanel = configPanel ?? throw new ArgumentNullException(nameof(configPanel));
            this.pluginPanel = pluginPanel ?? throw new ArgumentNullException(nameof(pluginPanel));
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.overridePanel = ovPanel;
        
            // ② 서비스
            logManager = new LogManager(AppDomain.CurrentDomain.BaseDirectory);
        
            // ③ 이벤트 연결
            this.pluginPanel.PluginsChanged += PluginPanel_PluginsChanged;
        
            btn_FlatSet.Click += btn_FlatSet_Click;
            btn_FlatClear.Click += btn_FlatClear_Click;
        
            btn_PreAlignSet.Click += btn_PreAlignSet_Click;   // ★ 추가
            btn_PreAlignClear.Click += btn_PreAlignClear_Click; // ★ 추가
        
            // ④ UI 로드
            LoadTargetFolderItems();
            LoadPluginItems();
        
            // ⑤ INI 복원
            LoadUploadSettings();
        
            // ⑥ 업로드 큐 소비 백그라운드
            Task.Run(() => ConsumeUploadQueueAsync(ctsUpload.Token));
        }
        
        /* ===== 개선된 전체 LoadUploadSettings() ===== */
        private void LoadUploadSettings()
        {
            // 1) Wafer-Flat ----------------------------------------------------
            string flatLine = settingsManager.GetValueFromSection(UploadSection, UploadKey_WaferFlat);
            if (!string.IsNullOrWhiteSpace(flatLine))
                RestoreUploadSetting("WaferFlat", flatLine);
        
            // 2) Pre-Align -----------------------------------------------------
            string preLine  = settingsManager.GetValueFromSection(UploadSection, UploadKey_PreAlign);
            if (!string.IsNullOrWhiteSpace(preLine))
                RestoreUploadSetting("PreAlign", preLine);
        }
        
        /* --- 헬퍼 : 공통 파서 ---------------------------------------------- */
        private void RestoreUploadSetting(string itemName, string valueLine)
        {
            // valueLine: "Folder : F:\TEST\ITM_Agent\wf, Plugin : Onto_WaferFlatData"
            string[] parts = valueLine.Split(',');
            if (parts.Length < 2) return;
        
            // (1) 폴더 경로
            int colonFolder = parts[0].IndexOf(':');
            string folderPath = colonFolder >= 0
                                ? parts[0].Substring(colonFolder + 1).Trim()
                                : string.Empty;
        
            // (2) 플러그인
            int colonPlugin = parts[1].IndexOf(':');
            string pluginName = colonPlugin >= 0
                                ? parts[1].Substring(colonPlugin + 1).Trim()
                                : string.Empty;
        
            if (itemName == "WaferFlat")
            {
                if (!cb_WaferFlat_Path.Items.Contains(folderPath))
                    cb_WaferFlat_Path.Items.Add(folderPath);
                cb_WaferFlat_Path.Text = folderPath;
        
                if (!cb_FlatPlugin.Items.Contains(pluginName))
                    cb_FlatPlugin.Items.Add(pluginName);
                cb_FlatPlugin.Text = pluginName;
        
                StartUploadFolderWatcher(folderPath);
            }
            else  // PreAlign
            {
                if (!cb_PreAlign_Path.Items.Contains(folderPath))
                    cb_PreAlign_Path.Items.Add(folderPath);
                cb_PreAlign_Path.Text = folderPath;
        
                if (!cb_PreAlignPlugin.Items.Contains(pluginName))
                    cb_PreAlignPlugin.Items.Add(pluginName);
                cb_PreAlignPlugin.Text = pluginName;
        
                StartPreAlignFolderWatcher(folderPath);
            }
        }

        // ucConfigurationPanel에서 대상 폴더 목록을 가져와 콤보박스에 로드
        private void LoadTargetFolderItems()
        {
            cb_WaferFlat_Path.Items.Clear();
            cb_PreAlign_Path.Items.Clear();                // ★ Pre-Align 추가
        
            IEnumerable<string> folders;
            if (configPanel != null)
                folders = configPanel.GetTargetFolders();
            else
                folders = settingsManager.GetFoldersFromSection("[TargetFolders]");
        
            cb_WaferFlat_Path.Items.AddRange(folders.ToArray());
            cb_PreAlign_Path.Items.AddRange(folders.ToArray()); // 두 콤보 모두 동일 목록
        }
        
        // ucPluginPanel에서 로드한 플러그인(PluginListItem) 목록을 콤보박스에 로드
        private void LoadPluginItems()
        {
            cb_FlatPlugin.Items.Clear();
            cb_PreAlignPlugin.Items.Clear();               // ★ Pre-Align 추가
        
            if (pluginPanel == null) return;
        
            foreach (var p in pluginPanel.GetLoadedPlugins())
            {
                cb_FlatPlugin.Items.Add(p.PluginName);
                cb_PreAlignPlugin.Items.Add(p.PluginName); // 두 콤보 모두 동일 목록
            }
        }
        
        // > 수정 전체 메서드 (원본은 주석 처리, 아래에 개선 코드 전체 제공)
        private void StartUploadFolderWatcher(string folderPath)
        {
            try
            {
                // ① 경로 문자열 정리(개행·앞뒤 공백 제거)
                folderPath = folderPath.Trim();
        
                // ② 유효성 검사
                if (string.IsNullOrEmpty(folderPath))
                    throw new ArgumentException("폴더 경로가 비어 있습니다.", nameof(folderPath));
        
                // ③ 폴더가 없으면 생성 (권한 없으면 예외 발생)
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
        
                // ④ 이전 Watcher 해제
                uploadFolderWatcher?.Dispose();
        
                // ⑤ 새로운 Watcher 생성
                uploadFolderWatcher = new FileSystemWatcher(folderPath)
                {
                    Filter = "*.*",
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName
                                            | NotifyFilters.Size
                                            | NotifyFilters.LastWrite,
                    EnableRaisingEvents   = true
                };
                uploadFolderWatcher.Created += UploadFolderWatcher_Event;
        
                logManager.LogEvent($"[UploadPanel] 폴더 감시 시작: {folderPath}");
            }
            catch (Exception ex)
            {
                // 실패 시 로그 + 사용자 알림
                logManager.LogError($"[UploadPanel] 폴더 감시 시작 실패: {ex.Message}");
                MessageBox.Show("폴더 감시를 시작할 수 없습니다.\n" + ex.Message,
                                "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void UploadFolderWatcher_Event(object sender, FileSystemEventArgs e)
        {
            /// 파일 생성 직후 잠금 해제까지 살짝 대기
            Thread.Sleep(200);
        
            uploadQueue.Enqueue(e.FullPath);
            logManager.LogEvent($"[UploadPanel] 대기 큐에 추가 : {e.FullPath}");
        
            string rawPath = e.FullPath;        // 원본 *.dat 경로
        
            /* 1) OverrideNamesPanel에서 선처리 (.info 생성·Rename) */
            string readyPath = overridePanel?.EnsureOverrideAndReturnPath(rawPath, 10_000);
            if (string.IsNullOrEmpty(readyPath))
            {
                logManager.LogError($"[UploadPanel] Override 미완료 → Upload 보류 : {rawPath}");
                return;
            }
        
            /* 2) UI 스레드 안전하게 플러그인 이름 취득 */
            string pluginName = string.Empty;
            if (InvokeRequired)
                Invoke(new MethodInvoker(() => pluginName = cb_FlatPlugin.Text.Trim()));
            else
                pluginName = cb_FlatPlugin.Text.Trim();
        
            if (string.IsNullOrEmpty(pluginName))
            {
                logManager.LogError("[UploadPanel] 플러그인이 선택되지 않았습니다.");
                return;
            }
        
            /* 3) Assembly 경로 확인 */
            var item = pluginPanel.GetLoadedPlugins()
                        .FirstOrDefault(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
            if (item == null || !File.Exists(item.AssemblyPath))
            {
                logManager.LogError($"[UploadPanel] 플러그인 DLL을 찾을 수 없습니다: {pluginName}");
                return;
            }
        
            try
            {
                /* 4) DLL 메모리 로드(잠금 방지) */
                byte[] dllBytes = File.ReadAllBytes(item.AssemblyPath);
                Assembly asm    = Assembly.Load(dllBytes);
        
                /* 5) ‘ProcessAndUpload’ 메서드를 가진 첫 번째 public 클래스 탐색 */
                Type targetType = asm.GetTypes()
                    .FirstOrDefault(t =>
                        t.IsClass && !t.IsAbstract &&
                        t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                         .Any(m => m.Name == "ProcessAndUpload"));
        
                if (targetType == null)
                {
                    logManager.LogError($"[UploadPanel] ProcessAndUpload 메서드를 가진 타입이 없습니다: {pluginName}");
                    return;
                }
        
                /* 6) 인스턴스 생성 (매개변수 없는 생성자 가정) */
                object pluginObj = Activator.CreateInstance(targetType);
        
                /* 7) 메서드 오버로드(1파라미터 / 2파라미터) 확인 */
                MethodInfo mi = targetType.GetMethod("ProcessAndUpload",
                                new[] { typeof(string), typeof(string) })   // (file, ini)
                            ?? targetType.GetMethod("ProcessAndUpload",
                                new[] { typeof(string) });                  // (file) 또는 (folder)
                if (mi == null)
                {
                    logManager.LogError($"[UploadPanel] ProcessAndUpload 메서드를 찾지 못했습니다: {pluginName}");
                    return;
                }
        
                /* 8) 인수 구성 후 호출 */
                string settingsIni = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");
                object[] args = mi.GetParameters().Length == 2
                                ? new object[] { readyPath, settingsIni }
                                : new object[] { readyPath };
        
                logManager.LogEvent($"[UploadPanel] 플러그인 실행 시작 > {pluginName}");
                mi.Invoke(pluginObj, args);
                logManager.LogEvent($"[UploadPanel] 플러그인 실행 완료 > {pluginName}");
            }
            catch (Exception ex)
            {
                logManager.LogError($"[UploadPanel] 플러그인 실행 실패: {ex.GetBaseException().Message}");
            }
        }
        
        // 파일 접근 준비 여부 확인 헬퍼 메서드
        private bool IsFileReady(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        
        // btn_FlatSet 클릭 시: 선택된 폴더와 플러그인 정보를 Settings.ini에 저장하고 감시 시작
        private void btn_FlatSet_Click(object sender, EventArgs e)
        {
            string folderPath = cb_WaferFlat_Path.Text.Trim();
            string pluginName = cb_FlatPlugin.Text.Trim();
        
            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(pluginName))
            {
                MessageBox.Show("Wafer-Flat 폴더와 플러그인을 모두 선택(입력)하세요.",
                                "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        
            string iniValue = $"Folder : {folderPath}, Plugin : {pluginName}";
            settingsManager.SetValueToSection(UploadSection, UploadKey_WaferFlat, iniValue);   // ★ 개별 Key
        
            if (!cb_WaferFlat_Path.Items.Contains(folderPath)) cb_WaferFlat_Path.Items.Add(folderPath);
            if (!cb_FlatPlugin.Items.Contains(pluginName))     cb_FlatPlugin.Items.Add(pluginName);
        
            logManager.LogEvent($"[ucUploadPanel] 저장(WaferFlat) ➜ {iniValue}");
            MessageBox.Show("Wafer-Flat 설정이 저장되었습니다.", "완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        
            StartUploadFolderWatcher(folderPath);
        }
        
        private void btn_FlatClear_Click(object sender, EventArgs e)
        {
            // ① UI 초기화
            cb_WaferFlat_Path.SelectedIndex = -1;
            cb_WaferFlat_Path.Text          = string.Empty;
            cb_FlatPlugin.SelectedIndex     = -1;
            cb_FlatPlugin.Text              = string.Empty;
        
            // ② 감시 중단
            if (uploadFolderWatcher != null)
            {
                uploadFolderWatcher.EnableRaisingEvents = false;
                uploadFolderWatcher.Dispose();
                uploadFolderWatcher = null;
            }
        
            // ③ Settings.ini 해당 Key 삭제
            settingsManager.RemoveKeyFromSection(UploadSection, UploadKey_WaferFlat);   // ★ 변경
            logManager.LogEvent("[ucUploadPanel] Wafer-Flat 설정 초기화");
        
            // ④ 안내
            MessageBox.Show("Wafer-Flat 업로드 설정이 초기화되었습니다.",
                            "초기화 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// [WaferFlat] 섹션에 폴더/플러그인 정보를 저장
        /// </summary>
        private void SaveWaferFlatSettings()
        {
            string folderPath = cb_WaferFlat_Path.Text.Trim();
            string pluginName = cb_FlatPlugin.Text.Trim();
        
            // 한 줄 포맷 예: "Folder : D:\ITM_Agent\wf, Plugin : Onto_WaferFlatData"
            string iniValue = $"Folder : {folderPath}, Plugin : {pluginName}";
            settingsManager.SetValueToSection(UploadSection, UploadKey_WaferFlat, iniValue);  // ★ 변경
        
            logManager.LogEvent($"[ucUploadPanel] 저장(WaferFlat) ➜ {iniValue}");
        }
        
        /// <summary>
        /// Settings.ini 의 [UploadSetting] 섹션에서
        /// Wafer-Flat 폴더·플러그인 정보를 읽어 콤보박스에 반영
        /// </summary>
        private void LoadWaferFlatSettings()
        {
            string line = settingsManager.GetValueFromSection(UploadSection, UploadKey_WaferFlat);
            if (string.IsNullOrWhiteSpace(line)) return;
        
            // "Folder : …, Plugin : …"
            string[] parts = line.Split(',');
            if (parts.Length < 2) return;
        
            string folderPath = parts[0].Split(':')[1].Trim();
            string pluginName = parts[1].Split(':')[1].Trim();
        
            if (!cb_WaferFlat_Path.Items.Contains(folderPath)) cb_WaferFlat_Path.Items.Add(folderPath);
            cb_WaferFlat_Path.Text = folderPath;
        
            if (!cb_FlatPlugin.Items.Contains(pluginName)) cb_FlatPlugin.Items.Add(pluginName);
            cb_FlatPlugin.Text = pluginName;
        
            StartUploadFolderWatcher(folderPath);
        }

        /// <summary>
        /// FileSystemWatcher 가 감시 폴더에서 새 파일을 감지했을 때 호출됩니다.
        /// </summary>
        private void UploadFolderWatcher_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                // (1) 전체 경로 얻기
                string filePath = e.FullPath;

                // (2) 로그 기록
                logManager.LogEvent("[UploadPanel] 새 파일 감지 : " + filePath);

                // (3) 워크로드가 무거울 수 있으므로 비동기로 처리
                Task.Run(() => ProcessWaferFlatFile(filePath));
            }
            catch (Exception ex)
            {
                logManager.LogError("[UploadPanel] 파일 처리 실패 : " + ex.Message);
            }
        }
        
        /// <summary>
        /// 플러그인을 이용해 Wafer-Flat 데이터 파일을 처리합니다.
        /// 실제 로직은 프로젝트 상황에 맞게 구현하세요.
        /// </summary>
        private void ProcessWaferFlatFile(string filePath)
        {
            // TODO: 플러그인 호출 및 DB 업로드 로직
            // ✔️ 여기서는 예시로 2초 Sleep 후 완료 로그만 남깁니다.
            System.Threading.Thread.Sleep(2000);
            logManager.LogEvent("[UploadPanel] 파일 처리 완료 : " + filePath);
        }
        
        private void PluginPanel_PluginsChanged(object sender, EventArgs e)
        {
            RefreshPluginCombo();
        }
        
        private void RefreshPluginCombo()
        {
            // (1) 현재 선택 상태 보존
            string prevSelection = cb_FlatPlugin.SelectedItem as string;
        
            cb_FlatPlugin.BeginUpdate();
            cb_FlatPlugin.Items.Clear();
        
            // (2) 플러그인 이름 다시 채우기
            foreach (var p in pluginPanel.GetLoadedPlugins())
                cb_FlatPlugin.Items.Add(p.PluginName);
        
            // ▼▼ 기존 코드 : 새 목록의 “마지막 항목”을 강제로 선택 -------------------
            //if (cb_FlatPlugin.Items.Count > 0)
            //    cb_FlatPlugin.SelectedIndex = cb_FlatPlugin.Items.Count - 1;
            // ▲▲ 삭제(주석처리) -----------------------------------------------------
        
            // (3) 이전에 선택돼 있던 값이 아직도 존재하면 그대로 유지
            if (!string.IsNullOrEmpty(prevSelection) &&
                cb_FlatPlugin.Items.Contains(prevSelection))
            {
                cb_FlatPlugin.SelectedItem = prevSelection;
            }
            else
            {
                // 아니면 아무 것도 선택하지 않음
                cb_FlatPlugin.SelectedIndex = -1;
            }
        
            cb_FlatPlugin.EndUpdate();
        }
        
        private async Task ConsumeUploadQueueAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // (0) 큐가 비어 있으면 잠시 대기
                    if (!uploadQueue.TryDequeue(out var rawPath))
                    {
                        await Task.Delay(300, token);
                        continue;
                    }
        
                    // (1) OverrideNamesPanel 선처리 (.info 대기 & rename)
                    string readyPath = overridePanel != null
                        ? overridePanel.EnsureOverrideAndReturnPath(rawPath, 180_000)
                        : rawPath;                           // 패널이 없으면 그대로 진행
        
                    // readyPath 는 null 이 아님 (.info 없이도 rename skip 처리됨)
        
                    // (2) 플러그인 이름 확보 (UI 스레드 안전)
                    string pluginName = string.Empty;
                    if (InvokeRequired)
                        Invoke(new MethodInvoker(() => pluginName = cb_FlatPlugin.Text.Trim()));
                    else
                        pluginName = cb_FlatPlugin.Text.Trim();
        
                    if (string.IsNullOrEmpty(pluginName))
                    {
                        logManager.LogError("[UploadPanel] 플러그인이 선택되지 않았습니다.");
                        continue;                           // 다음 파일 처리
                    }
        
                    // (3) DLL 경로 확인
                    var pluginItem = pluginPanel.GetLoadedPlugins()
                        .FirstOrDefault(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
        
                    if (pluginItem == null || !File.Exists(pluginItem.AssemblyPath))
                    {
                        logManager.LogError($"[UploadPanel] DLL을 찾을 수 없습니다: {pluginName}");
                        continue;
                    }
        
                    // (4) 플러그인 실행 (리플렉션 호출)
                    string err;
                    if (!TryRunProcessAndUpload(pluginItem.AssemblyPath, readyPath, out err))
                    {
                        logManager.LogError($"[UploadPanel] 업로드 실패: {Path.GetFileName(readyPath)} - {err}");
                    }
                    else
                    {
                        logManager.LogEvent($"[UploadPanel] 업로드 완료: {readyPath}");
                    }
                }
                catch (TaskCanceledException)
                {
                    // 취소 토큰으로 정상 종료
                    break;
                }
                catch (Exception ex)
                {
                    logManager.LogError($"[UploadPanel] 소비자 Task 오류: {ex.GetBaseException().Message}");
                }
            }
        }
        
        private bool TryRunProcessAndUpload(string dllPath, string readyPath, out string err)
        {
            err = null;
        
            try
            {
                /* (1) DLL을 메모리로만 로드해 파일 잠금 방지 */
                byte[] dllBytes = File.ReadAllBytes(dllPath);
                Assembly asm    = Assembly.Load(dllBytes);
        
                /* (2) ‘ProcessAndUpload’ 메서드를 가진 타입 검색 */
                Type targetType = asm.GetTypes()
                    .FirstOrDefault(t => t.IsClass && !t.IsAbstract &&
                                         t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                          .Any(m => m.Name == "ProcessAndUpload"));
        
                if (targetType == null)
                {
                    err = "ProcessAndUpload() 메서드를 가진 타입 없음";
                    return false;
                }
        
                /* (3) 인스턴스 생성 */
                object pluginObj = Activator.CreateInstance(targetType);
        
                /* (4) 2-파라미터 → 1-파라미터 순으로 메서드 찾기 */
                MethodInfo mi = targetType.GetMethod("ProcessAndUpload",
                                new[] { typeof(string), typeof(string) }) ??
                                targetType.GetMethod("ProcessAndUpload",
                                new[] { typeof(string) });
        
                if (mi == null)
                {
                    err = "호출 가능한 ProcessAndUpload() 오버로드 없음";
                    return false;
                }
        
                /* (5) 인자 배열 준비 후 Invoke */
                string settingsIni = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");
                object[] args = mi.GetParameters().Length == 2
                                ? new object[] { readyPath, settingsIni }
                                : new object[] { readyPath };
        
                mi.Invoke(pluginObj, args);
                return true;                        // ✔ 성공
            }
            catch (Exception ex)
            {
                err = ex.GetBaseException().Message;
                return false;                       // ✖ 실패
            }
        }
        
        public void UpdateStatusOnRun(bool isRunning)
        {
            // Run 상태이면 모든 조작 컨트롤을 잠그고,
            // Stopped/Ready 상태이면 다시 활성화합니다.
            SetControlsEnabled(!isRunning);
        }
        
        private void SetControlsEnabled(bool enabled)
        {
            // 버튼
            btn_FlatSet.Enabled = enabled;
            btn_FlatClear.Enabled = enabled;
            btn_PreAlignSet.Enabled = enabled;
            btn_PreAlignClear.Enabled = enabled;
            btn_ImgSet.Enabled = enabled;
            btn_ImgClear.Enabled = enabled;
            btn_ErrSet.Enabled = enabled;
            btn_ErrClear.Enabled = enabled;
            btn_EvSet.Enabled = enabled;
            btn_EvClear.Enabled = enabled;
            btn_WaveSet.Enabled = enabled;
            btn_WaveClear.Enabled = enabled;
        
            // 콤보박스(폴더 경로)
            cb_WaferFlat_Path.Enabled = enabled;
            cb_PreAlign_Path.Enabled = enabled;
            cb_ImgPath.Enabled = enabled;
            cb_ErrPath.Enabled = enabled;
            cb_EvPath.Enabled = enabled;
            cb_WavePath.Enabled = enabled;

            // 콤보박스(플러그-인 선택)
            cb_FlatPlugin.Enabled = enabled;
            cb_PreAlignPlugin.Enabled = enabled;
            cb_ImagePlugin.Enabled = enabled;
            cb_ErrPlugin.Enabled = enabled;
            cb_EvPlugin.Enabled = enabled;
            cb_WavePlugin.Enabled = enabled;
        }
        
        private void btn_PreAlignSet_Click(object sender, EventArgs e)
        {
            string folderPath = cb_PreAlign_Path.Text.Trim();
            string pluginName = cb_PreAlignPlugin.Text.Trim();
        
            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(pluginName))
            {
                MessageBox.Show("Pre-Align 폴더와 플러그인을 모두 선택(입력)하세요.",
                                "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        
            string iniValue = $"Folder : {folderPath}, Plugin : {pluginName}";
            settingsManager.SetValueToSection(UploadSection, UploadKey_PreAlign, iniValue);    // ★ 개별 Key
        
            if (!cb_PreAlign_Path.Items.Contains(folderPath))     cb_PreAlign_Path.Items.Add(folderPath);
            if (!cb_PreAlignPlugin.Items.Contains(pluginName))    cb_PreAlignPlugin.Items.Add(pluginName);
        
            logManager.LogEvent($"[ucUploadPanel] 저장(PreAlign) ➜ {iniValue}");
            MessageBox.Show("Pre-Align 설정이 저장되었습니다.", "완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        
            StartPreAlignFolderWatcher(folderPath);
        }
        
        /* ===== 개선된 전체 btn_PreAlignClear_Click ===== */
        private void btn_PreAlignClear_Click(object sender, EventArgs e)
        {
            cb_PreAlign_Path.SelectedIndex  = -1;
            cb_PreAlign_Path.Text           = string.Empty;
            cb_PreAlignPlugin.SelectedIndex = -1;
            cb_PreAlignPlugin.Text          = string.Empty;
        
            preAlignFolderWatcher?.Dispose();
            preAlignFolderWatcher = null;
        
            settingsManager.RemoveKeyFromSection(UploadSection, UploadKey_PreAlign);          // ★ 개별 Key
            logManager.LogEvent("[ucUploadPanel] Pre-Align 설정 초기화");
        
            MessageBox.Show("Pre-Align 업로드 설정이 초기화되었습니다.",
                            "초기화 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void StartPreAlignFolderWatcher(string folderPath)
        {
            try
            {
                folderPath = folderPath.Trim();
                if (string.IsNullOrEmpty(folderPath))
                    throw new ArgumentException("폴더 경로가 비어 있습니다.", nameof(folderPath));
        
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
        
                preAlignFolderWatcher?.Dispose(); // 기존 감시 해제
        
                preAlignFolderWatcher = new FileSystemWatcher(folderPath)
                {
                    Filter = "*.*",
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };
                preAlignFolderWatcher.Created += PreAlignFolderWatcher_Event;
        
                logManager.LogEvent($"[UploadPanel] Pre-Align 폴더 감시 시작: {folderPath}");
            }
            catch (Exception ex)
            {
                logManager.LogError($"[UploadPanel] Pre-Align 폴더 감시 시작 실패: {ex.Message}");
                MessageBox.Show("Pre-Align 폴더 감시를 시작할 수 없습니다.\n" + ex.Message,
                                "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // ───────────────── PreAlignFolderWatcher_Event ─────────────────
        private void PreAlignFolderWatcher_Event(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(200);                        // 잠금 해제 대기
            uploadQueue.Enqueue(e.FullPath);          // 기존 큐 재사용
            logManager.LogEvent($"[UploadPanel] (Pre-Align) 대기 큐 추가 : {e.FullPath}");
        
            // OverrideNamesPanel 처리
            string readyPath = overridePanel?.EnsureOverrideAndReturnPath(e.FullPath, 10_000)
                               ?? e.FullPath;
        
            string pluginName = string.Empty;
            if (InvokeRequired)
                Invoke(new MethodInvoker(() => pluginName = cb_PreAlignPlugin.Text.Trim()));
            else
                pluginName = cb_PreAlignPlugin.Text.Trim();
        
            if (string.IsNullOrEmpty(pluginName))
            {
                logManager.LogError("[UploadPanel] (Pre-Align) 플러그인 미선택");
                return;
            }
        
            // 이후 로직은 Wafer-Flat 처리와 동일 → 공통 메서드 호출
            Task.Run(() => ProcessFileWithPlugin(readyPath, pluginName));
        }
        
        // 공통 처리 메서드 (Wafer-Flat / Pre-Align 모두 사용)
        private void ProcessFileWithPlugin(string filePath, string pluginName)
        {
            var item = pluginPanel.GetLoadedPlugins()
                       .FirstOrDefault(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
        
            if (item == null || !File.Exists(item.AssemblyPath))
            {
                logManager.LogError($"[UploadPanel] 플러그인 DLL을 찾을 수 없습니다: {pluginName}");
                return;
            }
        
            try
            {
                byte[] dllBytes = File.ReadAllBytes(item.AssemblyPath);
                Assembly asm    = Assembly.Load(dllBytes);
        
                Type targetType = asm.GetTypes()
                    .FirstOrDefault(t => t.IsClass && !t.IsAbstract &&
                                         t.GetMethod("ProcessAndUpload",
                                                     BindingFlags.Instance | BindingFlags.Public) != null);
        
                if (targetType == null)
                {
                    logManager.LogError($"[UploadPanel] ProcessAndUpload 메서드가 없습니다: {pluginName}");
                    return;
                }
        
                dynamic instance = Activator.CreateInstance(targetType);
                instance.ProcessAndUpload(filePath, settingsManager, logManager); // 예시 시그니처
                logManager.LogEvent($"[UploadPanel] ({pluginName}) 업로드 완료 : {filePath}");
            }
            catch (Exception ex)
            {
                logManager.LogError($"[UploadPanel] ({pluginName}) 처리 실패: {ex.Message}");
            }
        }
    }
}
