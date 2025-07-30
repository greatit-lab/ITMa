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
        private const string OptSection          = "Option";
        private const string Key_PerfLog         = "EnablePerfoLog";
        private const string Key_InfoAutoDel     = "EnableInfoAutoDel";
        private const string Key_InfoRetention   = "InfoRetentionDays";

        private readonly SettingsManager settingsManager;

        public event Action<bool> DebugModeChanged;

        public ucOptionPanel(SettingsManager settings)
        {
            this.settingsManager = settings ?? throw new ArgumentNullException(nameof(settings));
            InitializeComponent();

            /* 1) Retention 콤보박스 고정 값 • DropDownList */
            cb_info_Retention.Items.Clear();                                       // [추가]
            cb_info_Retention.Items.AddRange(new object[] { "1", "3", "5" });      // [추가]
            cb_info_Retention.DropDownStyle = ComboBoxStyle.DropDownList;          // [추가]
    
            /* 2) UI 기본 비활성화 */
            UpdateRetentionControls(false);                                        // [추가]
    
            /* 3) 이벤트 연결 */
            chk_PerfoMode.CheckedChanged += chk_PerfoMode_CheckedChanged;          // [추가]
            chk_infoDel.CheckedChanged   += chk_infoDel_CheckedChanged;            // [추가]
            cb_info_Retention.SelectedIndexChanged += cb_info_Retention_SelectedIndexChanged; // [추가]
    
            /* 4) Settings.ini → UI 복원 */
            LoadOptionSettings(); 
        }

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

        private void UpdateRetentionControls(bool enabled)                         // [추가]
        {
            label3.Enabled           = enabled;
            cb_info_Retention.Enabled = enabled;
            label4.Enabled           = enabled;
    
            if (!enabled) cb_info_Retention.SelectedIndex = -1;  // 체크 해제 시 초기화
        }

        private void chk_PerfoMode_CheckedChanged(object sender, EventArgs e)      // [수정]
        {
            bool enable = chk_PerfoMode.Checked;
    
            PerformanceMonitor.Instance.StartSampling();
            PerformanceMonitor.Instance.SetFileLogging(enable);
    
            settingsManager.IsPerformanceLogging = enable;                 // 기존 프로퍼티
            settingsManager.SetValueToSection(OptSection,                  // INI 동기화
                                              Key_PerfLog,
                                              enable ? "1" : "0");         // [추가]
        }

        private void chk_infoDel_CheckedChanged(object sender, EventArgs e)        // [추가]
        {
            bool enable = chk_infoDel.Checked;
            UpdateRetentionControls(enable);
    
            settingsManager.SetValueToSection(OptSection, Key_InfoAutoDel, enable ? "1" : "0");
    
            if (!enable)
                settingsManager.SetValueToSection(OptSection, Key_InfoRetention, ""); // Retention 초기화
        }

        private void cb_info_Retention_SelectedIndexChanged(object sender, EventArgs e) // [추가]
        {
            if (cb_info_Retention.SelectedItem is null) return;
    
            string days = cb_info_Retention.SelectedItem.ToString();
            settingsManager.SetValueToSection(OptSection, Key_InfoRetention, days);
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
