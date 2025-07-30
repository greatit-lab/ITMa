// Program.cs
using ITM_Agent.Services;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ITM_Agent
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 1) WinForms 공통 초기화
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            /* ▼ OS UI 언어 확인 후 영문화 ― 추가 코드 --------------------- */
            var ui = CultureInfo.CurrentUICulture;
            if (!ui.Name.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            {
                /* Windows가 한국어 UI가 아니면 en-US 리소스 선택 */
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");   // [추가]
            }

            // 2) AssemblyResolve 훅 - BaseDir\Library 에서 의존 DLL 자동 로드
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string asmFile = new AssemblyName(args.Name).Name + ".dll";
                string libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library", asmFile);
                return File.Exists(libPath) ? Assembly.LoadFrom(libPath) : null;
            };

            // 3) SettingsManager 인스턴스 생성 (프로젝트 원본 코드 그대로 유지)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var settingsManager = new SettingsManager(Path.Combine(baseDir, "Settings.ini"));

            // 4) 메인 폼 실행 (원본 코드 그대로)
            Application.Run(new MainForm(settingsManager));
        }
    }
}
