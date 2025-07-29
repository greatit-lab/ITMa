// Services\PerformanceWarmUp.cs
using ConnectInfo;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ITM_Agent.Startup
{
    internal static class PerformanceWarmUp
    {
        public static void Run()
        {
            // 1) PDH 카운터 더미 호출
            var cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpu.NextValue();
            Thread.Sleep(100);
            cpu.NextValue();  // 유효 값 확보

            // 2) DB 커넥션 풀 최소 1개 미리 오픈
            try
            {
                string cs = DatabaseInfo.CreateDefault().GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                { conn.Open(); }            // SELECT 1 불필요
            }
            catch { /* 로깅만 */ }
        }
    }
}
