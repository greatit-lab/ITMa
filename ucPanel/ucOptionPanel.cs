// ucPanel\ucOptionPanel.cs
using System;
using System.Windows.Forms;
using ITM_Agent.Services;

namespace ITM_Agent.ucPanel
{
    /// <summary>
    /// MenuStrip1 → tsm_Option 클릭 시 표시되는 옵션(UserControl)  
    /// Debug Mode 체크 상태를 SettingsManager · LogManager · MainForm 에 즉시 전파합니다.
    /// </summary>
    public partial class ucOptionPanel : UserControl
    {
        private const string OptSection = "Option";
        private const string Key_PerfLog = "EnablePerfoLog";
        private const string Key_InfoAutoDel = "EnableInfoAutoDel";
        private const string Key_InfoRetention = "InfoRetentionDays";

        private readonly SettingsManager settingsManager;

        public event Action<bool> DebugModeChanged;

        public ucOptionPanel(SettingsManager settings)
        {
            this.settingsManager = settings ?? throw new ArgumentNullException(nameof(settings));
            InitializeComponent();

            /* 1) Retention 콤보박스 고정 값 • DropDownList */
            cb_info_Retention.Items.Clear();
            cb_info_Retention.Items.AddRange(new object[] { "1", "3", "5" });
            cb_info_Retention.DropDownStyle = ComboBoxStyle.DropDownList;
    
            /* 2) UI 기본 비활성화 */
            UpdateRetentionControls(false);
    
            /* Settings.ini ↔ UI 동기화 */
            chk_infoDel.Checked = settingsManager.IsInfoDeletionEnabled;         // ← 새 프로퍼티
            cb_info_Retention.Enabled = label3.Enabled = label4.Enabled = chk_infoDel.Checked;               // 초기 비활성 규칙
            if (chk_infoDel.Checked)
            {
                string d = settingsManager.InfoRetentionDays.ToString();
                cb_info_Retention.SelectedItem = cb_info_Retention.Items.Contains(d) ? d : "1";
            }
    
            /* 3) 이벤트 연결 */
            chk_PerfoMode.CheckedChanged += chk_PerfoMode_CheckedChanged;
            chk_infoDel.CheckedChanged += chk_infoDel_CheckedChanged;
            cb_info_Retention.SelectedIndexChanged += cb_info_Retention_SelectedIndexChanged;
    
            /* 4) Settings.ini → UI 복원 */
            LoadOptionSettings();
        }

        #region ====== Run 상태 동기화 ======   // [추가]
        /// <summary>Run/Stop 상태에 따라 모든 입력 컨트롤 Enable 토글</summary>
        private void SetControlsEnabled(bool enabled)     // [추가]
        {
            chk_DebugMode.Enabled  = enabled;             // [추가]
            chk_PerfoMode.Enabled  = enabled;             // [추가]
            chk_infoDel.Enabled    = enabled;             // [추가]
    
            /* Retention-관련 컨트롤은 ‘Info Delete’ 체크 여부와 동기화 */
            UpdateRetentionControls(enabled && chk_infoDel.Checked);   // [추가]
        }
    
        /// <summary>MainForm 에서 Run/Stop 전환 시 호출</summary>
        public void UpdateStatusOnRun(bool isRunning)     // [추가]
        {
            SetControlsEnabled(!isRunning);               // [추가]
        }
    
        /// <summary>처음 패널 로드 또는 화면 전환 시 상태 맞춤</summary>
        public void InitializePanel(bool isRunning)       // [추가]
        {
            SetControlsEnabled(!isRunning);               // [추가]
        }
        #endregion

        private void LoadOptionSettings()
        {
            // DebugMode 는 기존 로직 유지
            chk_DebugMode.Checked = settingsManager.IsDebugMode;
    
            /* Perf-Log */
            bool perf = settingsManager.GetValueFromSection(OptSection, Key_PerfLog) == "1";
            chk_PerfoMode.Checked = perf;
    
            /* Info 자동 삭제 */
            bool infoDel = settingsManager.GetValueFromSection(OptSection, Key_InfoAutoDel) == "1";
            chk_infoDel.Checked = infoDel;
    
            /* Retention 일수 */
            string days = settingsManager.GetValueFromSection(OptSection, Key_InfoRetention);
            if (days == "1" || days == "3" || days == "5")
                cb_info_Retention.SelectedItem = days;
    
            /* UI 동기화 */
            UpdateRetentionControls(infoDel);
        }

        private void UpdateRetentionControls(bool enabled)
        {
            label3.Enabled = enabled;
            cb_info_Retention.Enabled = enabled;
            label4.Enabled = enabled;

            if (!enabled) cb_info_Retention.SelectedIndex = -1;  // 체크 해제 시 초기화
        }

        private void chk_PerfoMode_CheckedChanged(object sender, EventArgs e)
        {
            bool enable = chk_PerfoMode.Checked;
            PerformanceMonitor.Instance.StartSampling();
            PerformanceMonitor.Instance.SetFileLogging(enable);
    
            settingsManager.IsPerformanceLogging = enable;
        }

        private void chk_infoDel_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = chk_infoDel.Checked;

            /* UI ↔ Settings */
            settingsManager.IsInfoDeletionEnabled = enabled;
            settingsManager.InfoRetentionDays =
                enabled ? int.Parse(cb_info_Retention.SelectedItem?.ToString() ?? "1") : 0;
        
            /* 컨트롤 Enable */
            cb_info_Retention.Enabled = label3.Enabled = label4.Enabled = enabled;
        
            /* 체크 시 기본 1 day 선택 */
            if (enabled && cb_info_Retention.SelectedIndex < 0)
                cb_info_Retention.SelectedItem = "1";
        }

        private void cb_info_Retention_SelectedIndexChanged(object s, EventArgs e)
        {
            if (!chk_infoDel.Checked) return;                  // Info-삭제 기능 Off 시 무시

            object item = cb_info_Retention.SelectedItem;
            if (item == null)     // 선택 해제 상태
                return;

            if (int.TryParse(item.ToString(), out int days))   // 파싱 안전 처리
                settingsManager.InfoRetentionDays = days;
        }

        private void chk_DebugMode_CheckedChanged(object sender, EventArgs e)
        {
            bool isDebug = chk_DebugMode.Checked;

            // ① Settings.ini 동기화
            settingsManager.IsDebugMode = isDebug;

            // ② LogManager 전체에 반영
            LogManager.GlobalDebugEnabled = isDebug;

            // ③ MainForm 알림
            DebugModeChanged?.Invoke(isDebug);
        }
    }
}
