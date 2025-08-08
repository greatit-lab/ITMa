// Services\PerformanceMonitor.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;          // [추가] 총 메모리 조회용 WMI
using System.Threading;

namespace ITM_Agent.Services
{
    /// <summary>
    /// CPU·메모리 사용률을 5 초(기본) / 1 초(과부하) 간격으로 수집하여
    /// CircularBuffer 에 저장 후 60 건 또는 30 초마다
    /// Logs\yyyyMMdd_performance.log 로 배치 기록하는 싱글턴 서비스
    /// </summary>
    public sealed class PerformanceMonitor
    {
        /* ───────────── 싱글턴 ───────────── */
        private static readonly Lazy<PerformanceMonitor> _inst =
            new Lazy<PerformanceMonitor>(() => new PerformanceMonitor());
        public static PerformanceMonitor Instance => _inst.Value;

        /* ───────────── 내부 필드 ───────────── */
        private const long MAX_LOG_SIZE = 5 * 1024 * 1024;   // 5 MB  // [추가]
        private readonly PdhSampler sampler;
        private readonly CircularBuffer<Metric> buffer =
            new CircularBuffer<Metric>(capacity: 1000);
        private readonly Timer flushTimer;
        private readonly object sync = new object();

        private const int FLUSH_INTERVAL_MS = 30_000;   // 30 초마다 배치 쓰기
        private const int BULK_COUNT = 60;       // 60건 이상 시 즉시 쓰기
        private bool isEnabled;

        private bool sampling;                 // 샘플러 실행 여부
        private bool fileLoggingEnabled;       // [추가] 파일 기록 여부

        /*──────── 샘플링 ON ───────*/
        internal void StartSampling()
        {
            if (sampling) return;
            sampling = true;
            sampler.Start();
        }

        /*──────── 샘플링 + 파일로깅 OFF ──*/
        internal void StopSampling()
        {
            if (!sampling) return;
            sampler.Stop();
            DisableFileLogging();
            sampling = false;
        }

        /*──────── 파일 로깅 ON/OFF API ──*/
        internal void SetFileLogging(bool enable) =>
            (enable ? (Action)EnableFileLogging : DisableFileLogging)();

        /*──────── 내부 구현 ─────────*/
        private void EnableFileLogging()
        {
            if (fileLoggingEnabled) return;
            Directory.CreateDirectory(GetLogDir());
            flushTimer.Change(FLUSH_INTERVAL_MS, FLUSH_INTERVAL_MS);
            fileLoggingEnabled = true;
        }

        private void DisableFileLogging()
        {
            if (!fileLoggingEnabled) return;
            flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
            FlushToFile();                       // 남은 버퍼 기록 후 off
            fileLoggingEnabled = false;
        }

        //──────────────── Consumers Registration ────────────────
        // 외부(Batch Writer 등)가 Metric 샘플을 구독/해제할 수 있는 공개 메서드
        internal void RegisterConsumer(Action<Metric> consumer)
        {
            sampler.OnSample += consumer;
        }

        internal void UnregisterConsumer(Action<Metric> consumer)
        {
            sampler.OnSample -= consumer;
        }

        /* ───────────── ctor ───────────── */
        private PerformanceMonitor()
        {
            sampler = new PdhSampler(intervalMs: 5_000);          // 기본 5 초
            sampler.OnSample += OnSampleReceived;
            sampler.OnThresholdExceeded += () => sampler.IntervalMs = 1_000;
            sampler.OnBackToNormal += () => sampler.IntervalMs = 5_000;

            flushTimer = new Timer(_ => FlushToFile(),
                                   null,
                                   Timeout.Infinite,
                                   Timeout.Infinite);
        }

        /* ───────────── Public API ───────────── */
        public void Start()
        {
            lock (sync)
            {
                if (isEnabled) return;
                isEnabled = true;

                Directory.CreateDirectory(GetLogDir());
                sampler.Start();
                flushTimer.Change(FLUSH_INTERVAL_MS, FLUSH_INTERVAL_MS);
            }
        }

        public void Stop()
        {
            lock (sync)
            {
                if (!isEnabled) return;
                isEnabled = false;

                sampler.Stop();
                flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
                FlushToFile();                                 // 종료 시 잔여 버퍼 기록
            }
        }

        /* ───────────── 내부 콜백 ───────────── */
        private void OnSampleReceived(Metric m)
        {
            lock (sync)
            {
                buffer.Push(m);
                if (fileLoggingEnabled && buffer.Count >= BULK_COUNT)
                    FlushToFile();
            }
        }

        private void FlushToFile()
        {
            if (!fileLoggingEnabled || buffer.Count == 0) return;

            string fileName = $"{DateTime.Now:yyyyMMdd}_performance.log";
            string filePath = Path.Combine(GetLogDir(), fileName);

            /*── ① 5 MB 초과 시 회전 ───────────────────────────────────────────────*/
            RotatePerfLogIfNeeded(filePath);    // [추가]

            /*── ② 실제 쓰기 ────────────────────────────────────────────────────*/
            using (var fs = new FileStream(filePath, FileMode.OpenOrCreate,
                                           FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Seek(0, SeekOrigin.End);                      // 항상 Append
                using (var sw = new StreamWriter(fs))
                {
                    foreach (Metric m in buffer.ToArray())
                    {
                        DateTime local = m.Timestamp;            // 이미 PC 로컬 시각
                        sw.WriteLine($"{local:yyyy-MM-dd HH:mm:ss.fff} " +
                                     $"C:{m.Cpu:F2} M:{m.Mem:F2}");
                    }
                }
            }
            buffer.Clear();
        }

        /*===================  (신규) 5 MB 초과 로테이션 메서드 ===================*/
        private void RotatePerfLogIfNeeded(string filePath)      // [추가]
        {
            var fi = new FileInfo(filePath);
            if (!fi.Exists || fi.Length <= MAX_LOG_SIZE) return;  // 5 MB 이하 → 그대로

            string extension = fi.Extension;                          // ".log"
            string baseName = Path.GetFileNameWithoutExtension(filePath); // "20250728_performance"
            string dir = fi.DirectoryName;

            int index = 1;
            string rotatedPath;
            do
            {
                string rotatedName = $"{baseName}_{index}{extension}"; // ex) 20250728_performance_3.log
                rotatedPath = Path.Combine(dir, rotatedName);
                index++;
            }
            while (File.Exists(rotatedPath));                          // 중복 방지

            File.Move(filePath, rotatedPath);                          // 회전 완료
        }

        private static string GetLogDir() =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
    }

    /*────────────────────────── Metric ──────────────────────────*/
    internal readonly struct Metric
    {
        public DateTime Timestamp { get; }   // 장비 로컬 시각
        public float Cpu { get; }
        public float Mem { get; }

        public Metric(float cpu, float mem)
        {
            Timestamp = DateTime.Now;         // [수정] 장비(PC) 현재 시각 그대로
            Cpu = cpu;
            Mem = mem;
        }
    }

    /*────────────────────── CircularBuffer<T> ───────────────────*/
    internal sealed class CircularBuffer<T>
    {
        private readonly T[] buf;
        private int head, count;
        public int Capacity { get; }
        public int Count => count;

        public CircularBuffer(int capacity)
        {
            Capacity = capacity;
            buf = new T[capacity];
        }

        public void Push(T item)
        {
            buf[(head + count) % Capacity] = item;
            if (count == Capacity)
                head = (head + 1) % Capacity;    // overwrite
            else
                count++;
        }

        public IEnumerable<T> ToArray() =>
            Enumerable.Range(0, count)
                      .Select(i => buf[(head + i) % Capacity]);

        public void Clear() => head = count = 0;
    }

    /*──────────────────────── PdhSampler ────────────────────────*/
    /// <summary>
    /// PDH(PerfCounter) 기반 경량 샘플러 + 임계치 감시
    /// </summary>
    internal sealed class PdhSampler
    {
        public event Action<Metric> OnSample;
        public event Action OnThresholdExceeded;
        public event Action OnBackToNormal;

        private readonly PerformanceCounter cpuCounter =
            new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private readonly PerformanceCounter memFreeMbCounter =
            new PerformanceCounter("Memory", "Available MBytes");

        private readonly float totalMemMb;            // [추가] WMI 1회 조회
        private Timer timer;
        private int interval;
        private bool overload;

        public int IntervalMs
        {
            get => interval;
            set
            {
                interval = Math.Max(500, value);      // 최소 0.5 초
                timer?.Change(0, interval);
            }
        }

        public PdhSampler(int intervalMs)
        {
            interval = intervalMs;
            totalMemMb = GetTotalMemoryMb();        // [수정] Microsoft.VisualBasic 제거
        }

        public void Start() => timer = new Timer(_ => Sample(), null, 0, interval);
        public void Stop() { timer?.Dispose(); timer = null; }

        /* ───────────── 샘플링 ───────────── */
        private void Sample()
        {
            float cpuVal = cpuCounter.NextValue();
            float freeMb = memFreeMbCounter.NextValue();
            // 1초 동안 평균 사용량을 측정하기 위해 대기 시간을 1000ms로 변경
            Thread.Sleep(1000);
            cpuVal = cpuCounter.NextValue();

            // totalMemMb가 0인 경우 DivideByZeroException 방지
            float usedRatio = (totalMemMb > 0) ? ((totalMemMb - freeMb) / totalMemMb) * 100f : 0f;
            OnSample?.Invoke(new Metric(cpuVal, usedRatio));

            bool isOver = (cpuVal > 75f) || (usedRatio > 80f);
            if (isOver && !overload)
            {
                overload = true;
                OnThresholdExceeded?.Invoke();
            }
            else if (!isOver && overload)
            {
                overload = false;
                OnBackToNormal?.Invoke();
            }
        }

        /* ───────────── 총 메모리(WMI) ───────────── */
        private static float GetTotalMemoryMb()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                       "SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject o in searcher.Get())
                        return Convert.ToSingle(o["TotalVisibleMemorySize"]) / 1024f;
                }
            }
            catch { /* 실패 시 0 반환 → 나눗셈 보호는 호출부에서 */ }
            return 0f;
        }
    }
}
