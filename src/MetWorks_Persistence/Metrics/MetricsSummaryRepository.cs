using System.Text.RegularExpressions;
using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Metrics;

public sealed class MetricsSummaryRepository : IMetricsSummaryRepository
{
    static readonly Regex ValidIdentifier = new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    ISqliteDatabase? _sqliteDatabase;

    public MetricsSummaryRepository()
    {
    }

    public Task<bool> InitializeAsync(ISqliteDatabase sqliteDatabase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);

        _sqliteDatabase = sqliteDatabase;
        return Task.FromResult(true);
    }

    ISqliteDatabase GetInitializedSqliteDatabase() =>
        _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(MetricsSummaryRepository)} has not been initialized.");

    public async Task InsertAsync(MetricsSummaryInsertRow row, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (string.IsNullOrWhiteSpace(row.Table))
            throw new ArgumentException("Table is required.", nameof(row));

        if (!ValidIdentifier.IsMatch(row.Table))
            throw new ArgumentException(
                "Table name contains invalid characters. Only letters, digits and underscore are allowed.",
                nameof(row));

        if (string.IsNullOrWhiteSpace(row.CombId))
            throw new ArgumentException("CombId is required.", nameof(row));

        if (string.IsNullOrWhiteSpace(row.JsonMetricsSummary))
            throw new ArgumentException("JsonMetricsSummary is required.", nameof(row));

        var schemaVersion = row.SchemaVersion <= 0 ? 1 : row.SchemaVersion;

        var sql = $"""
INSERT INTO [{row.Table}] (
    comb_id,
    installation_id,
    captured_utc,
    capture_interval_seconds,
    application_id,
    schema_version,
    json_metrics_summary
)
VALUES (
    $comb_id,
    $installation_id,
    $captured_utc,
    $capture_interval_seconds,
    $application_id,
    $schema_version,
    $json_metrics_summary
);
""";

        await using var session = await GetInitializedSqliteDatabase().OpenSessionAsync(cancellationToken).ConfigureAwait(false);

        _ = await session.ExecuteAsync(
            sql,
            [
                new DbParam("$comb_id", row.CombId),
                new DbParam("$installation_id", string.IsNullOrWhiteSpace(row.InstallationId) ? DBNull.Value : row.InstallationId),
                new DbParam("$captured_utc", row.CapturedUtc.ToUniversalTime().ToString("O")),
                new DbParam("$capture_interval_seconds", row.CaptureIntervalSeconds),
                new DbParam("$application_id", string.IsNullOrWhiteSpace(row.ApplicationId) ? DBNull.Value : row.ApplicationId),
                new DbParam("$schema_version", schemaVersion),
                new DbParam("$json_metrics_summary", row.JsonMetricsSummary),
            ],
            cancellationToken).ConfigureAwait(false);
    }
}
