// Library\IOnto_PrealignData.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ConnectInfo;
using Npgsql;

namespace PrealignDataLib
{
    /*──────────────────────── Logger ────────────────────────*/
    internal static class SimpleLogger
    {
        private static readonly object _sync = new object();
        private static readonly string _dir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        private static string PathOf(string sfx) =>
            Path.Combine(_dir, $"{DateTime.Now:yyyyMMdd}_{sfx}.log");

        private static void Write(string s, string m)
        {
            lock (_sync)
            {
                Directory.CreateDirectory(_dir);
                string line =
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [Prealign] {m}{Environment.NewLine}";
                try { File.AppendAllText(PathOf(s), line, Encoding.UTF8); } catch { }
            }
        }
        public static void Event(string m) => Write("event", m);
        public static void Error(string m) => Write("error", m);
        public static void Debug(string m) => Write("debug", m);
    }

    /*──────────────────────── Interface ─────────────────────*/
    public interface IOnto_PrealignData
    {
        string PluginName { get; }
        void ProcessAndUpload(string filePath, object arg1 = null, object arg2 = null);
        void StartWatch(string folderPath);
        void StopWatch();
    }

    /*──────────────────────── Implementation ────────────────*/
    public class Onto_PrealignData : IOnto_PrealignData
    {
        /* 상태 · 상수 */
        private static readonly TimeSpan WINDOW = TimeSpan.FromHours(72);
        private static readonly Dictionary<string, DateTime> _lastTs =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);     // 마지막 ts
        private static readonly Dictionary<string, long> _lastLen =
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);        // 파일 길이

        private FileSystemWatcher _fw;
        private DateTime _lastEvt = DateTime.MinValue;   // debounce

        private readonly string _pluginName;
        public  string  PluginName => _pluginName;

        /* 정적 ctor : CP949 등록 */
        static Onto_PrealignData()
        {
#if NETCOREAPP || NET5_0_OR_GREATER
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

        /* ctor : PluginName 동적 결정 */
        public Onto_PrealignData()
        {
            _pluginName = Assembly.GetExecutingAssembly().GetName().Name; // 기본 DLL 이름
            string ini = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini");
            if (File.Exists(ini))
                foreach (string ln in File.ReadLines(ini))
                    if (ln.Trim().StartsWith("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = ln.IndexOf('=');
                        if (idx > 0)
                        {
                            string v = ln.Substring(idx + 1).Trim();
                            if (v.Length > 0) _pluginName = v;
                        }
                    }
        }

        /*──────────────── Folder Watch ─────────────────────*/
        public void StartWatch(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                SimpleLogger.Error("watch path invalid: " + folder);
                return;
            }

            StopWatch();     // 중복 방지

            _fw = new FileSystemWatcher(folder)
            {
                NotifyFilter =
                    NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                Filter               = "PreAlignLog.dat",
                IncludeSubdirectories= false,
                EnableRaisingEvents  = true
            };
            _fw.Created += OnChanged;
            _fw.Changed += OnChanged;

            SimpleLogger.Event("Watcher started ▶ " + folder);
        }

        public void StopWatch()
        {
            if (_fw == null) return;
            _fw.EnableRaisingEvents = false;
            _fw.Dispose();
            _fw = null;
            SimpleLogger.Event("Watcher stopped");
        }

        /*──────────────── File 이벤트 처리 ─────────────────*/
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            /* Debounce: 0.3 초 */
            if ((DateTime.Now - _lastEvt).TotalMilliseconds < 300) return;
            _lastEvt = DateTime.Now;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                for (int i = 0; i < 3; i++)                 // 잠금 재시도
                {
                    try
                    {
                        Thread.Sleep(400);                 // 파일 닫히길 대기

                        long prevLen = _lastLen.ContainsKey(e.FullPath) ? _lastLen[e.FullPath] : 0;
                        long currLen = new FileInfo(e.FullPath).Length;
                        if (currLen == prevLen) return;    // 길이 변화 없음 → 새 로그 없음

                        string eqpid = GetEqpid("Settings.ini");

                        BackupFile(e.FullPath);
                        ProcessIncremental(e.FullPath, eqpid, prevLen); // [추가] 증분

                        _lastLen[e.FullPath] = currLen;    // 길이 갱신
                        return;
                    }
                    catch (IOException) { Thread.Sleep(400); }
                    catch (Exception ex)
                    {
                        SimpleLogger.Error("OnChanged EX " + ex.Message);
                        return;
                    }
                }
            });
        }

        /*──────────────── Backup ───────────────────────────*/
        private void BackupFile(string src)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                          "lb_BaseFolder", "PreAlign");
                Directory.CreateDirectory(dir);
                string dst = Path.Combine(dir, DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".dat");
                File.Copy(src, dst, true);
            }
            catch (Exception ex) { SimpleLogger.Debug("backup fail: " + ex.Message); }
        }

        /*──────────────── 증분 처리 ───────────────────────*/
        private void ProcessIncremental(string path, string eqpid, long prevLen)   // [추가]
        {
            var rows = new List<Tuple<decimal, decimal, decimal, DateTime>>();
            var rex  = new Regex(
                @"Xmm\s*([-\d.]+)\s*Ymm\s*([-\d.]+)\s*Notch\s*([-\d.]+)\s*Time\s*([\d\-:\s]+)",
                RegexOptions.IgnoreCase);

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(prevLen, SeekOrigin.Begin);                    // 추가분만
                using (var sr = new StreamReader(fs, Encoding.GetEncoding(949)))
                {
                    string added = sr.ReadToEnd();
                    foreach (Match m in rex.Matches(added))
                    {
                        DateTime ts;
                        bool ok = DateTime.TryParseExact(
                                     m.Groups[4].Value.Trim(),
                                     new[] { "MM-dd-yy HH:mm:ss", "M-d-yy HH:mm:ss" },
                                     CultureInfo.InvariantCulture,
                                     DateTimeStyles.None,
                                     out ts) ||
                                  DateTime.TryParse(m.Groups[4].Value.Trim(), out ts);
                        if (!ok) continue;

                        decimal x, y, n;
                        if (decimal.TryParse(m.Groups[1].Value, out x) &&
                            decimal.TryParse(m.Groups[2].Value, out y) &&
                            decimal.TryParse(m.Groups[3].Value, out n))
                        {
                            rows.Add(Tuple.Create(x, y, n, ts));
                        }
                    }
                }
            }

            if (rows.Count > 0)
                InsertRows(rows, eqpid);
        }

        /*──────────────── 전체 파일 재처리 (수동 호출용) ───*/
        public void ProcessAndUpload(string filePath, object arg1 = null, object arg2 = null)
        {
            if (!File.Exists(filePath)) { SimpleLogger.Debug("no file"); return; }
            if (!WaitReady(filePath))   { SimpleLogger.Debug("lock skip"); return; }

            string eqpid = GetEqpid(arg1 as string ?? "Settings.ini");
            try { ProcessCore(filePath, eqpid); }
            catch (Exception ex) { SimpleLogger.Error("EX " + ex.Message); }
        }

        private bool WaitReady(string p, int retry = 6, int delay = 400)
        {
            for (int i = 0; i < retry; i++)
            {
                try { using (File.Open(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) return true; }
                catch (IOException) { Thread.Sleep(delay); }
            }
            return false;
        }

        /*──────────────── 전체 파일 처리 (초기 적재) ───────*/
        private void ProcessCore(string file, string eqpid)
        {
            string[] lines = ReadAllText(file)
                             .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            DateTime now       = DateTime.Now;
            DateTime winStart  = now - WINDOW;

            var rex = new Regex(
                @"Xmm\s*([-\d.]+)\\s*Ymm\\s*([-\d.]+)\\s*Notch\\s*([-\d.]+)\\s*Time\\s*([\\d\\-:\\s]+)",
                RegexOptions.IgnoreCase);

            var rows = new List<Tuple<decimal, decimal, decimal, DateTime>>();

            foreach (string ln in lines)
            {
                Match m = rex.Match(ln);
                if (!m.Success) continue;

                DateTime ts;
                bool ok = DateTime.TryParseExact(
                             m.Groups[4].Value.Trim(),
                             new[] { "MM-dd-yy HH:mm:ss", "M-d-yy HH:mm:ss" },
                             CultureInfo.InvariantCulture,
                             DateTimeStyles.None,
                             out ts) ||
                          DateTime.TryParse(m.Groups[4].Value.Trim(), out ts);

                if (!ok || ts < winStart) continue;

                decimal x, y, n;
                if (decimal.TryParse(m.Groups[1].Value, out x) &&
                    decimal.TryParse(m.Groups[2].Value, out y) &&
                    decimal.TryParse(m.Groups[3].Value, out n))
                {
                    rows.Add(Tuple.Create(x, y, n, ts));
                }
            }

            if (rows.Count > 0)
                InsertRows(rows, eqpid);
        }

        /*──────────────── 행 → DB 삽입 공통 ────────────────*/
        private void InsertRows(List<Tuple<decimal, decimal, decimal, DateTime>> rows, string eqpid)
        {
            rows.Sort((a, b) => a.Item4.CompareTo(b.Item4));

            /* diff */
            TimeSpan diff = GetPerfDiff(eqpid);
            if (diff == TimeSpan.Zero)
                diff = GetTimeSyncDiff()
                       .GetValueOrDefault(DateTime.Now - rows[rows.Count - 1].Item4);

            /* DataTable */
            var dt = new DataTable();
            dt.Columns.Add("eqpid",    typeof(string));
            dt.Columns.Add("datetime", typeof(DateTime));
            dt.Columns.Add("xmm",      typeof(decimal));
            dt.Columns.Add("ymm",      typeof(decimal));
            dt.Columns.Add("notch",    typeof(decimal));
            dt.Columns.Add("serv_ts",  typeof(DateTime));

            foreach (var r in rows)
            {
                DateTime serv = (r.Item4 + diff)
                                .AddTicks(-((r.Item4 + diff).Ticks % TimeSpan.TicksPerSecond));
                dt.Rows.Add(eqpid, r.Item4, r.Item1, r.Item2, r.Item3, serv);
            }

            Upload(dt);
            SimpleLogger.Event($"rows={dt.Rows.Count} uploaded");
        }

        /*──────────────── diff 계산 ───────────────────────*/
        private TimeSpan GetPerfDiff(string eqpid)
        {
            try
            {
                string cs = DatabaseInfo.CreateDefault().GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText =
                            "SELECT serv_ts, ts FROM eqp_perf WHERE eqpid=@e " +
                            "ORDER BY serv_ts DESC LIMIT 1";
                        cmd.Parameters.AddWithValue("@e", eqpid);
                        using (var rd = cmd.ExecuteReader())
                        {
                            if (rd.Read()) return rd.GetDateTime(0) - rd.GetDateTime(1);
                        }
                    }
                }
            }
            catch (Exception ex) { SimpleLogger.Debug("perfDiff fail: " + ex.Message); }
            return TimeSpan.Zero;
        }

        private TimeSpan? GetTimeSyncDiff()
        {
            try
            {
                Type tp = Type.GetType("ITM_Agent.Services.TimeSyncProvider, ITM_Agent");
                if (tp != null)
                {
                    object inst = tp.GetProperty("Instance",
                                   BindingFlags.Public | BindingFlags.Static).GetValue(null);
                    return (TimeSpan)tp.GetProperty("Diff").GetValue(inst);
                }
            }
            catch { }
            return null;
        }

        /*──────────────── DB Upload ───────────────────────*/
        private void Upload(DataTable dt)
        {
            string cs = DatabaseInfo.CreateDefault().GetConnectionString();
            using (var conn = new NpgsqlConnection(cs))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    string cols = string.Join(",",
                        dt.Columns.Cast<DataColumn>().Select(c => "\"" + c.ColumnName + "\""));
                    string prm  = string.Join(",",
                        dt.Columns.Cast<DataColumn>().Select(c => "@" + c.ColumnName));

                    cmd.CommandText =
                        $"INSERT INTO public.prealign ({cols}) VALUES ({prm}) " +
                        "ON CONFLICT (eqpid, datetime) DO NOTHING;";

                    foreach (DataColumn c in dt.Columns)
                        cmd.Parameters.Add(new NpgsqlParameter("@" + c.ColumnName, DbType.Object));

                    foreach (DataRow r in dt.Rows)
                    {
                        foreach (DataColumn c in dt.Columns)
                            cmd.Parameters["@" + c.ColumnName].Value = r[c] ?? DBNull.Value;
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
        }

        /*──────────────── Utilities ───────────────────────*/
        private static string ReadAllText(string path, int ms = 30000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                try { return File.ReadAllText(path, Encoding.GetEncoding(949)); }
                catch (IOException)
                {
                    if (sw.ElapsedMilliseconds > ms) throw;
                    Thread.Sleep(250);
                }
            }
        }

        private string GetEqpid(string ini)
        {
            if (!File.Exists(ini)) return string.Empty;
            foreach (string ln in File.ReadLines(ini))
            {
                if (ln.Trim().StartsWith("Eqpid", StringComparison.OrdinalIgnoreCase))
                {
                    int idx = ln.IndexOf('=');
                    if (idx > 0) return ln.Substring(idx + 1).Trim();
                }
            }
            return string.Empty;
        }
    }
}
