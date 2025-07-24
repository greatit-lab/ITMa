// Library\IOnto_WaferFlatData.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using ConnectInfo;
using System.Threading;

namespace Onto_WaferFlatDataLib
{
    /*──────────────────────── Logger (개선 버전) ────────────────────────*/
    internal static class SimpleLogger
    {
        /* (1) ── 전역 Debug 모드 플래그 ──────────────────────────────── */
        private static volatile bool _debugEnabled = false;
        public  static void  SetDebugMode(bool enable) => _debugEnabled = enable;

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
                string line     = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} " +
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
                    {   Thread.Sleep(250); }
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
                    using (var fs = new FileStream(path, FileMode.Open,
                                                   FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, enc))
                        return sr.ReadToEnd();
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
            return false;                            // 최종 실패
        }
        
        private void ProcessFile(string filePath, string eqpid)
        {
            SimpleLogger.Debug($"PARSE ▶ {Path.GetFileName(filePath)}");
        
            /* ---------------------------------------------------- *
             * 0) 파일 읽기
             * ---------------------------------------------------- */
            string raw   = ReadAllTextSafe(filePath, Encoding.GetEncoding(949)); // CP949
            var    lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        
            /* ---------------------------------------------------- *
             * 1) Key–Value 메타 영역 파싱
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
            int hdrIdx = Array.FindIndex(lines, l =>
                         l.TrimStart().StartsWith("Point#", StringComparison.OrdinalIgnoreCase));
            if (hdrIdx == -1)
            {
                SimpleLogger.Error("Header NOT FOUND → skip");
                return;
            }
        
            /* 2-1) 헤더 정규화 함수 */
            string NormalizeHeader(string h)
            {
                // ① (no Cal), (no cal), (no Cal.), (no cal.) 등을 대소문자 구분 없이 _noCal 로 치환
                h = Regex.Replace(
                        h,
                        @"\(\s*no\s*cal\.?\s*\)",  // 괄호 안 no cal + 선택적 마침표
                        "_noCal",
                        RegexOptions.IgnoreCase
                    )
                    // ② (Cal)은 기존대로 처리
                    .Replace("(Cal)", "_CAL")
                    .Replace("(mm)", "")
                    .Replace("(탆)", "")
                    .Replace("Die X", "DieX")
                    .Replace("Die Y", "DieY")
                    .Trim();
            
                // ③ 공백·특수문자 처리
                h = Regex.Replace(h, @"\s+", " ");    // 다중 공백 → 단일 공백
                h = h.Replace(" ", "_");              // 공백 → 밑줄
                h = Regex.Replace(h, @"[#/:\-]", ""); // #, /, :, - 제거
            
                return h;
            }
        
            /* 2-2) 헤더 리스트 & 매핑 사전 */
            var headers      = lines[hdrIdx].Split(',').Select(NormalizeHeader).ToList();
            var headerIndex  = headers.Select((h, idx) => new { h, idx })
                                      .ToDictionary(x => x.h, x => x.idx);
        
            /* ---------------------------------------------------- *
             * 3) 데이터 파싱
             * ---------------------------------------------------- */
            var rows = new List<Dictionary<string, object>>();
        
            for (int i = hdrIdx + 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
        
                var vals = lines[i].Split(',').Select(v => v.Trim()).ToArray();
                if (vals.Length < headers.Count) continue;
        
                /* 3-1) 기본 메타 컬럼 */
                var row = new Dictionary<string, object>
                {
                    ["CassetteRCP"] = meta.ContainsKey("Cassette Recipe Name") ? meta["Cassette Recipe Name"] : "",
                    ["StageRCP"]    = meta.ContainsKey("Stage Recipe Name")    ? meta["Stage Recipe Name"]    : "",
                    ["StageGroup"]  = meta.ContainsKey("Stage Group Name")     ? meta["Stage Group Name"]     : "",
                    ["LotID"]       = meta.ContainsKey("Lot ID")               ? meta["Lot ID"]               : "",
                    ["WaferID"]     = waferNo ?? (object)DBNull.Value,
                    ["DateTime"]    = (dtVal != DateTime.MinValue) ? (object)dtVal : DBNull.Value,
                    ["Film"]        = meta.ContainsKey("Film Name") ? meta["Film Name"] : ""
                };
        
                /* ---------- 🛠 OLD 인덱스 고정 로직 (주석) ----------
                int tmpInt; double tmpDbl;
                row["Point"] = (vals.Length > 0 && int.TryParse(vals[0], out tmpInt)) ? (object)tmpInt : DBNull.Value;
                row["MSE"]   = (vals.Length > 1 && double.TryParse(vals[1], out tmpDbl)) ? (object)tmpDbl : DBNull.Value;
                for (int col = 2; col < headers.Count && col < vals.Length; col++) { … }
                ---------------------------------------------------- */
        
                /* ---------- ✅ NEW 헤더명 매핑 로직 ---------- */
                int tmpInt; double tmpDbl;
                foreach (var kv in headerIndex)   // kv.Key = 컬럼명, kv.Value = 인덱스
                {
                    string colName = kv.Key;
                    int    idx     = kv.Value;
                    string rawVal  = (idx < vals.Length) ? vals[idx] : "";
        
                    if (string.IsNullOrEmpty(rawVal))
                    {
                        row[colName] = DBNull.Value;
                        continue;
                    }
        
                    /* 숫자형/문자형 판단 */
                    if (new[] { "Point", "DieRow", "DieCol", "DieNum", "DiePointTag" }.Contains(colName)
                        && int.TryParse(rawVal, out tmpInt))
                    {
                        row[colName] = tmpInt;
                    }
                    else if (double.TryParse(rawVal, out tmpDbl))
                    {
                        row[colName] = tmpDbl;
                    }
                    else
                    {
                        row[colName] = rawVal;   // 문자형
                    }
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
            dt.Columns.Add("Eqpid", typeof(string));
        
            foreach (var r in rows)
            {
                var dr = dt.NewRow();
                foreach (var k in r.Keys) dr[k] = r[k] ?? DBNull.Value;
                dr["Eqpid"] = eqpid;
                dt.Rows.Add(dr);
            }
        
            // ★ 수정 : 파일명을 함께 전달
            UploadToMySQL(dt, Path.GetFileName(filePath));
        
            SimpleLogger.Event($"{Path.GetFileName(filePath)} ▶ rows={dt.Rows.Count}");
            try { File.Delete(filePath); } catch { /* ignore */ }
        }
        
        #region === DB Upload ===
        private void UploadToMySQL(DataTable dt, string srcFile)
        {
            var dbInfo = DatabaseInfo.CreateDefault();
            using (var conn = new MySqlConnection(dbInfo.GetConnectionString()))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    var cols = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
                    string sql = $"INSERT INTO wf_flat ({string.Join(",", cols)}) " +
                                 $"VALUES ({string.Join(",", cols.Select(c => "@" + c))})";
        
                    using (var cmd = new MySqlCommand(sql, conn, tx))
                    {
                        foreach (var c in cols)
                            cmd.Parameters.Add(new MySqlParameter("@" + c, DBNull.Value));
        
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
                        /* ───── 중복 키(1062) 전용 ───── */
                        catch (MySqlException mex) when (mex.Number == 1062)
                        {
                            tx.Rollback();
        
                            // ① 자세한 내용은 Debug 로그
                            SimpleLogger.Debug($"Duplicate entry skipped ▶ {mex.Message}");
        
                            // ② Error 로그는 “업로드 생략” 한 줄만
                            SimpleLogger.Error(
                                $"동일한 데이터가 이미 등록되어 업로드가 생략되었습니다 ▶ {srcFile}");
                        }
                        /* ───── 기타 MySQL 오류 ───── */
                        catch (MySqlException mex)
                        {
                            tx.Rollback();
                            var sb = new StringBuilder()
                                .AppendLine($"MySQL ERRNO={mex.Number}")
                                .AppendLine($"Message={mex.Message}")
                                .AppendLine("SQL=" + sql);
                            foreach (MySqlParameter p in cmd.Parameters)
                                sb.AppendLine($"{p.ParameterName}={p.Value}");
                            SimpleLogger.Error("DB FAIL ▶ " + sb);
                        }
                        /* ───── 그 외 예외 ───── */
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
