// Services\PerformanceDbWriter.cs
using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using ITM_Agent.Services;   // PerformanceMonitor

namespace ITM_Agent.Services
{
    /// <summary>
    /// PerformanceMonitor 의 Metric 을 버퍼링하여
    /// eqp_perf 테이블로 60건/30초 단위 배치 INSERT
    /// </summary>
    public sealed class PerformanceDbWriter
    {
        /* ---------- 필드 ---------- */
        private readonly string eqpid;
        private readonly List<Metric> buf = new List<Metric>(capacity: 1000);
        private readonly Timer timer;
        private readonly object sync = new object();
        private const int BULK = 60;
        private const int FLUSH_MS = 30_000;

        /* ---------- 생성 & 소멸 ---------- */
        private PerformanceDbWriter(string eqpid)
        {
            this.eqpid = eqpid;
        
            /*──────── 기존: private 필드 직접 접근 [삭제]────────
            PerformanceMonitor.Instance.sampler.OnSample += OnSample;
            ─────────────────────────────────────────────*/
        
            PerformanceMonitor.Instance.RegisterConsumer(OnSample);      // [수정]
        
            timer = new Timer(_ => Flush(), null, FLUSH_MS, FLUSH_MS);
        }

        private static PerformanceDbWriter current;

        public static void Start(string eqpid)
        {
            if (current != null) return;               // 이미 실행 중
            PerformanceMonitor.Instance.Start();       // 모니터 시작
            current = new PerformanceDbWriter(eqpid);
        }

        public static void Stop()
        {
            if (current == null) return;
        
            PerformanceMonitor.Instance.Stop();
            current.Flush();
            current.timer.Dispose();
        
            /*──────── 기존: private 필드 직접 접근 [삭제]────────
            PerformanceMonitor.Instance.sampler.OnSample -= current.OnSample;
            ─────────────────────────────────────────────*/
        
            PerformanceMonitor.Instance.UnregisterConsumer(current.OnSample); // [수정]
            current = null;
        }

        /* ---------- 콜백 ---------- */
        private void OnSample(Metric m)
        {
            lock (sync)
            {
                buf.Add(m);
                if (buf.Count >= BULK)
                    Flush();
            }
        }

        private void Flush()
        {
            /* ① 버퍼 스냅샷 */
            List<Metric> batch;
            lock (sync)
            {
                if (buf.Count == 0) return;
                batch = new List<Metric>(buf);
                buf.Clear();
            }
        
            /* ② 연결 문자열 */
            string cs;
            try     { cs = DatabaseInfo.CreateDefault().GetConnectionString(); }
            catch   { logger.LogError("[Perf] ConnString 실패"); return; }
        
            /* ③ 배치 INSERT */
            try
            {
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText =
                          "INSERT INTO eqp_perf (eqpid, ts, cpu_usage, mem_usage) " +
                          "VALUES (@eqp, @ts, @cpu, @mem) " +
                          "ON CONFLICT (eqpid, ts) DO NOTHING;";
        
                        var pEqp = cmd.Parameters.Add("@eqp", NpgsqlTypes.NpgsqlDbType.Varchar);
                        var pTs  = cmd.Parameters.Add("@ts",  NpgsqlTypes.NpgsqlDbType.Timestamp); // [수정]
                        var pCpu = cmd.Parameters.Add("@cpu", NpgsqlTypes.NpgsqlDbType.Real);
                        var pMem = cmd.Parameters.Add("@mem", NpgsqlTypes.NpgsqlDbType.Real);
        
                        foreach (var m in batch)
                        {
                            /* Eqpid 접두어 “Eqpid: ” 제거 후 삽입 */
                            pEqp.Value = eqpid.Replace("Eqpid:", "", StringComparison.OrdinalIgnoreCase)
                                              .Trim();
        
                            /* 타임스탬프 → 로컬·밀리초 3자리로 절단 */
                            var local = m.Timestamp.ToLocalTime();
                            var truncated = new DateTime(local.Year, local.Month, local.Day,
                                                         local.Hour, local.Minute, local.Second,
                                                         local.Millisecond, DateTimeKind.Unspecified);
                            pTs.Value = truncated;
        
                            /* CPU·메모리 2 자리 반올림 */
                            pCpu.Value = Math.Round(m.Cpu, 2);
                            pMem.Value = Math.Round(m.Mem, 2);
        
                            cmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[Perf] Batch INSERT 실패: {ex.Message}");
            }
        }

    }
}
