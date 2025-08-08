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
    public sealed class PerformanceDbWriter
    {
        /* ---------- 필드 ---------- */
        private readonly string eqpid;
        private readonly List<Metric> buf = new List<Metric>(1000);
        private readonly Timer timer;
        private readonly object sync = new object();
        private const int BULK = 60;
        private const int FLUSH_MS = 30_000;
        private static readonly LogManager logger = new LogManager(AppDomain.CurrentDomain.BaseDirectory);
        private readonly EqpidManager eqpidManager; // ★ EqpidManager 필드

        private PerformanceDbWriter(string eqpid, EqpidManager manager) // 생성자
        {
            this.eqpid = eqpid;
            this.eqpidManager = manager ?? throw new ArgumentNullException(nameof(manager)); // null 체크
            PerformanceMonitor.Instance.RegisterConsumer(OnSample);
            timer = new Timer(_ => Flush(), null, FLUSH_MS, FLUSH_MS);
        }

        /* ---------- 생성 & 소멸 ---------- */
        private PerformanceDbWriter(string eqpid)
        {
            this.eqpid = eqpid;
            PerformanceMonitor.Instance.RegisterConsumer(OnSample);      // [수정]

            timer = new Timer(_ => Flush(), null, FLUSH_MS, FLUSH_MS);
        }

        private static PerformanceDbWriter current;

        public static void Start(string eqpid, EqpidManager manager)
        {
            if (current != null) return;
            PerformanceMonitor.Instance.StartSampling();
            current = new PerformanceDbWriter(eqpid, manager);
        }

        public static void Stop()
        {
            if (current == null) return;
            PerformanceMonitor.Instance.StopSampling();
            current.Flush();
            current.timer.Dispose();
            PerformanceMonitor.Instance.UnregisterConsumer(current.OnSample);
            current = null;
        }

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
            List<Metric> batch;
            lock (sync)
            {
                if (buf.Count == 0) return;
                batch = new List<Metric>(buf);
                buf.Clear();
            }

            string cs;
            try { cs = DatabaseInfo.CreateDefault().GetConnectionString(); }
            catch { logger.LogError("[Perf] ConnString 실패"); return; }

            // ★ 이제 eqpidManager는 null이 아니므로 오류가 발생하지 않습니다.
            var sourceZone = eqpidManager.GetTimezoneForEqpid(eqpid);

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

                        var pEqp = cmd.Parameters.Add("@eqp", NpgsqlTypes.NpgsqlDbType.Varchar);
                        var pTs = cmd.Parameters.Add("@ts", NpgsqlTypes.NpgsqlDbType.Timestamp);
                        var pSrv = cmd.Parameters.Add("@srv", NpgsqlTypes.NpgsqlDbType.Timestamp);
                        var pCpu = cmd.Parameters.Add("@cpu", NpgsqlTypes.NpgsqlDbType.Real);
                        var pMem = cmd.Parameters.Add("@mem", NpgsqlTypes.NpgsqlDbType.Real);

                        foreach (var m in batch)
                        {
                            string clean = eqpid.StartsWith("Eqpid:", StringComparison.OrdinalIgnoreCase)
                                           ? eqpid.Substring(6).Trim() : eqpid.Trim();
                            pEqp.Value = clean;

                            var ts = new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day,
                                                  m.Timestamp.Hour, m.Timestamp.Minute, m.Timestamp.Second);
                            pTs.Value = ts;

                            var srv = TimeSyncProvider.Instance.ToSynchronizedKst(ts);
                            srv = new DateTime(srv.Year, srv.Month, srv.Day,
                                               srv.Hour, srv.Minute, srv.Second);
                            pSrv.Value = srv;

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
