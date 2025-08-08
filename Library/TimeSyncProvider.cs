// ITM_Agent.Services.TimeSyncProvider.cs
using ConnectInfo;
using Npgsql;
using System;
using System.Threading;

namespace ITM_Agent.Services
{
    /// <summary>
    /// 서버-PC 간의 시간 오차를 보정하고, 모든 시간을 한국 표준시(KST)로 변환하는
    /// 중앙 집중형 시간 동기화 클래스입니다.
    /// </summary>
    public sealed class TimeSyncProvider : IDisposable
    {
        private static readonly Lazy<TimeSyncProvider> _inst =
            new Lazy<TimeSyncProvider>(() => new TimeSyncProvider());
        public static TimeSyncProvider Instance => _inst.Value;

        private readonly object syncLock = new object();
        private TimeSpan clockDiff = TimeSpan.Zero; // 서버와 PC의 순수한 시간 차이
        private readonly TimeZoneInfo kstZone;
        private readonly Timer syncTimer;

        public TimeSpan Diff
        {
            get
            {
                lock (syncLock)
                {
                    return clockDiff;
                }
            }
        }

        private TimeSyncProvider()
        {
            // KST 타임존 정보 로드
            try
            {
                kstZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                kstZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
            }

            // 앱 시작 시 즉시 동기화 후, 10분마다 주기적으로 서버 시간과 동기화
            syncTimer = new Timer(
                _ => SynchronizeWithServer(),
                null,
                TimeSpan.Zero,                // 즉시 실행
                TimeSpan.FromMinutes(10)      // 10분 간격
            );
        }

        /// <summary>
        /// DB 서버의 UTC 시간과 로컬 PC의 UTC 시간을 비교하여 순수한 시간 차이를 계산합니다.
        /// </summary>
        private void SynchronizeWithServer()
        {
            try
            {
                string cs = DatabaseInfo.CreateDefault().GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT NOW() AT TIME ZONE 'UTC'", conn))
                    {
                        DateTime serverUtcTime = Convert.ToDateTime(cmd.ExecuteScalar());
                        DateTime clientUtcTime = DateTime.UtcNow;

                        // 서버와 클라이언트의 UTC 기준 시간 차이를 계산하여 저장
                        lock (syncLock)
                        {
                            clockDiff = serverUtcTime - clientUtcTime;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // DB 연결 실패 시 기존 시간 차이 값을 유지
            }
        }

        /// <summary>
        /// 장비의 로컬 시간을 서버 시간과 동기화한 후, 한국 표준시(KST)로 변환합니다.
        /// </summary>
        /// <param name="agentLocalTime">장비에서 발생한 로컬 시간</param>
        /// <returns>오차가 보정된 KST 시간</returns>
        public DateTime ToSynchronizedKst(DateTime agentLocalTime)
        {
            // 1. 입력된 시간을 UTC 시간으로 변환합니다.
            //    (DateTimeKind가 지정되지 않았다면 Local로 간주)
            DateTime agentUtcTime = agentLocalTime.ToUniversalTime();

            // 2. 계산된 서버-장비 시간 오차를 더하여 시간을 동기화합니다.
            DateTime synchronizedUtcTime;
            lock (syncLock)
            {
                synchronizedUtcTime = agentUtcTime.Add(clockDiff);
            }

            // 3. 동기화된 UTC 시간을 KST로 최종 변환합니다.
            return TimeZoneInfo.ConvertTimeFromUtc(synchronizedUtcTime, kstZone);
        }

        public void Dispose()
        {
            syncTimer?.Dispose();
        }
    }
}
