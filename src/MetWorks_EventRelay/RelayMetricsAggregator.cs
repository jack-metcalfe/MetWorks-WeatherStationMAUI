namespace MetWorks.EventRelay;
internal sealed class RelayMetricsAggregator
{
    ConcurrentDictionary<(string MessageType, string RecipientType), HandlerStats> _stats = new();
    ConcurrentDictionary<string, FanOutStats> _fanOutByMessageType = new(StringComparer.Ordinal);

    public void Record(string messageType, string recipientType, long elapsedTicks)
    {
        var key = (messageType, recipientType);
        var entry = _stats.GetOrAdd(key, static _ => new HandlerStats());
        entry.Add(elapsedTicks);
    }

    public void RecordFanOut(string messageType)
    {
        var entry = _fanOutByMessageType.GetOrAdd(messageType, static _ => new FanOutStats());
        entry.Increment();
    }

    public IReadOnlyList<RelayHotspot> SnapshotTopN(int topN)
    {
        if (topN <= 0) return Array.Empty<RelayHotspot>();

        var snapshot = new List<RelayHotspot>(_stats.Count);
        foreach (var kv in _stats)
        {
            var s = kv.Value.Snapshot();
            if (s.Count <= 0) continue;
            snapshot.Add(new RelayHotspot(kv.Key.MessageType, kv.Key.RecipientType, s.Count, s.TotalTicks, s.MaxTicks));
        }

        return snapshot
            .OrderByDescending(h => h.TotalTicks)
            .Take(topN)
            .ToArray();
    }

    public IReadOnlyList<RelayHotspot> SnapshotTopNAndReset(int topN)
    {
        if (topN <= 0) return Array.Empty<RelayHotspot>();

        // Swap out the current stats dictionary so new recordings don't interfere with this snapshot.
        var snapshotDict = Interlocked.Exchange(
            ref _stats,
            new ConcurrentDictionary<(string MessageType, string RecipientType), HandlerStats>());

        if (snapshotDict.IsEmpty) return Array.Empty<RelayHotspot>();

        var snapshot = new List<RelayHotspot>(snapshotDict.Count);
        foreach (var kv in snapshotDict)
        {
            var s = kv.Value.Snapshot();
            if (s.Count <= 0) continue;
            snapshot.Add(new RelayHotspot(kv.Key.MessageType, kv.Key.RecipientType, s.Count, s.TotalTicks, s.MaxTicks));
        }

        return snapshot
            .OrderByDescending(h => h.TotalTicks)
            .Take(topN)
            .ToArray();
    }

    public IReadOnlyList<FanOutHotspot> SnapshotTopFanOutAndReset(int topN)
    {
        if (topN <= 0) return Array.Empty<FanOutHotspot>();

        var snapshotDict = Interlocked.Exchange(
            ref _fanOutByMessageType,
            new ConcurrentDictionary<string, FanOutStats>(StringComparer.Ordinal));

        if (snapshotDict.IsEmpty) return Array.Empty<FanOutHotspot>();

        var snapshot = new List<FanOutHotspot>(snapshotDict.Count);
        foreach (var kv in snapshotDict)
        {
            var count = kv.Value.SnapshotCount();
            if (count <= 0) continue;
            snapshot.Add(new FanOutHotspot(kv.Key, count));
        }

        return snapshot
            .OrderByDescending(s => s.HandlerInvocations)
            .Take(topN)
            .ToArray();
    }

    sealed class HandlerStats
    {
        long _count;
        long _totalTicks;
        long _maxTicks;

        public void Add(long ticks)
        {
            Interlocked.Increment(ref _count);
            Interlocked.Add(ref _totalTicks, ticks);

            long currentMax;
            while (ticks > (currentMax = Interlocked.Read(ref _maxTicks)))
            {
                if (Interlocked.CompareExchange(ref _maxTicks, ticks, currentMax) == currentMax)
                    break;
            }
        }

        public (long Count, long TotalTicks, long MaxTicks) Snapshot()
            => (Interlocked.Read(ref _count), Interlocked.Read(ref _totalTicks), Interlocked.Read(ref _maxTicks));
    }

    sealed class FanOutStats
    {
        long _handlerInvocations;

        public void Increment() => Interlocked.Increment(ref _handlerInvocations);

        public long SnapshotCount() => Interlocked.Read(ref _handlerInvocations);
    }
}

internal readonly record struct FanOutHotspot(
    string MessageType,
    long HandlerInvocations);

internal readonly record struct RelayHotspot(
    string MessageType,
    string RecipientType,
    long Count,
    long TotalTicks,
    long MaxTicks)
{
    public double TotalMilliseconds => TotalTicks * 1000.0 / Stopwatch.Frequency;
    public double MaxMilliseconds => MaxTicks * 1000.0 / Stopwatch.Frequency;
    public double AverageMilliseconds => Count <= 0 ? 0 : TotalMilliseconds / Count;
}
