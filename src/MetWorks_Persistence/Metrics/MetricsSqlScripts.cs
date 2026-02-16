using System.Text.RegularExpressions;
using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Metrics;

internal static class MetricsSqlScripts
{
    static readonly Regex ValidIdentifier = new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    internal static IReadOnlyList<SqlScript> GetForTable(string table)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(table);

        var identifier = table.Trim();

        if (identifier.Length >= 2 && identifier[0] == '"' && identifier[^1] == '"')
            identifier = identifier[1..^1];

        if (!ValidIdentifier.IsMatch(identifier))
            throw new ArgumentException(
                "Table name contains invalid characters. Only letters, digits and underscore are allowed.",
                nameof(table));

        return
        [
            new(
                Name: $"metrics_summary:{identifier}",
                Sql: $"""
CREATE TABLE IF NOT EXISTS [{identifier}]
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

CREATE INDEX IF NOT EXISTS idx_{identifier}_captured_utc ON [{identifier}](captured_utc);
CREATE INDEX IF NOT EXISTS idx_{identifier}_installation_id ON [{identifier}](installation_id);
""")
        ];
    }
}
