namespace MetWorks.Common.Metrics;

using System.Collections.Concurrent;
using System.Diagnostics;

public static class StreamShippingUploadMetrics
{
    static StreamShippingMetricsAggregator _aggregator = new();

    public static void Record(
        string table,
        int rows,
        long gzipBytes,
        long elapsedTicks,
        bool success)
    {
        if (string.IsNullOrWhiteSpace(table)) return;
        if (rows < 0) rows = 0;
        if (gzipBytes < 0) gzipBytes = 0;
        if (elapsedTicks < 0) elapsedTicks = 0;

        _aggregator.Record(table.Trim(), rows, gzipBytes, elapsedTicks, success);
    }

    public static IReadOnlyList<StreamShippingUploadHotspot> SnapshotTopNAndReset(int topN)
        => _aggregator.SnapshotTopNAndReset(topN);

    internal sealed class StreamShippingMetricsAggregator
    {
        ConcurrentDictionary<string, UploadStats> _stats = new();

        public void Record(string table, int rows, long gzipBytes, long elapsedTicks, bool success)
        {
            var key = table;
            var entry = _stats.GetOrAdd(key, static _ => new UploadStats());
            entry.Add(rows, gzipBytes, elapsedTicks, success);
        }

        public IReadOnlyList<StreamShippingUploadHotspot> SnapshotTopNAndReset(int topN)
        {
            if (topN <= 0) return Array.Empty<StreamShippingUploadHotspot>();

            var snapshotDict = Interlocked.Exchange(
                ref _stats,
                new ConcurrentDictionary<string, UploadStats>());

            if (snapshotDict.IsEmpty) return Array.Empty<StreamShippingUploadHotspot>();

            var snapshot = new List<StreamShippingUploadHotspot>(snapshotDict.Count);
            foreach (var kv in snapshotDict)
            {
                var s = kv.Value.Snapshot();
                if (s.Attempts <= 0) continue;

                snapshot.Add(new StreamShippingUploadHotspot(
                    Table: kv.Key,
                    Attempts: s.Attempts,
                    Successes: s.Successes,
                    Failures: s.Failures,
                    Rows: s.Rows,
                    GzipBytes: s.GzipBytes,
                    TotalTicks: s.TotalTicks,
                    MaxTicks: s.MaxTicks));
            }

            return snapshot
                .OrderByDescending(h => h.TotalTicks)
                .Take(topN)
                .ToArray();
        }

        sealed class UploadStats
        {
            long _attempts;
            long _successes;
            long _failures;
            long _rows;
            long _gzipBytes;
            long _totalTicks;
            long _maxTicks;

            public void Add(int rows, long gzipBytes, long ticks, bool success)
            {
                Interlocked.Increment(ref _attempts);
                if (success) Interlocked.Increment(ref _successes);
                else Interlocked.Increment(ref _failures);

                if (rows > 0) Interlocked.Add(ref _rows, rows);
                if (gzipBytes > 0) Interlocked.Add(ref _gzipBytes, gzipBytes);
                if (ticks > 0) Interlocked.Add(ref _totalTicks, ticks);

                long currentMax;
                while (ticks > (currentMax = Interlocked.Read(ref _maxTicks)))
                {
                    if (Interlocked.CompareExchange(ref _maxTicks, ticks, currentMax) == currentMax)
                        break;
                }
            }

            public (long Attempts, long Successes, long Failures, long Rows, long GzipBytes, long TotalTicks, long MaxTicks) Snapshot()
                => (
                    Interlocked.Read(ref _attempts),
                    Interlocked.Read(ref _successes),
                    Interlocked.Read(ref _failures),
                    Interlocked.Read(ref _rows),
                    Interlocked.Read(ref _gzipBytes),
                    Interlocked.Read(ref _totalTicks),
                    Interlocked.Read(ref _maxTicks));
        }
    }
}

public readonly record struct StreamShippingUploadHotspot(
    string Table,
    long Attempts,
    long Successes,
    long Failures,
    long Rows,
    long GzipBytes,
    long TotalTicks,
    long MaxTicks)
{
    public double TotalMilliseconds => TotalTicks * 1000.0 / Stopwatch.Frequency;
    public double MaxMilliseconds => MaxTicks * 1000.0 / Stopwatch.Frequency;
    public double AverageMilliseconds => Attempts <= 0 ? 0 : TotalMilliseconds / Attempts;
}
