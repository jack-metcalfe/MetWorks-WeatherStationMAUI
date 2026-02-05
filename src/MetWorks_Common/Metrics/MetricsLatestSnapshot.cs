namespace MetWorks.Common.Metrics;
public sealed record MetricsLatestSnapshot(
    DateTime CapturedUtc,
    int IntervalSeconds,
    string JsonPayload,
    string PersistStatus,
    DateTime? PersistAttemptUtc,
    string? PersistErrorMessage
);
