// Services\InfoRetentionCleaner.cs
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace ITM_Agent.Services
{
    /// <summary>
    /// Baseline 폴더의 *.info 파일을 선택한 보존일수만큼 자동 삭제하고,
    /// 추가로 BaseFolder 하위 폴더의 모든 파일 중 파일명에 날짜/시간 패턴이 있으면
    /// '날짜' 기준으로 보존일수를 넘어선 파일을 자동 삭제합니다.
    /// </summary>
    internal sealed class InfoRetentionCleaner : IDisposable
    {
        private readonly SettingsManager settings;
        private readonly LogManager log;
        private readonly Timer timer;
        private static readonly Regex TsRegex = new Regex(@"^(?<ts>\d{8}_\d{6})_", RegexOptions.Compiled);

        // [추가] 파일명에 포함될 수 있는 날짜/시간 패턴(시간이 있어도 날짜만 사용)
        //   1) yyyyMMdd_HHmmss  (예: 20250711_142530)
        //   2) yyyy-MM-dd       (예: 2025-07-11)
        //   3) yyyyMMdd         (예: 20250711)
        private static readonly Regex RxYmdHms = new Regex(@"(?<!\d)(?<ymd>\d{8})_(?<hms>\d{6})(?!\d)", RegexOptions.Compiled);
        private static readonly Regex RxHyphen = new Regex(@"(?<!\d)(?<date>\d{4}-\d{2}-\d{2})(?!\d)", RegexOptions.Compiled);
        private static readonly Regex RxYmd = new Regex(@"(?<!\d)(?<ymd>\d{8})(?!\d)", RegexOptions.Compiled);

        //private const int SCAN_INTERVAL_MS = 60 * 60 * 1000; // 1 시간
        private const int SCAN_INTERVAL_MS = 5 * 60 * 1000;   // Test (즉시 1회 실행 후 주기 스케줄)

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

            string baseFolder = settings.GetBaseFolder();
            if (string.IsNullOrEmpty(baseFolder)) return;

            string baselineDir = Path.Combine(baseFolder, "Baseline");
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

                if ((now - ts).TotalDays < days) continue;

                bool deleted = false;
                try
                {
                    if (!File.Exists(file))
                    {
                        log.LogEvent($"[InfoCleaner] Already removed: {name}");
                        deleted = true;
                        continue;
                    }

                    // 읽기전용 해제
                    var attrs = File.GetAttributes(file);
                    if ((attrs & FileAttributes.ReadOnly) != 0)
                        File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);

                    File.Delete(file);
                    deleted = true;
                }
                catch (UnauthorizedAccessException)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        deleted = true;
                    }
                    catch (Exception ex2)
                    {
                        log.LogError($"[InfoCleaner] Delete fail {name} → {ex2.Message}");
                    }
                }
                catch (FileNotFoundException)
                {
                    log.LogEvent($"[InfoCleaner] Already removed: {name}");
                    deleted = true;
                }
                catch (DirectoryNotFoundException)
                {
                    // 폴더가 동시에 이동/삭제되어 발생 → 에러 아님
                    log.LogEvent($"[InfoCleaner] Already removed (dir): {name}");
                    deleted = true;
                }
                catch (Exception ex)
                {
                    log.LogError($"[InfoCleaner] Delete fail {name} → {ex.Message}");
                }

                if (deleted)
                    log.LogEvent($"[InfoCleaner] Deleted: {name}");
            }
        }

        // [추가] 하위 폴더(재귀) 스캔 & 삭제
        private void CleanFolderRecursively(string rootDir, int days)
        {
            DateTime today = DateTime.Today;

            foreach (var file in Directory.EnumerateFiles(rootDir, "*.*", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);

                DateTime? d = TryExtractDateFromFileName(name);
                if (!d.HasValue) continue;

                DateTime onlyDate = d.Value.Date;                                     // 시간 무시(날짜 기준)
                if ((today - onlyDate).TotalDays < days) continue;

                TryDelete(file, name);
            }
        }

        // [추가] 파일명에서 날짜를 안전하게 추출 (시간이 있어도 '날짜'만 반환)
        private static DateTime? TryExtractDateFromFileName(string fileName)
        {
            // 1) yyyyMMdd_HHmmss → yyyyMMdd만 추출하여 DateTime 변환
            var m1 = RxYmdHms.Match(fileName);
            if (m1.Success)
            {
                string ymd = m1.Groups["ymd"].Value; // 8자리
                if (DateTime.TryParseExact(ymd, "yyyyMMdd", CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out DateTime d1))
                    return d1.Date;
            }

            // 2) yyyy-MM-dd
            var m2 = RxHyphen.Match(fileName);
            if (m2.Success)
            {
                string s = m2.Groups["date"].Value; // "yyyy-MM-dd"
                if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out DateTime d2))
                    return d2.Date;
            }

            // 3) yyyyMMdd (단독 날짜)
            var m3 = RxYmd.Match(fileName);
            if (m3.Success)
            {
                string ymd = m3.Groups["ymd"].Value; // 8자리
                if (DateTime.TryParseExact(ymd, "yyyyMMdd", CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out DateTime d3))
                    return d3.Date;
            }
            return null;
        }

        // [수정] 공통 삭제 헬퍼 (읽기전용/권한 예외 포함 재시도)
        private void TryDelete(string filePath, string displayName)
        {
            bool deleted = false;
            try
            {
                if (!File.Exists(filePath))
                {   // 다른 스레드/프로세스가 이미 삭제
                    log.LogEvent($"[InfoCleaner] Skip (not found): {displayName}");
                    return;
                }

                // 읽기 전용 해제 시도
                var attrs = File.GetAttributes(filePath);
                if ((attrs & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(filePath, attrs & ~FileAttributes.ReadOnly);

                File.Delete(filePath);
                deleted = true;
            }
            catch (UnauthorizedAccessException)
            {
                try
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                    deleted = true;
                }
                catch (Exception ex2)
                {
                    log.LogError($"[InfoCleaner] Delete fail {displayName} → {ex2.Message}");
                }
            }
            catch (FileNotFoundException)
            {
                log.LogEvent($"[InfoCleaner] Already removed: {displayName}");
                deleted = true;
            }
            catch (Exception ex)
            {
                log.LogError($"[InfoCleaner] Delete fail {displayName} → {ex.Message}");
            }

            if (deleted)
                log.LogEvent($"[InfoCleaner] Deleted: {displayName}");
        }

        public void Dispose() => timer?.Dispose();
    }
}
