// Services\PerformanceMonitorService.cs
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
    /// ETW/PDH 경량 샘플링 → 버퍼링 → 배치 쓰기 담당 (Singleton)
    /// </summary>
    public sealed class PerformanceMonitorService
    {
        /* ---------- 싱글턴 ---------- */
        private static readonly Lazy<PerformanceMonitorService> _inst =
            new Lazy<PerformanceMonitorService>(() => new PerformanceMonitorService());
        public static PerformanceMonitorService Instance => _inst.Value;

        /* ---------- 내부 필드 ---------- */
        private readonly PdhSampler sampler;
        private readonly CircularBuffer<Metric> buffer = new CircularBuffer<Metric>(capacity: 1000);
        private readonly Timer flushTimer;
        private readonly object sync = new object();

        private const int FLUSH_INTERVAL_MS = 30_000;  // 30초
        private const int BULK_COUNT        = 60;      // 60건 이상 시 즉시 플러시
        private bool isEnabled;

        /* ---------- ctor ---------- */
        private PerformanceMonitorService()
        {
            sampler = new PdhSampler(intervalMs: 5_000);                  // 기본 5초
            sampler.OnSample += OnSampleReceived;
            sampler.OnThresholdExceeded += () => sampler.IntervalMs = 1_000;
            sampler.OnBackToNormal     += () => sampler.IntervalMs = 5_000;

            flushTimer = new Timer(_ => FlushToFile(), null,
                                   Timeout.Infinite, Timeout.Infinite);
        }

        /* ---------- Public API ---------- */
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
                FlushToFile();                         // 남은 버퍼 즉시 기록
            }
        }

        /* ---------- 내부 콜백 ---------- */
        private void OnSampleReceived(Metric m)
        {
            lock (sync)
            {
                buffer.Push(m);
                if (buffer.Count >= BULK_COUNT)
                    FlushToFile();
            }
        }

        private void FlushToFile()
        {
            lock (sync)
            {
                if (buffer.Count == 0) return;
                string path = Path.Combine(GetLogDir(),
                                $"{DateTime.Now:yyyyMMdd}_performance.log");

                using (var fs = new FileStream(path, FileMode.Append,
                                               FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(fs))
                {
                    foreach (Metric m in buffer.ToArray())
                        sw.WriteLine($"{m.Timestamp:HH:mm:ss.fff}," +
                                     $"{m.Cpu:F1},{m.Mem:F1}");
                }
                buffer.Clear();
            }
        }

        private static string GetLogDir() =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
    }

    /*──────────────── Metric 구조체 ───────────────*/
    internal readonly struct Metric
    {
        public DateTime Timestamp { get; }
        public float    Cpu       { get; }
        public float    Mem       { get; }    // % 사용률

        public Metric(float cpu, float mem)
        {
            Timestamp = DateTime.Now;
            Cpu = cpu;
            Mem = mem;
        }
    }

    /*──────────────── CircularBuffer<T> ───────────*/
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
                head = (head + 1) % Capacity;          // overwrite
            else
                count++;
        }
        public IEnumerable<T> ToArray() =>
            Enumerable.Range(0, count)
                      .Select(i => buf[(head + i) % Capacity]);
        public void Clear() { head = count = 0; }
    }

    /*──────────────── PDH 래퍼 (경량) ─────────────*/
    internal sealed class PdhSampler
    {
        public event Action<Metric> OnSample;
        public event Action         OnThresholdExceeded;
        public event Action         OnBackToNormal;

        private readonly PerformanceCounter cpu =
            new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private readonly PerformanceCounter mem =
            new PerformanceCounter("Memory", "Available MBytes");

        private Timer timer;
        private int interval;
        private bool overload;

        public int IntervalMs
        {
            get => interval;
            set
            {
                interval = Math.Max(500, value);        // 최소 0.5초 보호
                if (timer != null)
                    timer.Change(0, interval);
            }
        }

        public PdhSampler(int intervalMs)
        {
            interval = intervalMs;
        }

        public void Start()
        {
            timer = new Timer(_ => Sample(), null, 0, interval);
        }
        public void Stop()
        {
            timer?.Dispose();
            timer = null;
        }

        private void Sample()
        {
            // 첫 호출 시 CPU 카운터 워밍-업 필요 → 2회 읽기
            float cpuVal = cpu.NextValue();
            float memFree = mem.NextValue();           // MB
            Thread.Sleep(50);
            cpuVal = cpu.NextValue();

            var pc = new Microsoft.VisualBasic.Devices.ComputerInfo(); // 가벼움
            float memTotal = pc.TotalPhysicalMemory / 1_048_576f;      // MB
            float memUsedRatio = ((memTotal - memFree) / memTotal) * 100f;

            OnSample?.Invoke(new Metric(cpuVal, memUsedRatio));

            bool isOver = (cpuVal > 75f) || (memUsedRatio > 80f);
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
    }
}
