// Program.cs
using ITM_Agent.Services;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;

namespace ITM_Agent
{
    internal static class Program
    {
        // ▼▼▼ [추가] Mutex 객체와 고유 이름 선언 ▼▼▼
        private static Mutex mutex = null;
        private const string appGuid = "c0a76b5a-12ab-45c5-b9d9-d693faa6e7b9"; // 다른 프로그램과 절대 겹치지 않을 고유 ID

        [STAThread]
        static void Main()
        {
            // ▼▼▼ [추가] 중복 실행 확인 로직 ▼▼▼
            mutex = new Mutex(true, appGuid, out bool createdNew);

            if (!createdNew)
            {
                // 이미 뮤텍스가 생성되어 있다면(프로그램이 실행 중이라면)
                MessageBox.Show("ITM Agent가 이미 실행 중입니다.", "실행 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return; // 프로그램 종료
            }
            // ▲▲▲ [추가] 중복 실행 확인 로직 끝 ▲▲▲


            // 1) WinForms 공통 초기화 (기존 코드)
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            /* OS UI 언어 확인 후 영문화 ― 추가 코드 (기존 코드) */
            var ui = CultureInfo.CurrentUICulture;
            if (!ui.Name.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            }

            // 2) AssemblyResolve 훅 (기존 코드)
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string asmFile = new AssemblyName(args.Name).Name + ".dll";
                string libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library", asmFile);
                return File.Exists(libPath) ? Assembly.LoadFrom(libPath) : null;
            };

            // 3) SettingsManager 인스턴스 생성 (기존 코드)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var settingsManager = new SettingsManager(Path.Combine(baseDir, "Settings.ini"));

            // 4) 메인 폼 실행 (기존 코드)
            Application.Run(new MainForm(settingsManager));

            // ▼▼▼ [추가] 프로그램 종료 시 Mutex 해제 ▼▼▼
            mutex.ReleaseMutex();
        }
    }
}
