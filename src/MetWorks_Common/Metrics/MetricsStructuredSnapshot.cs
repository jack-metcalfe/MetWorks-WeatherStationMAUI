namespace MetWorks.Common.Metrics;

public sealed record MetricsStructuredSnapshot(
    int SchemaVersion,
    DateTime CapturedUtc,
    int IntervalSeconds,
    MetricsProcessSnapshot? Process,
    MetricsRelaySnapshot? Relay,
    MetricsPipelineSnapshot? Pipeline
);

public sealed record MetricsProcessSnapshot(
    double CpuSecondsDelta,
    double CpuUtilizationRatio,
    int ProcessorCount,
    int Threads,
    MetricsGcSnapshot Gc
);

public sealed record MetricsGcSnapshot(
    int Gen0Delta,
    int Gen1Delta,
    int Gen2Delta,
    long ManagedMemoryBytes
);

public sealed record MetricsRelaySnapshot(
    IReadOnlyList<MetricsRelayHandlerHotspot> TopHandlers,
    IReadOnlyList<MetricsRelayFanoutHotspot> TopFanout
);

public sealed record MetricsRelayHandlerHotspot(
    string MessageType,
    string RecipientType,
    long Count,
    double TotalMs,
    double AvgMs,
    double MaxMs
);

public sealed record MetricsRelayFanoutHotspot(
    string MessageType,
    long HandlerInvocations
);

public sealed record MetricsPipelineSnapshot(
    IReadOnlyList<MetricsPipelineReadingHotspot> TopReadings
);

public sealed record MetricsPipelineReadingHotspot(
    string ReadingType,
    long Count,
    long Retransforms,
    double UdpToTransformStartAvgMs,
    double UdpToTransformStartMaxMs,
    double TransformAvgMs,
    double TransformMaxMs,
    double UdpToTransformEndAvgMs,
    double UdpToTransformEndMaxMs
);
