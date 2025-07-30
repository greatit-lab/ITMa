// Services\InfoRetentionCleaner.cs
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using ITM_Agent.Services;

namespace ITM_Agent.Services
{
    /// <summary>
    /// Baseline 폴더의 *.info 파일을 선택한 보존일수만큼 자동 삭제한다.
    /// </summary>
    internal sealed class InfoRetentionCleaner : IDisposable
    {
        private readonly SettingsManager settings;
        private readonly LogManager log;
        private readonly Timer timer;
        private static readonly Regex TsRegex = new Regex(@"^(?<ts>\d{8}_\d{6})_",
                                                          RegexOptions.Compiled);
        private const int SCAN_INTERVAL_MS = 60 * 60 * 1000; // 1 시간

        public InfoRetentionCleaner(SettingsManager settingsManager)
        {
            settings = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            log = new LogManager(AppDomain.CurrentDomain.BaseDirectory);
            /* 즉시 1회 실행 후 주기 스케줄 */
            timer = new Timer(_ => Execute(), null, 0, SCAN_INTERVAL_MS);
        }

        private void Execute()
        {
            if (!settings.IsInfoDeletionEnabled) return;
            int days = settings.InfoRetentionDays;
            if (days <= 0) return;

            string baselineDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Baseline");
            if (!Directory.Exists(baselineDir)) return;

            DateTime now = DateTime.Now;
            foreach (string file in Directory.GetFiles(baselineDir, "*.info"))
            {
                string name = Path.GetFileName(file);
                Match m = TsRegex.Match(name);
                if (!m.Success) continue;

                if (!DateTime.TryParseExact(m.Groups["ts"].Value,
                                            "yyyyMMdd_HHmmss",
                                            CultureInfo.InvariantCulture,
                                            DateTimeStyles.None,
                                            out DateTime ts)) continue;

                if ((now - ts).TotalDays >= days)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        log.LogEvent($"[InfoCleaner] Deleted: {name}");
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"[InfoCleaner] Delete fail {name} → {ex.Message}");
                    }
                }
            }
        }

        public void Dispose() => timer?.Dispose();
    }
}
