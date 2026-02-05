namespace MetWorks.EventRelay;
public class EventRelayBasic : IEventRelayBasic
{
    IMessenger _iMessenger = new WeakReferenceMessenger();

    static readonly RelayMetricsAggregator _relayMetrics = new();
    static readonly PipelineMetricsAggregator _pipelineMetrics = new();

    public static bool RelayMetricsEnabled { get; set; }
    public static bool PipelineMetricsEnabled { get; set; }

    public EventRelayBasic() { }
    public void Send<TMessage>(TMessage message) where TMessage : class
        => _iMessenger.Send(message);
    public void Register<TMessage>(object recipient, Action<TMessage> handler) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(handler);

        if (!RelayMetricsEnabled)
        {
            _iMessenger.Register<object, TMessage>(recipient, (r, m) => handler(m));
            return;
        }

        var recipientType = recipient.GetType().Name;
        var messageType = typeof(TMessage).Name;

        _iMessenger.Register<object, TMessage>(recipient, (r, m) =>
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            try
            {
                _relayMetrics.RecordFanOut(messageType);

                if (PipelineMetricsEnabled && m is MetWorks.Interfaces.IWeatherReading reading)
                {
                    try
                    {
                        var p = reading.Provenance;
                        _pipelineMetrics.Record(
                            readingType: messageType,
                            udpToTransformStart: p.TransformStartTime - p.UdpReceiptTime,
                            transformDuration: p.TransformDuration,
                            udpToTransformEnd: p.TotalPipelineTime,
                            isRetransformation: p.IsRetransformation);
                    }
                    catch { }
                }

                if (PipelineMetricsEnabled && m is MetWorks.Interfaces.IRawPacketRecordTyped raw)
                {
                    try
                    {
                        var nowUtc = DateTime.UtcNow;
                        _pipelineMetrics.Record(
                            readingType: messageType,
                            udpToTransformStart: nowUtc - raw.ReceivedTime,
                            transformDuration: TimeSpan.Zero,
                            udpToTransformEnd: nowUtc - raw.ReceivedTime,
                            isRetransformation: false);
                    }
                    catch { }
                }

                handler(m);
            }
            finally
            {
                var end = System.Diagnostics.Stopwatch.GetTimestamp();
                _relayMetrics.Record(messageType, recipientType, end - start);
            }
        });
    }
    public void Unregister<TMessage>(object recipient) where TMessage : class
        => _iMessenger.Unregister<TMessage>(recipient);

    internal static IReadOnlyList<RelayHotspot> SnapshotTopRelayHotspots(int topN)
        => _relayMetrics.SnapshotTopNAndReset(topN);

    internal static IReadOnlyList<FanOutHotspot> SnapshotTopFanOutHotspots(int topN)
        => _relayMetrics.SnapshotTopFanOutAndReset(topN);

    internal static IReadOnlyList<PipelineReadingHotspot> SnapshotTopPipelineHotspots(int topN)
        => _pipelineMetrics.SnapshotTopNAndReset(topN);

    public static IReadOnlyList<RelayHotspotSnapshot> GetTopRelayHotspotsSnapshot(int topN)
        => SnapshotTopRelayHotspots(topN)
            .Select(h => new RelayHotspotSnapshot(
                h.MessageType,
                h.RecipientType,
                h.Count,
                h.TotalMilliseconds,
                h.AverageMilliseconds,
                h.MaxMilliseconds))
            .ToArray();

    public static IReadOnlyList<FanOutHotspotSnapshot> GetTopFanOutSnapshot(int topN)
        => SnapshotTopFanOutHotspots(topN)
            .Select(h => new FanOutHotspotSnapshot(h.MessageType, h.HandlerInvocations))
            .ToArray();

    public static IReadOnlyList<PipelineReadingHotspotSnapshot> GetTopPipelineSnapshot(int topN)
        => SnapshotTopPipelineHotspots(topN)
            .Select(h => new PipelineReadingHotspotSnapshot(
                h.ReadingType,
                h.Stats.Count,
                h.Stats.RetransformCount,
                h.Stats.UdpToTransformStartAvgMs,
                h.Stats.UdpToTransformStartMaxMs,
                h.Stats.TransformDurationAvgMs,
                h.Stats.TransformDurationMaxMs,
                h.Stats.UdpToTransformEndAvgMs,
                h.Stats.UdpToTransformEndMaxMs))
            .ToArray();
}

public readonly record struct PipelineReadingHotspotSnapshot(
    string ReadingType,
    long Count,
    long RetransformCount,
    double UdpToTransformStartAvgMs,
    double UdpToTransformStartMaxMs,
    double TransformAvgMs,
    double TransformMaxMs,
    double UdpToTransformEndAvgMs,
    double UdpToTransformEndMaxMs);

public readonly record struct RelayHotspotSnapshot(
    string MessageType,
    string RecipientType,
    long Count,
    double TotalMilliseconds,
    double AverageMilliseconds,
    double MaxMilliseconds);

public readonly record struct FanOutHotspotSnapshot(
    string MessageType,
    long HandlerInvocations);
