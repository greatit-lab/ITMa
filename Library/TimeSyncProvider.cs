// ITM_Agent.Services.TimeSyncProvider.cs
using System;

namespace ITM_Agent.Services
{
    /// <summary>
    /// 서버-PC 시각 차(diff)를 공통으로 보관/제공하는 싱글턴
    /// </summary>
    public sealed class TimeSyncProvider
    {
        private static readonly Lazy<TimeSyncProvider> _inst =
            new Lazy<TimeSyncProvider>(() => new TimeSyncProvider());
        public static TimeSyncProvider Instance => _inst.Value;

        private readonly object sync = new object();
        private TimeSpan diff = TimeSpan.Zero;   // 초기 0

        private TimeSyncProvider() { }

        /* ▶ diff 읽기 */
        public TimeSpan Diff
        {
            get { lock (sync) return diff; }
        }

        /* ▶ diff 갱신 (PerformanceDbWriter 전용) */
        public void UpdateDiff(TimeSpan newDiff)
        {
            lock (sync) diff = newDiff;
        }

        /* ▶ 로컬 시각 → 서버 보정 시각 */
        public DateTime Apply(DateTime local) => local + Diff;
    }
}
