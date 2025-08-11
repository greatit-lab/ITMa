// Library\IOnto_WaferFlatData.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;
using ConnectInfo;
using System.Threading;
using ITM_Agent.Services;

namespace Onto_WaferFlatDataLib
{
    /*──────────────────────── Logger (개선 버전) ────────────────────────*/
    internal static class SimpleLogger
    {
        /* (1) ── 전역 Debug 모드 플래그 ──────────────────────────────── */
        private static volatile bool _debugEnabled = false;
        public static void SetDebugMode(bool enable) => _debugEnabled = enable;

        /* (2) ── 공통 경로 & 동시성 ─────────────────────────────────── */
        private static readonly object _sync = new object();
        private static readonly string _logDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        private static string GetPath(string suffix) =>
            Path.Combine(_logDir, $"{DateTime.Now:yyyyMMdd}_{suffix}.log");

        /* (3) ── 노출 API ──────────────────────────────────────────── */
        public static void Event(string msg) => Write("event", msg);
        public static void Error(string msg) => Write("error", msg);
        public static void Debug(string msg)
        {            // Debug 모드일 때만 기록
            if (_debugEnabled) Write("debug", msg);
        }

        /* (4) ── 내부 공통 쓰기 ─────────────────────────────────────── */
        private static void Write(string suffix, string msg)
        {
            lock (_sync)
            {
                if (!Directory.Exists(_logDir))
                    Directory.CreateDirectory(_logDir);

                string filePath = GetPath(suffix);
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} " +
                                  $"[Onto_WaferFlatData] {msg}{Environment.NewLine}";

                const int MAX_RETRY = 3;
                for (int i = 1; i <= MAX_RETRY; i++)
                {
                    try
                    {
                        using (var fs = new FileStream(filePath,
                                  FileMode.OpenOrCreate, FileAccess.Write,
                                  FileShare.ReadWrite))
                        {
                            fs.Seek(0, SeekOrigin.End);
                            using (var sw = new StreamWriter(fs, Encoding.UTF8))
                                sw.Write(line);
                        }
                        return;
                    }
                    catch (IOException) when (i < MAX_RETRY)
                    { Thread.Sleep(250); }
                }
            }
        }
    }
    /*──────────────────────────────────────────────────────────────────*/

    public interface IOnto_WaferFlatData
    {
        string PluginName { get; }
        void ProcessAndUpload(string folderPath, string settingsFilePath = "Settings.ini");
    }

    public class Onto_WaferFlatData : IOnto_WaferFlatData
    {
        private static string ReadAllTextSafe(string path, Encoding enc, int timeoutMs = 30000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, enc))
                    {
                        return sr.ReadToEnd();
                    }
                }
                catch (IOException)
                {
                    if (sw.ElapsedMilliseconds > timeoutMs)
                        throw;
                    System.Threading.Thread.Sleep(500);
                }
            }
        }

        static Onto_WaferFlatData()                           // ← 추가
        {
            // .NET Core/5+/6+/8+ 에서 CP949 등 코드 페이지 인코딩 사용 가능하게 등록
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public string PluginName => "Onto_WaferFlatData";

        #region === 외부 호출 ===
        public void ProcessAndUpload(string filePath)
        {
            /* 1) 첫 호출 기록 */
            SimpleLogger.Event($"ProcessAndUpload(file) ▶ {filePath}");

            /* 2) 파일 준비가 될 때까지 재시도 */
            if (!WaitForFileReady(filePath, 20, 500))
            {
                // 10 초(20×0.5 s) 대기 후에도 준비 안 되면 “Warning”만 남기고 종료
                SimpleLogger.Event($"SKIP – file still not ready ▶ {filePath}");
                return;
            }

            /* 3) 정상 처리 */
            string eqpid = GetEqpidFromSettings("Settings.ini");
            try
            {
                ProcessFile(filePath, eqpid);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"Unhandled EX ▶ {ex.Message}");
            }
        }

        /* UploadPanel 에서 2-파라미터로 호출할 때 */
        public void ProcessAndUpload(string filePath, string settingsPath = "Settings.ini")
        {
            SimpleLogger.Event($"ProcessAndUpload(file,ini) ▶ {filePath}");

            if (!WaitForFileReady(filePath, 20, 500))
            {
                SimpleLogger.Event($"SKIP – file still not ready ▶ {filePath}");
                return;
            }

            string eqpid = GetEqpidFromSettings(settingsPath);
            try
            {
                ProcessFile(filePath, eqpid);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"Unhandled EX ▶ {ex.Message}");
            }
        }
        #endregion

        /* -----------------------------------------------------------------
         * 파일 Ready 헬퍼 – FileWatcherManager·OverrideNamesPanel 에서
         * 이미 사용 중인 패턴을 그대로 차용 (동일 알고리즘) :contentReference[oaicite:1]{index=1}
         * ----------------------------------------------------------------*/
        private bool WaitForFileReady(string path, int maxRetries = 10, int delayMs = 500)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                if (File.Exists(path))
                {
                    /* 공유 잠금이 풀렸는지 확인 */
                    try
                    {
                        using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            return true;            // 파일이 존재하고 잠겨 있지 않음
                        }
                    }
                    catch (IOException)
                    {
                        /* 여전히 잠겨 있음 – 재시도 */
                    }
                }
                Thread.Sleep(delayMs);
            }
            return false;
        }

        private void ProcessFile(string filePath, string eqpid)
        {
            SimpleLogger.Debug($"PARSE ▶ {Path.GetFileName(filePath)}");

            /* ---------------------------------------------------- *
             * 0) 파일 읽기
             * ---------------------------------------------------- */
            string fileContent = ReadAllTextSafe(filePath, Encoding.GetEncoding(949));
            var lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            /* ---------------------------------------------------- *
             * 1) Key–Value 메타 파싱
             * ---------------------------------------------------- */
            var meta = new Dictionary<string, string>();
            foreach (var ln in lines)
            {
                int idx = ln.IndexOf(':');
                if (idx > 0)
                {
                    string key = ln.Substring(0, idx).Trim();
                    string val = ln.Substring(idx + 1).Trim();
                    if (!meta.ContainsKey(key)) meta[key] = val;
                }
            }

            /* 1-1) WaferNo, DateTime 추출 */
            int? waferNo = null;
            if (meta.TryGetValue("Wafer ID", out string waferId))
            {
                var m = Regex.Match(waferId, @"W(\d+)");
                if (m.Success && int.TryParse(m.Groups[1].Value, out int w)) waferNo = w;
            }

            DateTime dtVal = DateTime.MinValue;
            if (meta.TryGetValue("Date and Time", out string dtStr))
                DateTime.TryParse(dtStr, out dtVal);

            /* ---------------------------------------------------- *
             * 2) 헤더 위치 탐색
             * ---------------------------------------------------- */
            int hdrIdx = Array.FindIndex(
                lines,
                l => l.TrimStart().StartsWith("Point#", StringComparison.OrdinalIgnoreCase)
            );
            if (hdrIdx == -1)
            {
                SimpleLogger.Debug("Header NOT FOUND → skip");
                return;
            }

            /* ---------- 2-1) 헤더 정규화 함수 ---------- */
            string NormalizeHeader(string h)
            {
                /* ① 일괄 소문자화 */
                h = h.ToLowerInvariant();

                /* ② (no cal), no cal, no_cal  →  nocal   // [추가] */
                h = Regex.Replace(h, @"\(\s*no\s*cal\.?\s*\)", " nocal ", RegexOptions.IgnoreCase);
                h = Regex.Replace(h, @"\bno[\s_]*cal\b", "nocal", RegexOptions.IgnoreCase);

                /* ③ (cal) → cal (후속 언더바 처리로 ‘_cal’ 유지) */
                h = Regex.Replace(h, @"\(\s*cal\.?\s*\)", " cal ", RegexOptions.IgnoreCase);

                /* ④ 기타 단위·불필요 어구 제거 */
                h = h.Replace("(mm)", "")
                     .Replace("(탆)", "")
                     .Replace("die x", "diex")
                     .Replace("die y", "diey")
                     .Trim();

                /* ⑤ 공백 → _ , 특수문자 삭제 */
                h = Regex.Replace(h, @"\s+", "_");
                h = Regex.Replace(h, @"[#/:\-]", "");

                return h;                  // 예) cu_ht_nocal, point, die_x …
            }

            /* ---------- 2-2) 헤더 리스트 & 매핑 사전 ---------- */
            var headers = lines[hdrIdx].Split(',').Select(NormalizeHeader).ToList();          // [수정]
            var headerIndex = headers.Select((h, idx) => new { h, idx })
                                     .GroupBy(x => x.h)                                           // 중복 제거
                                     .ToDictionary(g => g.Key, g => g.First().idx);               // [수정]

            /* ---------------------------------------------------- *
             * 3) 데이터 파싱
             * ---------------------------------------------------- */
            var rows = new List<Dictionary<string, object>>();
            var intCols = new HashSet<string> { "point", "dierow", "diecol", "dienum", "diepointtag" }; // [수정]

            for (int i = hdrIdx + 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var vals = lines[i].Split(',').Select(v => v.Trim()).ToArray();
                if (vals.Length < headers.Count) continue;

                /* 3-1) 기본 메타 컬럼 */
                var row = new Dictionary<string, object>
                {
                    ["cassettercp"] = meta.TryGetValue("Cassette Recipe Name", out var v1) ? v1 : "",
                    ["stagercp"] = meta.TryGetValue("Stage Recipe Name", out var v2) ? v2 : "",
                    ["stagegroup"] = meta.TryGetValue("Stage Group Name", out var v3) ? v3 : "",
                    ["lotid"] = meta.TryGetValue("Lot ID", out var v4) ? v4 : "",
                    ["waferid"] = waferNo ?? (object)DBNull.Value,
                    ["datetime"] = (dtVal != DateTime.MinValue) ? (object)dtVal : DBNull.Value,
                    ["film"] = meta.TryGetValue("Film Name", out var v5) ? v5 : ""
                };

                /* 3-2) 헤더-값 매핑 */
                int tmpInt; double tmpDbl;
                foreach (var kv in headerIndex)
                {
                    string colName = kv.Key;             // 소문자 snake_case
                    int idx = kv.Value;
                    string valRaw = (idx < vals.Length) ? vals[idx] : "";      // [수정] raw → valRaw

                    if (string.IsNullOrEmpty(valRaw)) { row[colName] = DBNull.Value; continue; }

                    if (intCols.Contains(colName) && int.TryParse(valRaw, out tmpInt))
                        row[colName] = tmpInt;
                    else if (double.TryParse(valRaw, out tmpDbl))
                        row[colName] = tmpDbl;
                    else
                        row[colName] = valRaw;
                }

                rows.Add(row);
            }

            if (rows.Count == 0)
            {
                SimpleLogger.Debug("rows=0 → skip");
                return;
            }

            /* ---------------------------------------------------- *
             * 4) DataTable 생성 & DB 업로드
             * ---------------------------------------------------- */
            DataTable dt = new DataTable();
            foreach (var k in rows[0].Keys) dt.Columns.Add(k, typeof(object));
            dt.Columns.Add("eqpid", typeof(string));

            foreach (var r in rows)
            {
                var dr = dt.NewRow();
                foreach (var k in r.Keys) dr[k] = r[k] ?? DBNull.Value;
                dr["eqpid"] = eqpid;
                dt.Rows.Add(dr);
            }

            UploadToSQL(dt, Path.GetFileName(filePath));
            SimpleLogger.Event($"{Path.GetFileName(filePath)} ▶ rows={dt.Rows.Count}");

            try { File.Delete(filePath); } catch { /* ignore */ }
        }

        #region === DB Upload ===
        private void UploadToSQL(DataTable dt, string srcFile)
        {
            // (상단 생략 없음: 전체 메서드)
            if (!dt.Columns.Contains("serv_ts"))
                dt.Columns.Add("serv_ts", typeof(DateTime));
            if (!dt.Columns.Contains("eqpid"))
                dt.Columns.Add("eqpid", typeof(string));

            foreach (DataRow r in dt.Rows)
            {
                if (r["datetime"] != DBNull.Value)
                {
                    DateTime ts = (DateTime)r["datetime"];
                    r["serv_ts"] = ITM_Agent.Services.TimeSyncProvider.Instance.ToSynchronizedKst(ts);
                }
                else r["serv_ts"] = DBNull.Value;
            }

            var dbInfo = DatabaseInfo.CreateDefault();
            using (var conn = new NpgsqlConnection(dbInfo.GetConnectionString()))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    var cols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

                    // PostgreSQL은 대소문자/예약어 충돌 방지를 위해 "컬럼" 을 큰따옴표로 감쌉니다
                    string colList = string.Join(",", cols.Select(c => $"\"{c}\""));
                    string paramList = string.Join(",", cols.Select(c => "@" + c));

                    // [수정] 테이블명: public.wf_flat → public.plg_wf_flat
                    string sql = $"INSERT INTO public.plg_wf_flat ({colList}) VALUES ({paramList});";

                    using (var cmd = new NpgsqlCommand(sql, conn, tx))
                    {
                        foreach (var c in cols)
                            cmd.Parameters.Add(new NpgsqlParameter("@" + c, DbType.Object));

                        int ok = 0;
                        try
                        {
                            foreach (DataRow r in dt.Rows)
                            {
                                foreach (var c in cols)
                                    cmd.Parameters["@" + c].Value = r[c] ?? DBNull.Value;

                                cmd.ExecuteNonQuery();
                                ok++;
                            }
                            tx.Commit();
                            SimpleLogger.Debug($"DB OK ▶ {ok} rows");
                        }
                        catch (PostgresException pex) when (pex.SqlState == "23505")
                        {
                            tx.Rollback();
                            SimpleLogger.Debug($"Duplicate entry skipped ▶ {pex.Message}");
                            SimpleLogger.Event($"동일한 데이터가 이미 등록되어 업로드가 생략되었습니다 ▶ {srcFile}");
                        }
                        catch (PostgresException pex)
                        {
                            tx.Rollback();
                            var sb = new StringBuilder()
                                .AppendLine($"PG CODE = {pex.SqlState}")
                                .AppendLine($"Message  = {pex.Message}")
                                .AppendLine("SQL      = " + sql);
                            foreach (NpgsqlParameter p in cmd.Parameters)
                                sb.AppendLine($"{p.ParameterName} = {p.Value}");
                            SimpleLogger.Error("DB FAIL ▶ " + sb);
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            SimpleLogger.Error("DB FAIL ▶ " + ex);
                        }
                    }
                }
            }
        }
        #endregion

        #region === Eqpid 읽기 ===
        private string GetEqpidFromSettings(string iniPath)
        {
            if (!File.Exists(iniPath)) return "";
            foreach (var line in File.ReadLines(iniPath))
            {
                if (line.Trim().StartsWith("Eqpid", StringComparison.OrdinalIgnoreCase))
                {
                    int idx = line.IndexOf('=');
                    if (idx > 0) return line.Substring(idx + 1).Trim();
                }
            }
            return "";
        }
        #endregion
    }
}
