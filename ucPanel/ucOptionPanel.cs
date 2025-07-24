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
        private readonly SettingsManager settingsManager;

        /// <summary>
        /// MainForm 에 디버그 모드 변경을 알리는 이벤트
        /// </summary>
        public event Action<bool> DebugModeChanged;

        public ucOptionPanel(SettingsManager settings)
        {
            this.settingsManager = settings ?? throw new ArgumentNullException(nameof(settings));

            // 디자이너에서 생성된 UI 초기화
            InitializeComponent();

            // Settings.ini 값으로 체크 초기화
            chk_DebugMode.Checked = settingsManager.IsDebugMode;
        }

        /// <summary>
        /// Debug Mode 체크박스 상태 변경 시 호출
        /// </summary>
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
