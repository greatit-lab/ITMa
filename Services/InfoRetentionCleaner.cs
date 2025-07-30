// Services\InfoRetentionCleaner.cs
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

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
        private static readonly Regex TsRegex = new Regex(@"^(?<ts>\d{8}_\d{6})_", RegexOptions.Compiled);
        //private const int SCAN_INTERVAL_MS = 60 * 60 * 1000; // 1 시간
        private const int SCAN_INTERVAL_MS = 2 * 60 * 1000;   // Test

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
        
            string baseFolder  = settings.GetBaseFolder();
            if (string.IsNullOrEmpty(baseFolder)) return;
            string baselineDir = Path.Combine(baseFolder, "Baseline");
            if (!Directory.Exists(baselineDir)) return;
        
            DateTime now = DateTime.Now;
        
            foreach (string file in Directory.GetFiles(baselineDir, "*.info"))
            {
                string name = Path.GetFileName(file);
                Match  m    = TsRegex.Match(name);
                if (!m.Success) continue;
        
                if (!DateTime.TryParseExact(m.Groups["ts"].Value,
                                            "yyyyMMdd_HHmmss",
                                            CultureInfo.InvariantCulture,
                                            DateTimeStyles.None,
                                            out DateTime ts)) continue;
        
                if ((now - ts).TotalDays < days) continue;
        
                /* ───────── 파일 삭제 로직 ───────── */
                bool deleted = false;                                              // [추가]
                try
                {
                    if (!File.Exists(file))
                    {   // 다른 스레드/프로세스가 이미 삭제
                        log.LogEvent($"[InfoCleaner] Skip (not found): {name}");   // [수정]
                        continue;
                    }
        
                    /* 읽기 전용 해제 시도 */
                    var attrs = File.GetAttributes(file);                          // [추가]
                    if ((attrs & FileAttributes.ReadOnly) != 0)                    // [추가]
                        File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);// [추가]
        
                    File.Delete(file);                                             // [수정]
                    deleted = true;                                                // [추가]
                }
                catch (UnauthorizedAccessException uaEx)                           // [추가]
                {
                    /* 권한/속성 문제 → 한 번 더 속성 해제 후 재시도 */
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);           // [추가]
                        File.Delete(file);                                         // [추가]
                        deleted = true;                                            // [추가]
                    }
                    catch (Exception ex2)
                    {
                        log.LogError($"[InfoCleaner] Delete fail {name} → {ex2.Message}");
                    }
                }
                catch (FileNotFoundException)
                {
                    log.LogEvent($"[InfoCleaner] Already removed: {name}");        // [수정]
                    deleted = true;                                                // [추가]
                }
                catch (Exception ex)
                {
                    log.LogError($"[InfoCleaner] Delete fail {name} → {ex.Message}");
                }
        
                /* 최종 결과 기록 */
                if (deleted)
                    log.LogEvent($"[InfoCleaner] Deleted: {name}");                // [추가]
            }
        }
        public void Dispose() => timer?.Dispose();
    }
}
