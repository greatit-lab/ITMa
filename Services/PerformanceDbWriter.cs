// Services\PerformanceDbWriter.cs
using ConnectInfo;
using ITM_Agent.Services;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        /* LogManager 인스턴스 (Writer 전용) ────────────────────────────────*/
        private static readonly LogManager logger =
            new LogManager(AppDomain.CurrentDomain.BaseDirectory);   // [추가]

        /* ---------- 생성 & 소멸 ---------- */
        private PerformanceDbWriter(string eqpid)
        {
            this.eqpid = eqpid;
            PerformanceMonitor.Instance.RegisterConsumer(OnSample);      // [수정]
        
            timer = new Timer(_ => Flush(), null, FLUSH_MS, FLUSH_MS);
        }

        private static PerformanceDbWriter current;

        public static void Start(string eqpid)
        {
            if (current != null) return;
            
            PerformanceMonitor.Instance.StartSampling();
            current = new PerformanceDbWriter(eqpid);
        }

        public static void Stop()
        {
            if (current == null) return;
        
            PerformanceMonitor.Instance.StopSampling();
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
        
            /* ② 커넥션 문자열 ------------------------------------------- */
            string cs;
            try     { cs = DatabaseInfo.CreateDefault().GetConnectionString(); }
            catch   { logger.LogError("[Perf] ConnString 실패"); return; }
        
            /* ③ 보정량 계산 --------------------------------------------- */
            DateTime serverNow = DateTime.Now;                       // 서버 기준 시각(로컬)
            DateTime tsMax = batch.Max(b => b.Timestamp);            // 버퍼 중 가장 늦은 ts
            TimeSpan diff = serverNow - tsMax;                       // 보정량
            TimeSyncProvider.SetDiff(eqpid, diff);
        
            /* ④ 배치 INSERT --------------------------------------------- */
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
                          "INSERT INTO eqp_perf " +
                          " (eqpid, ts, serv_ts, cpu_usage, mem_usage) " +
                          " VALUES (@eqp, @ts, @srv, @cpu, @mem) " +
                          " ON CONFLICT (eqpid, ts) DO NOTHING;";
        
                        var pEqp = cmd.Parameters.Add("@eqp",  NpgsqlTypes.NpgsqlDbType.Varchar);
                        var pTs  = cmd.Parameters.Add("@ts",   NpgsqlTypes.NpgsqlDbType.Timestamp);
                        var pSrv = cmd.Parameters.Add("@srv",  NpgsqlTypes.NpgsqlDbType.Timestamp);
                        var pCpu = cmd.Parameters.Add("@cpu",  NpgsqlTypes.NpgsqlDbType.Real);
                        var pMem = cmd.Parameters.Add("@mem",  NpgsqlTypes.NpgsqlDbType.Real);
        
                        foreach (var m in batch)
                        {
                            /* eqpid 전처리 */
                            string clean = eqpid.StartsWith("Eqpid:", StringComparison.OrdinalIgnoreCase)
                                           ? eqpid.Substring(6).Trim() : eqpid.Trim();
                            pEqp.Value = clean;
        
                            /* ts (밀리초 절단) */
                            var ts = new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day,
                                                  m.Timestamp.Hour, m.Timestamp.Minute, m.Timestamp.Second);
                            pTs.Value  = ts;
        
                            /* serv_ts = ts + diff, 이후 밀리초 제거 ----------------------- */
                            var srv = TimeSyncProvider.Instance.Apply(ts);              // [추가] ts + diff
                            srv = new DateTime(srv.Year, srv.Month, srv.Day,            // [추가] 밀리초 절단
                                               srv.Hour, srv.Minute, srv.Second);
                            pSrv.Value = srv;
        
                            /* 사용률 */
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
