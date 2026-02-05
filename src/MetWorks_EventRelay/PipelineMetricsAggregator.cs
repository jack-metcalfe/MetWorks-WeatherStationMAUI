namespace MetWorks.EventRelay;

using System.Collections.Concurrent;

internal sealed class PipelineMetricsAggregator
{
    ConcurrentDictionary<string, ReadingPipelineStats> _byReadingType = new(StringComparer.Ordinal);

    public void Record(string readingType, TimeSpan udpToTransformStart, TimeSpan transformDuration, TimeSpan udpToTransformEnd, bool isRetransformation)
    {
        var entry = _byReadingType.GetOrAdd(readingType, static _ => new ReadingPipelineStats());
        entry.Add(udpToTransformStart, transformDuration, udpToTransformEnd, isRetransformation);
    }

    public IReadOnlyList<PipelineReadingHotspot> SnapshotTopNAndReset(int topN)
    {
        if (topN <= 0) return Array.Empty<PipelineReadingHotspot>();

        var snapshotDict = Interlocked.Exchange(
            ref _byReadingType,
            new ConcurrentDictionary<string, ReadingPipelineStats>(StringComparer.Ordinal));

        if (snapshotDict.IsEmpty) return Array.Empty<PipelineReadingHotspot>();

        var snapshot = new List<PipelineReadingHotspot>(snapshotDict.Count);
        foreach (var kv in snapshotDict)
        {
            var s = kv.Value.Snapshot();
            if (s.Count <= 0) continue;
            snapshot.Add(new PipelineReadingHotspot(kv.Key, s));
        }

        return snapshot
            .OrderByDescending(s => s.Stats.UdpToTransformEndTotalTicks)
            .Take(topN)
            .ToArray();
    }

    sealed class ReadingPipelineStats
    {
        long _count;
        long _retransformCount;

        long _udpToTransformStartTotalTicks;
        long _udpToTransformStartMaxTicks;

        long _transformDurationTotalTicks;
        long _transformDurationMaxTicks;

        long _udpToTransformEndTotalTicks;
        long _udpToTransformEndMaxTicks;

        public void Add(TimeSpan udpToTransformStart, TimeSpan transformDuration, TimeSpan udpToTransformEnd, bool isRetransformation)
        {
            Interlocked.Increment(ref _count);
            if (isRetransformation) Interlocked.Increment(ref _retransformCount);

            AddDuration(ref _udpToTransformStartTotalTicks, ref _udpToTransformStartMaxTicks, udpToTransformStart);
            AddDuration(ref _transformDurationTotalTicks, ref _transformDurationMaxTicks, transformDuration);
            AddDuration(ref _udpToTransformEndTotalTicks, ref _udpToTransformEndMaxTicks, udpToTransformEnd);
        }

        static void AddDuration(ref long totalTicks, ref long maxTicks, TimeSpan duration)
        {
            var ticks = duration.Ticks;
            if (ticks < 0) ticks = 0;

            Interlocked.Add(ref totalTicks, ticks);

            long currentMax;
            while (ticks > (currentMax = Interlocked.Read(ref maxTicks)))
            {
                if (Interlocked.CompareExchange(ref maxTicks, ticks, currentMax) == currentMax)
                    break;
            }
        }

        public PipelineReadingStatsSnapshot Snapshot()
            => new(
                Count: Interlocked.Read(ref _count),
                RetransformCount: Interlocked.Read(ref _retransformCount),
                UdpToTransformStartTotalTicks: Interlocked.Read(ref _udpToTransformStartTotalTicks),
                UdpToTransformStartMaxTicks: Interlocked.Read(ref _udpToTransformStartMaxTicks),
                TransformDurationTotalTicks: Interlocked.Read(ref _transformDurationTotalTicks),
                TransformDurationMaxTicks: Interlocked.Read(ref _transformDurationMaxTicks),
                UdpToTransformEndTotalTicks: Interlocked.Read(ref _udpToTransformEndTotalTicks),
                UdpToTransformEndMaxTicks: Interlocked.Read(ref _udpToTransformEndMaxTicks)
            );
    }
}

internal readonly record struct PipelineReadingHotspot(
    string ReadingType,
    PipelineReadingStatsSnapshot Stats);

internal readonly record struct PipelineReadingStatsSnapshot(
    long Count,
    long RetransformCount,
    long UdpToTransformStartTotalTicks,
    long UdpToTransformStartMaxTicks,
    long TransformDurationTotalTicks,
    long TransformDurationMaxTicks,
    long UdpToTransformEndTotalTicks,
    long UdpToTransformEndMaxTicks)
{
    public double UdpToTransformStartTotalMs => TimeSpan.FromTicks(UdpToTransformStartTotalTicks).TotalMilliseconds;
    public double UdpToTransformStartAvgMs => Count <= 0 ? 0 : UdpToTransformStartTotalMs / Count;
    public double UdpToTransformStartMaxMs => TimeSpan.FromTicks(UdpToTransformStartMaxTicks).TotalMilliseconds;

    public double TransformDurationTotalMs => TimeSpan.FromTicks(TransformDurationTotalTicks).TotalMilliseconds;
    public double TransformDurationAvgMs => Count <= 0 ? 0 : TransformDurationTotalMs / Count;
    public double TransformDurationMaxMs => TimeSpan.FromTicks(TransformDurationMaxTicks).TotalMilliseconds;

    public double UdpToTransformEndTotalMs => TimeSpan.FromTicks(UdpToTransformEndTotalTicks).TotalMilliseconds;
    public double UdpToTransformEndAvgMs => Count <= 0 ? 0 : UdpToTransformEndTotalMs / Count;
    public double UdpToTransformEndMaxMs => TimeSpan.FromTicks(UdpToTransformEndMaxTicks).TotalMilliseconds;
}