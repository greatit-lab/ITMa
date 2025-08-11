// Library\IOnto_ErrorData.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Npgsql;
using ConnectInfo;
using ITM_Agent.Services;

namespace ErrorDataLib
{
    /*──────────────────────── Logger ───────────────────────*/
    internal static class SimpleLogger
    {
        private static readonly object _sync = new object();
        private static readonly string _dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static string PathOf(string sfx) { return Path.Combine(_dir, string.Format("{0:yyyyMMdd}_{1}.log", DateTime.Now, sfx)); }

        private static void Write(string s, string m)
        {
            try
            {
                lock (_sync)
                {
                    Directory.CreateDirectory(_dir);
                    var line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [ErrorData] {1}{2}", DateTime.Now, m, Environment.NewLine);
                    File.AppendAllText(PathOf(s), line, Encoding.UTF8);
                }
            }
            catch { /* 로깅 실패는 무시 */ }
        }

        public static void Event(string m) { Write("event", m); }
        public static void Error(string m) { Write("error", m); }
        public static void Debug(string m) { Write("debug", m); }
    }

    /*──────────────────────── Interface ─────────────────────*/
    // [추가] 기존 플러그인들과 동일한 시그니처(호환성 보장)
    public interface IOnto_ErrorData
    {
        string PluginName { get; }
        void ProcessAndUpload(string filePath, object arg1 = null, object arg2 = null);
        void StartWatch(string folderPath);
        void StopWatch();
    }

    /*──────────────────────── Implementation ───────────────*/
    public class Onto_ErrorData : IOnto_ErrorData
    {
        /* 상태값/상수 */
        private FileSystemWatcher _fw;
        private DateTime _lastEvt = DateTime.MinValue;
        private static readonly Dictionary<string, long> _lastLen =
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        private readonly string _pluginName = "Onto_ErrorData";
        public string PluginName { get { return _pluginName; } }

        static Onto_ErrorData()
        {
#if NETCOREAPP || NET5_0_OR_GREATER
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

        #region === Public API ===
        // [추가] ucUploadPanel 호출 규약과 동일한 메서드 시그니처
        public void ProcessAndUpload(string filePath, object arg1 = null, object arg2 = null)
        {
            SimpleLogger.Event("Process ▶ " + filePath);

            // 파일 준비 대기 (잠금 해제까지 재시도)
            if (!WaitForFileReady(filePath, 20, 500))
            {
                SimpleLogger.Event("SKIP – file still not ready ▶ " + filePath);
                return;
            }

            // Eqpid 로드
            string eqpid = GetEqpidFromSettings("Settings.ini");

            try
            {
                ProcessFile(filePath, eqpid);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error("Unhandled EX ▶ " + ex);
            }
        }

        public void StartWatch(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                SimpleLogger.Error("watch path invalid: " + folderPath);
                return;
            }

            StopWatch(); // 기존 인스턴스 정리

            _fw = new FileSystemWatcher(folderPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = "*.log",
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _fw.Created += OnChanged;
            _fw.Changed += OnChanged;

            SimpleLogger.Event("Watcher started ▶ " + folderPath);
        }

        public void StopWatch()
        {
            if (_fw == null) return;

            _fw.EnableRaisingEvents = false;
            _fw.Created -= OnChanged;
            _fw.Changed -= OnChanged;
            _fw.Dispose();
            _fw = null;

            SimpleLogger.Event("Watcher stopped");
        }
        #endregion

        #region === Watcher Internal ===
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            // 간단한 debounce (200ms)
            var now = DateTime.Now;
            if ((now - _lastEvt).TotalMilliseconds < 200) return;
            _lastEvt = now;

            // 파일 길이 변화 없으면 무시 (중복 이벤트 억제)
            try
            {
                if (File.Exists(e.FullPath))
                {
                    long len = new FileInfo(e.FullPath).Length;
                    long prev;
                    if (_lastLen.TryGetValue(e.FullPath, out prev) && prev == len)
                        return;
                    _lastLen[e.FullPath] = len;

                    // 비동기 처리
                    ThreadPool.QueueUserWorkItem(_ => ProcessAndUpload(e.FullPath));
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Error("Watcher EX ▶ " + ex.Message);
            }
        }
        #endregion

        #region === Core ===
        private void ProcessFile(string filePath, string eqpid) // [수정]
        {
            string raw = File.ReadAllText(filePath, Encoding.GetEncoding(949));                   // [유지]
            var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries); // [유지]
            var meta = ParseMeta(lines);                                                          // [유지]
            if (!meta.ContainsKey("EqpId")) meta["EqpId"] = eqpid;                                // [유지]
        
            var infoTable = BuildInfoDataTable(meta);                                            // [유지]
            UploadItmInfoUpsert(infoTable);                                                      // [추가] 장비당 1건 업서트
        
            var errorTable = BuildErrorDataTable(lines, eqpid);                                   // [유지]
            if (errorTable.Rows.Count > 0)
                UploadDataTable(errorTable, "plg_error");                                         // [수정] 테이블명: error → plg_error
        
            SimpleLogger.Event("Done ▶ " + Path.GetFileName(filePath));                           // [유지]
        }

        private void UploadItmInfoUpsert(DataTable dt) // [추가]
        {
            if (dt == null || dt.Rows.Count == 0) return;
        
            var r = dt.Rows[0];
            string cs = DatabaseInfo.CreateDefault().GetConnectionString();
        
            const string SQL = @"
                INSERT INTO public.itm_info
                    (eqpid, system_name, system_model, serial_num, application, version, db_version, customer, ""date"")
                VALUES
                    (@eqpid, @system_name, @system_model, @serial_num, @application, @version, @db_version, @customer, @date)
                ON CONFLICT (eqpid) DO UPDATE
                SET
                    system_name  = EXCLUDED.system_name,
                    system_model = EXCLUDED.system_model,
                    serial_num   = EXCLUDED.serial_num,
                    application  = EXCLUDED.application,
                    version      = EXCLUDED.version,
                    db_version   = EXCLUDED.db_version,
                    customer     = EXCLUDED.customer,
                    ""date""      = EXCLUDED.""date"",
                    serv_ts      = NOW()
                WHERE
                    (itm_info.system_name  IS DISTINCT FROM EXCLUDED.system_name)  OR
                    (itm_info.system_model IS DISTINCT FROM EXCLUDED.system_model) OR
                    (itm_info.serial_num   IS DISTINCT FROM EXCLUDED.serial_num)   OR
                    (itm_info.application  IS DISTINCT FROM EXCLUDED.application)  OR
                    (itm_info.version      IS DISTINCT FROM EXCLUDED.version)      OR
                    (itm_info.db_version   IS DISTINCT FROM EXCLUDED.db_version)   OR
                    (itm_info.customer     IS DISTINCT FROM EXCLUDED.customer)     OR
                    (itm_info.""date""      IS DISTINCT FROM EXCLUDED.""date"");
                ";
        
            using (var conn = new NpgsqlConnection(cs))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(SQL, conn))
                {
                    cmd.Parameters.AddWithValue("@eqpid",       r["eqpid"]        ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@system_name", r["system_name"]  ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@system_model",r["system_model"] ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@serial_num",  r["serial_num"]   ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@application", r["application"]  ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@version",     r["version"]      ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@db_version",  r["db_version"]   ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@customer",    r["customer"]     ?? (object)DBNull.Value);
        
                    object dateParam = DBNull.Value;
                    var dv = r["date"];
                    if (dv != null && dv != DBNull.Value)
                    {
                        DateTime dtParsed;
                        if (DateTime.TryParseExact(dv.ToString(), "yyyy-MM-dd HH:mm:ss",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out dtParsed))
                            dateParam = dtParsed;
                        else
                            dateParam = dv.ToString();
                    }
                    cmd.Parameters.AddWithValue("@date", dateParam);
        
                    cmd.ExecuteNonQuery();
                }
            }
            SimpleLogger.Event("itm_info upsert OK ▶ eqpid=" + r["eqpid"]);
        }

        // 파일 상단의 "키:," 형태 메타데이터 파싱 + DATE 형식 변환
        private Dictionary<string, string> ParseMeta(string[] lines)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                var ln = lines[i];
                int idx = ln.IndexOf(":,");
                if (idx <= 0) continue;

                string key = ln.Substring(0, idx).Trim();
                string val = ln.Substring(idx + 2).Trim();
                if (key.Length == 0) continue;

                // 'EXPORT_TYPE' 은 스킵 (요구사항)                                       // [추가]
                if (string.Equals(key, "EXPORT_TYPE", StringComparison.OrdinalIgnoreCase))
                    continue;

                d[key] = val;
            }

            // DATE: M/d/yyyy H:m:s → yyyy-MM-dd HH:mm:ss                                 // [추가]
            string ds;
            if (d.TryGetValue("DATE", out ds))
            {
                DateTime dt;
                if (DateTime.TryParseExact(ds, "M/d/yyyy H:m:s", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    d["DATE"] = dt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            return d;
        }

        // itm_info DataTable 구성 (컬럼은 소문자 스네이크케이스로 통일)
        private DataTable BuildInfoDataTable(Dictionary<string, string> meta)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "DATE",        "date"        },   // [추가]
                { "SYSTEM_NAME", "system_name" },
                { "SYSTEM_MODEL","system_model"},
                { "SERIAL_NUM",  "serial_num"  },
                { "APPLICATION", "application" },
                { "VERSION",     "version"     },
                { "DB_VERSION",  "db_version"  },
                { "CUSTOMER",    "customer"    },
                { "EqpId",       "eqpid"       }
            };

            var dt = new DataTable();
            foreach (var col in map.Values) dt.Columns.Add(col, typeof(string));

            var row = dt.NewRow();
            foreach (var kv in map)
            {
                string v;
                row[kv.Value] = meta.TryGetValue(kv.Key, out v) ? (object)v : DBNull.Value;
            }
            dt.Rows.Add(row);
            return dt;
        }

        // error DataTable 구성
        private DataTable BuildErrorDataTable(string[] lines, string eqpid)
        {
            var dt = new DataTable();
            dt.Columns.Add("eqpid", typeof(string));              // [추가]
            dt.Columns.Add("error_id", typeof(string));
            dt.Columns.Add("time_stamp", typeof(DateTime));
            dt.Columns.Add("error_label", typeof(string));
            dt.Columns.Add("error_desc", typeof(string));
            dt.Columns.Add("millisecond", typeof(int));
            dt.Columns.Add("extra_message_1", typeof(string));
            dt.Columns.Add("extra_message_2", typeof(string));

            // 예: ERR001, 24-Jul-25 01:23:45 PM, Label, Desc, 123, (Optional Extra ...)
            var rg = new Regex(@"^(?<id>\w+),\s*(?<ts>[^,]+),\s*(?<lbl>[^,]+),\s*(?<desc>[^,]+),\s*(?<ms>\d+)(?:,\s*(?<extra>.*))?", RegexOptions.Compiled);

            for (int i = 0; i < lines.Length; i++)
            {
                var m = rg.Match(lines[i]);
                if (!m.Success) continue;

                var r = dt.NewRow();
                r["eqpid"] = eqpid;
                r["error_id"] = m.Groups["id"].Value.Trim();

                DateTime ts;
                if (DateTime.TryParseExact(m.Groups["ts"].Value.Trim(), "dd-MMM-yy h:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out ts))
                    r["time_stamp"] = ts;

                r["error_label"] = m.Groups["lbl"].Value.Trim();
                r["error_desc"] = m.Groups["desc"].Value.Trim();

                int ms;
                if (int.TryParse(m.Groups["ms"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ms))
                    r["millisecond"] = ms;

                r["extra_message_1"] = m.Groups["extra"].Value.Trim();
                r["extra_message_2"] = ""; // 사용 안 함

                dt.Rows.Add(r);
            }
            return dt;
        }
        #endregion

        #region === DB Helper ===
        // itm_info 변경 여부 판단 (DATE 제외)
        private bool IsInfoChanged(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return false;
            var r = dt.Rows[0];

            string cs = DatabaseInfo.CreateDefault().GetConnectionString();
            const string SQL = @"
SELECT 1 
FROM public.itm_info 
WHERE system_name = @sn 
  AND system_model = @sm 
  AND serial_num   = @snm 
  AND application  = @app 
  AND version      = @ver 
  AND db_version   = @dbv 
  AND customer     = @cust 
  AND eqpid        = @eqp
LIMIT 1;";

            using (var conn = new NpgsqlConnection(cs))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(SQL, conn))
                {
                    cmd.Parameters.AddWithValue("@sn",   r["system_name"] ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@sm",   r["system_model"] ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@snm",  r["serial_num"]   ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@app",  r["application"]  ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ver",  r["version"]      ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@dbv",  r["db_version"]   ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cust", r["customer"]     ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@eqp",  r["eqpid"]        ?? (object)DBNull.Value);

                    object o = cmd.ExecuteScalar();
                    // 기존 행이 없으면 "변경됨"으로 간주 → 업로드
                    return o == null;
                }
            }
        }

        // 공용 업로드 (WaferFlat/Prealign 스타일과 동일한 INSERT 루프)
        private void UploadDataTable(DataTable dt, string tableName)
        {
            if (dt == null || dt.Rows.Count == 0) return;

            string cs = DatabaseInfo.CreateDefault().GetConnectionString();

            using (var conn = new NpgsqlConnection(cs))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    // 1) 컬럼/파라미터 목록 생성
                    var cols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();                   // [추가]
                    string colList   = string.Join(",", cols.Select(c => "\"" + c + "\""));
                    string paramList = string.Join(",", cols.Select(c => "@" + c));

                    string sql = string.Format("INSERT INTO public.{0} ({1}) VALUES ({2});", tableName, colList, paramList);

                    using (var cmd = new NpgsqlCommand(sql, conn, tx))
                    {
                        // 2) 파라미터 미리 준비
                        foreach (var c in cols)
                            cmd.Parameters.Add(new NpgsqlParameter("@" + c, DbType.Object));

                        int ok = 0;
                        try
                        {
                            // 3) DataTable → INSERT 루프
                            foreach (DataRow r in dt.Rows)
                            {
                                foreach (var c in cols)
                                    cmd.Parameters["@" + c].Value = r[c] ?? DBNull.Value;

                                cmd.ExecuteNonQuery();
                                ok++;
                            }
                            tx.Commit();
                            SimpleLogger.Debug(string.Format("DB OK ▶ {0}, rows={1}", tableName, ok));
                        }
                        catch (PostgresException pex) when (pex.SqlState == "23505") // unique_violation
                        {
                            tx.Rollback();
                            SimpleLogger.Event("Duplicate entry skipped ▶ " + tableName);
                        }
                        catch (PostgresException pex)
                        {
                            tx.Rollback();
                            var sb = new StringBuilder()
                                .AppendLine("PG CODE = " + pex.SqlState)
                                .AppendLine("Message  = " + pex.Message)
                                .AppendLine("SQL      = " + sql);
                            foreach (NpgsqlParameter p in cmd.Parameters)
                                sb.AppendLine(p.ParameterName + " = " + (p.Value ?? "NULL"));
                            SimpleLogger.Error("DB FAIL ▶ " + sb.ToString());
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

        #region === Utility ===
        private bool WaitForFileReady(string path, int maxRetries, int delayMs)
        {
            for (int i = 0; i < maxRetries; i++)                                          // [추가]
            {
                if (File.Exists(path))
                {
                    try
                    {
                        using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            return true;             // 잠금 해제 상태
                        }
                    }
                    catch (IOException)
                    {
                        // 여전히 잠겨 있음 → 재시도
                    }
                }
                Thread.Sleep(delayMs);
            }
            return false;                                                                 // [추가]
        }

        private string GetEqpidFromSettings(string iniPath)
        {
            try
            {
                string path = Path.IsPathRooted(iniPath)
                    ? iniPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iniPath);

                if (!File.Exists(path)) return string.Empty;

                foreach (var line in File.ReadLines(path))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("Eqpid", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = trimmed.IndexOf('=');
                        if (idx > 0) return trimmed.Substring(idx + 1).Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Error("GetEqpidFromSettings EX ▶ " + ex.Message);
            }
            return string.Empty;
        }
        #endregion
    }
}
