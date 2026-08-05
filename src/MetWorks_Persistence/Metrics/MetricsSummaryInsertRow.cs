namespace MetWorks.Persistence.Metrics;

public sealed record MetricsSummaryInsertRow(
    string Table,
    string CombId,
    string? InstallationId,
    DateTime CapturedUtc,
    int CaptureIntervalSeconds,
    string? ApplicationId,
    int SchemaVersion,
    string JsonMetricsSummary);
