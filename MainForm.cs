// MainForm.cs
using ITM_Agent.Services;
using ITM_Agent.Startup;
using ITM_Agent.ucPanel;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ITM_Agent
{
    public partial class MainForm : Form
    {
        private bool isExiting = false;              // [추가] 중복·재귀 종료 방지
        private SettingsManager settingsManager;
        private LogManager logManager;
        private FileWatcherManager fileWatcherManager;
        private EqpidManager eqpidManager;
        private InfoRetentionCleaner infoCleaner;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem titleItem;
        private ToolStripMenuItem runItem;
        private ToolStripMenuItem stopItem;
        private ToolStripMenuItem quitItem;

        private const string AppVersion = "v1.0.0";

        ucPanel.ucConfigurationPanel ucSc1;

        private ucConfigurationPanel ucConfigPanel;
        private ucOverrideNamesPanel ucOverrideNamesPanel;
        private ucImageTransPanel ucImageTransPanel;
        //private ucScreen4 ucUploadDataPanel;

        private bool isRunning = false; // 현재 상태 플래그
        private bool isDebugMode = false;   // 디버그 모드 상태
        private ucOptionPanel ucOptionPanel;  // ← 옵션 패널

        // MainForm.cs 상단 (다른 user control 변수들과 함께)
        private ucUploadPanel ucUploadPanel;
        private ucPluginPanel ucPluginPanel;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PerformanceWarmUp.Run();    // 예열
        }
        
        public MainForm(SettingsManager settingsManager)
        {
           this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
           this.settingsManager = settingsManager;
        
           InitializeComponent();
        
           // 폼 핸들이 생성된 직후 상태 표시(Stopped, 빨간색)
           this.HandleCreated += (sender, e) => UpdateMainStatus("Stopped", Color.Red);
        
           string baseDir = AppDomain.CurrentDomain.BaseDirectory;
           logManager = new LogManager(baseDir);
        
           InitializeUserControls();
           RegisterMenuEvents();
        
           // 설정 패널
           ucSc1 = new ucPanel.ucConfigurationPanel(settingsManager);
           // Override Names 패널 (Designer에서 배치된 ucConfigPanel 컨트롤 인스턴스 전달)
           ucOverrideNamesPanel = new ucOverrideNamesPanel(settingsManager,this.ucConfigPanel,this.logManager,this.settingsManager.IsDebugMode);
        
           // FileWatcherManager 생성 (SettingsManager, LogManager, 디버그 모드 플래그)
           fileWatcherManager = new FileWatcherManager(settingsManager, logManager, isDebugMode);
           eqpidManager = new EqpidManager(settingsManager, logManager, AppVersion);
        
           // 아이콘 설정
           SetFormIcon();
        
           this.Text = $"ITM Agent - {AppVersion}";
           this.MaximizeBox = false;
        
           InitializeTrayIcon();
           this.FormClosing += MainForm_FormClosing;
        
           // EQPID 초기화 및 메인 기능 진행
           eqpidManager.InitializeEqpid();
           string eqpid = settingsManager.GetEqpid();
           if (!string.IsNullOrEmpty(eqpid))
           {
               ProceedWithMainFunctionality(eqpid);
           }
        
           fileWatcherManager.InitializeWatchers();
        
           btn_Run.Click += btn_Run_Click;
           btn_Stop.Click += btn_Stop_Click;
           // btn_Quit.Click += btn_Quit_Click;
        
           UpdateUIBasedOnSettings();
        }
        
        private void SetFormIcon()
        {
            // 제목줄 아이콘 설정
            this.Icon = new Icon(@"Resources\Icons\icon.ico");
        }

        private void ProceedWithMainFunctionality(string eqpid)
        {
            lb_eqpid.Text = $"Eqpid: {eqpid}";
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();

            titleItem = new ToolStripMenuItem(this.Text);
            titleItem.Click += (sender, e) => RestoreMainForm();
            trayMenu.Items.Add(titleItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            runItem = new ToolStripMenuItem("Run", null, (sender, e) => btn_Run.PerformClick());
            trayMenu.Items.Add(runItem);
            
            // Stop 메뉴 클릭 시 btn_Stop_Click 이벤트 호출
            stopItem = new ToolStripMenuItem("Stop", null, (sender, e) => btn_Stop.PerformClick());
            trayMenu.Items.Add(stopItem);
            
            // Quit 메뉴 클릭 시 btn_Quit_Click 이벤트 호출
            quitItem = new ToolStripMenuItem("Quit", null, (sender, e) => btn_Quit.PerformClick());
            trayMenu.Items.Add(quitItem);

            trayIcon = new NotifyIcon
            {
                Icon = new Icon(@"Resources\Icons\icon.ico"), // TrayIcon에 사용할 아이콘
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = this.Text
            };
            trayIcon.DoubleClick += (sender, e) => RestoreMainForm();
        }

        private void RestoreMainForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            titleItem.Enabled = false;  // 트레이 메뉴 비활성화
        }

        private void UpdateTrayMenuStatus()
        {
            if (runItem != null) runItem.Enabled = btn_Run.Enabled;
            if (stopItem != null) stopItem.Enabled = btn_Stop.Enabled;
            if (quitItem != null) quitItem.Enabled = btn_Quit.Enabled;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) // X 버튼 클릭 시
            {
                e.Cancel = true; // 종료 방지
                this.Hide(); // 폼을 숨김
                trayIcon.BalloonTipTitle = "ITM Agent";
                trayIcon.BalloonTipText = "ITM Agent가 백그라운드에서 실행 중입니다.";
                trayIcon.ShowBalloonTip(3000); // 3초 동안 풍선 도움말 표시
                return;                     // 더 진행하지 않음
            }
            // ② Alt+F4, 메뉴 Quit, Application.Exit 등 ‘실제 종료’ 요청
            if (!isExiting)
            {
                e.Cancel = true;           // 첫 진입에서는 일단 취소
                isExiting = true;           // 재진입 차단 플래그       // [추가]
                PerformQuit();              // 공통 종료 루틴 호출      // [추가]
                return;
            }
        }

        private void UpdateUIBasedOnSettings()
        {
            // SettingsManager의 IsReadyToRun 결과에 따라 상태 업데이트
            if (settingsManager.IsReadyToRun())
            {
                UpdateMainStatus("Ready to Run", Color.Green);
                btn_Run.Enabled = true; // Run 버튼 활성화
            }
            else
            {
                UpdateMainStatus("Stopped!", Color.Red);
                btn_Run.Enabled = false; // Run 버튼 비활성화
            }
            btn_Stop.Enabled = false; // 초기 상태에서 Stop 버튼 비활성화
            btn_Quit.Enabled = true;  // Quit 버튼 활성화
        }

        private void UpdateMainStatus(string status, Color color)
        {
            ts_Status.Text = status;
            ts_Status.ForeColor = color;
            
            /* ---------- 런닝 상태 판정 로직 ---------- */
            bool isRunning = status.StartsWith("Running");    // ← 핵심 수정
            
            // --- 모든 UserControl에 상태 전달 ---------------------
            ucOverrideNamesPanel?.UpdateStatus(status);
            ucConfigPanel?.UpdateStatusOnRun(isRunning);
            ucOverrideNamesPanel?.UpdateStatusOnRun(isRunning);
            ucImageTransPanel?.UpdateStatusOnRun(isRunning);
            ucUploadPanel?.UpdateStatusOnRun(isRunning);
            ucPluginPanel?.UpdateStatusOnRun(isRunning);      // 추가

            // 디버그 체크박스
            // cb_DebugMode.Enabled = !isRunning;
        
            logManager.LogEvent($"Status updated to: {status}");
            if (isDebugMode)
                logManager.LogDebug($"Status updated to: {status}. Running state: {isRunning}");
        
            // --- 버튼/메뉴/트레이 항목 Enable 처리 ----------------
            if (status == "Stopped!")
            {
                btn_Run.Enabled = false;
                btn_Stop.Enabled = false;
                btn_Quit.Enabled = true;
            }
            else if (status == "Ready to Run")
            {
                btn_Run.Enabled = true;
                btn_Stop.Enabled = false;
                btn_Quit.Enabled = true;
            }
            else if (isRunning)                     // ← 핵심 수정
            {
                btn_Run.Enabled = false;
                btn_Stop.Enabled = true;
                btn_Quit.Enabled = false;
            }
            else
            {
                btn_Run.Enabled = false;
                btn_Stop.Enabled = false;
                btn_Quit.Enabled = false;
            }
        
            UpdateTrayMenuStatus();
            UpdateMenuItemsState(isRunning);
            UpdateButtonsState();
        }

        private void UpdateMenuItemsState(bool isRunning)
        {
            if (menuStrip1 != null)
            {
                foreach (ToolStripMenuItem item in menuStrip1.Items)
                {
                    if (item.Text == "File")
                    {
                        foreach (ToolStripItem subItem in item.DropDownItems)
                        {
                            if (subItem.Text == "New" || subItem.Text == "Open" || subItem.Text == "Quit")
                            {
                                subItem.Enabled = !isRunning; // Running 상태에서 비활성화
                            }
                        }
                    }
                }
            }
        }

        private void btn_Run_Click(object sender, EventArgs e)
        {
            logManager.LogEvent("Run button clicked.");
            try
            {
                /*──────── File Watcher 시작 ────────*/
                fileWatcherManager.StartWatching();
        
                /*──────── Performance DB 로깅 ON ───*/   // [추가]
                PerformanceDbWriter.Start(lb_eqpid.Text);  // [추가]
                
                isRunning = true; // 상태 업데이트
                UpdateMainStatus("Running...", Color.Blue);

                if (isDebugMode)
                {
                    logManager.LogDebug("FileWatcherManager started successfully.");
                }
            }
            catch (Exception ex)
            {
                logManager.LogError($"Error starting monitoring: {ex.Message}");
                UpdateMainStatus("Stopped!", Color.Red);
            }
        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            // 경고창 표시
            DialogResult result = MessageBox.Show(
                "프로그램을 중지하시겠습니까?\n모든 파일 감시 및 업로드 기능이 중단됩니다.",
                "작업 중지 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
        
            if (result == DialogResult.Yes)
            {
                logManager.LogEvent("Stop button clicked and confirmed.");
        
                try
                {
                    /*─ FileWatcher + Performance 로깅 중지 ─*/
                    fileWatcherManager.StopWatchers();           // [기존]
                    PerformanceDbWriter.Stop();                  // [추가]
        
                    isRunning = false;
        
                    /*─ 상태 표시 반영 ─*/
                    bool isReady = ucConfigPanel.IsReadyToRun();
                    UpdateMainStatus(isReady ? "Ready to Run" : "Stopped!", 
                                     isReady ? Color.Green     : Color.Red);
        
                    /*─ 패널 동기화 ─*/
                    ucConfigPanel.InitializePanel(isRunning);
                    ucOverrideNamesPanel.InitializePanel(isRunning);
        
                    if (isDebugMode)
                        logManager.LogDebug("FileWatcherManager & PerformanceDbWriter stopped successfully."); // [추가]
                }
                catch (Exception ex)                               // [추가]
                {
                    logManager.LogError($"Error stopping processes: {ex.Message}"); // [추가]
                    UpdateMainStatus("Error Stopping!", Color.Red);                 // [추가]
                }
            }
            else
            {
                logManager.LogEvent("Stop action was canceled by the user.");
            }
        }

        private void UpdateButtonsState()
        {
            //btn_Run.Enabled = !isRunning;
            //btn_Stop.Enabled = isRunning;
            //btn_Quit.Enabled = !isRunning;

            UpdateTrayMenuStatus(); // Tray 아이콘 상태 업데이트
        }

        private void btn_Quit_Click(object sender, EventArgs e)
        {
            // 종료 확인창 표시
            DialogResult result = MessageBox.Show(
                "프로그램을 완전히 종료하시겠습니까?",
                "종료 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            // 'Yes'를 선택한 경우에만 종료
            if (result == DialogResult.Yes)
            {
                PerformQuit(); // 실제 종료 로직 호출
            }
        }

        private void PerformQuit()
        {
            logManager?.LogEvent("[MainForm] Quit requested.");
            
            /* 1) 러너 Watcher 안전 종료 */
            try
            {
                fileWatcherManager?.StopWatchers();   // ☑ Dispose → StopWatchers
                fileWatcherManager = null;
                
                // SettingsManager는 Dispose 필요 없음
                settingsManager = null;
            }
            catch (Exception ex)
            {
                logManager?.LogError($"[MainForm] Clean-up error: {ex}");
            }
            
            /* 2) TrayIcon 정리 */
            try
            {
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                    trayIcon = null;
                }
                trayMenu?.Dispose();
                trayMenu = null;
            }
            catch (Exception ex)
            {
                logManager?.LogError($"[MainForm] Tray clean-up error: {ex}");
            }
            
            /* 3) 애플리케이션 종료 */
            BeginInvoke(new Action(() =>
            {
                logManager?.LogEvent("[MainForm] Application.Exit invoked.");
                Application.Exit();                   // Environment.Exit → Application.Exit
            }));
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // (1) 폼 로드시 실행할 로직
            pMain.Controls.Add(ucSc1);
            UpdateMenusBasedOnType();   // 메뉴 상태 업데이트

            // (2) 초기 패널 설정 및 UserControl 상태 동기화
            ShowUserControl(ucConfigPanel); // 가장 먼저 ucConfigPanel 보여줌

            // 만약 isRunning 플래그를 기본값으로 false로 두고 있다면,
            // 우선 "멈춤" 상태를 패널들에 반영
            ucConfigPanel.UpdateStatusOnRun(isRunning);
            ucImageTransPanel.UpdateStatusOnRun(isRunning);

            // (3) ucConfigurationPanel 에서 현재 Target/Folder/Regex 등이 모두 세팅되었는지 확인
            bool isReady = ucConfigPanel.IsReadyToRun();

            // (4) 준비되었으면 "Ready to Run" 상태로,
            //     아니면 "Stopped!" 상태로 업데이트
            if (isReady)
            {
                UpdateMainStatus("Ready to Run", Color.Green);
            }
            else
            {
                UpdateMainStatus("Stopped!", Color.Red);
            }
        }

        private void RefreshUI()
        {
            // Eqpid 상태 갱신
            string eqpid = settingsManager.GetEqpid();
            lb_eqpid.Text = $"Eqpid: {eqpid}";

            // TargetFolders, Regex 리스트 갱신
            ucSc1.RefreshUI(); // UserControl의 UI 갱신 호출
            ucConfigPanel?.RefreshUI();
            ucOverrideNamesPanel?.RefreshUI();

            // MainForm 상태 업데이트
            UpdateUIBasedOnSettings();
        }

        private void NewMenuItem_Click(object sender, EventArgs e)
        {
            // Settings 초기화 (Eqpid 제외)
            settingsManager.ResetExceptEqpid();
            MessageBox.Show("Settings 초기화 완료 (Eqpid 제외)", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);

            RefreshUI(); // 초기화 후 UI 갱신
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        settingsManager.LoadFromFile(openFileDialog.FileName);
                        MessageBox.Show("새로운 Settings.ini 파일이 로드되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        RefreshUI(); // 파일 로드 후 UI 갱신
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"파일 로드 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveAsMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        settingsManager.SaveToFile(saveFileDialog.FileName);
                        MessageBox.Show("Settings.ini가 저장되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"파일 저장 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            // Quit 버튼 클릭 시 btn_Quit_Click 호출
            btn_Quit.PerformClick();
        }

        private void InitializeUserControls()
        {
            // 1) 설정(분류) 패널 – 다른 패널들이 참조하므로 가장 먼저
            ucConfigPanel = new ucConfigurationPanel(settingsManager);
        
            // 2) 플러그인 목록 패널 – UploadPanel 에서 참조
            ucPluginPanel = new ucPluginPanel(settingsManager);
        
            // 3) Override Names 패널 – UploadPanel 보다 먼저 생성
            ucOverrideNamesPanel = new ucOverrideNamesPanel(
                settingsManager, ucConfigPanel, logManager, settingsManager.IsDebugMode);
        
            // 4) 이미지 병합(PDF 변환) 패널
            ucImageTransPanel = new ucImageTransPanel(settingsManager, ucConfigPanel);
        
            // 5) 업로드 패널 – OverrideNamesPanel 참조 추가됨 (4번째 인자)
            ucUploadPanel = new ucUploadPanel(
                ucConfigPanel, ucPluginPanel, settingsManager, ucOverrideNamesPanel);
        
            // 6) **새 옵션 패널 생성**
            ucOptionPanel = new ucOptionPanel(settingsManager);
            ucOptionPanel.DebugModeChanged += OptionPanel_DebugModeChanged;
        
            // 7) 필요한 패널 중 디자이너에 배치되지 않은 것만 컨트롤 컬렉션에 추가
            this.Controls.Add(ucOverrideNamesPanel);
        
            // 8) ★ 모든 패널 Run 상태 초기화 ★               // [수정]
            ucConfigPanel.InitializePanel(isRunning);
            ucOverrideNamesPanel.InitializePanel(isRunning);
            ucPluginPanel.InitializePanel(isRunning);         // [추가]
        }

        private void RegisterMenuEvents()
        {
            // Common -> Categorize
            tsm_Categorize.Click += (s, e) => ShowUserControl(ucConfigPanel);
        
            // ONTO -> Override Names
            tsm_OverrideNames.Click += (s, e) => ShowUserControl(ucOverrideNamesPanel);
        
            // ONTO -> Image Trans
            tsm_ImageTrans.Click += (s, e) => ShowUserControl(ucImageTransPanel);
        
            // ONTO -> Upload Data
            tsm_UploadData.Click += (s, e) => ShowUserControl(ucUploadPanel);
        
            // Common -> Plugin
            tsm_PluginList.Click += (s, e) => ShowUserControl(ucPluginPanel);
        
            // **새 옵션 메뉴**
            tsm_Option.Click += (s, e) => ShowUserControl(ucOptionPanel);
        }

        private void OptionPanel_DebugModeChanged(bool isDebug)
        {
            isDebugMode = isDebug;                               // 내부 플래그
            fileWatcherManager.UpdateDebugMode(isDebugMode);     // FileSystemWatcher 즉시 반영
        
            if (isDebugMode)
            {
                logManager.LogEvent("Debug Mode: Enabled");
                logManager.LogDebug("Debug mode enabled.");
            }
            else
            {
                logManager.LogEvent("Debug Mode: Disabled");
                logManager.LogDebug("Debug mode disabled.");
            }
        }

        private void ShowUserControl(UserControl control)
        {
            pMain.Controls.Clear();
            pMain.Controls.Add(control);
            control.Dock = DockStyle.Fill;

            // 상태 동기화
            if (control is ucConfigurationPanel cfg) cfg.InitializePanel(isRunning);
            else if (control is ucOverrideNamesPanel ov) ov.InitializePanel(isRunning);
            else if (control is ucPluginPanel plg) plg.InitializePanel(isRunning);  // [추가]
        }

        private void UpdateMenusBasedOnType()
        {
            string type = settingsManager.GetType();
            if (type == "ONTO")
            {
                tsm_Nova.Visible = false;
                tsm_Onto.Visible = true;
            }
            else if (type == "NOVA")
            {
                tsm_Onto.Visible = false;
                tsm_Nova.Visible = true;
            }
            else
            {
                tsm_Onto.Visible = false;
                tsm_Nova.Visible = false;
                return;
            }

            // Type 값에 따라 메뉴 표시/숨김 처리
            tsm_Onto.Visible = type.Equals("ONTO", StringComparison.OrdinalIgnoreCase);
            tsm_Nova.Visible = type.Equals("NOVA", StringComparison.OrdinalIgnoreCase);
        }

        private void InitializeMainMenu()
        {
            // 기존 메뉴 초기화 코드...
            UpdateMenusBasedOnType();
        }
        // 기본 생성자 추가
        public MainForm()
            : this(new SettingsManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini")))
        {
            // 추가 동작 없음
        }
    }
}
