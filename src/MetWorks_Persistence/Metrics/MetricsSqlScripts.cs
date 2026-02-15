using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Metrics;

internal static class MetricsSqlScripts
{
    internal static IReadOnlyList<SqlScript> GetForTable(string table)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(table);

        return
        [
            new(
                Name: $"metrics_summary:{table}",
                Sql: $"""
CREATE TABLE IF NOT EXISTS ""{table}""
(
    comb_id TEXT PRIMARY KEY,
    installation_id TEXT NULL,
    captured_utc TEXT NOT NULL,
    capture_interval_seconds INTEGER NOT NULL,
    application_id TEXT NULL,
    schema_version INTEGER NOT NULL,
    json_metrics_summary TEXT NOT NULL,
    database_received_utc_timestampz TEXT DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);

CREATE INDEX IF NOT EXISTS idx_{table}_captured_utc ON ""{table}""(captured_utc);
CREATE INDEX IF NOT EXISTS idx_{table}_installation_id ON ""{table}""(installation_id);
""")
        ];
    }
}
