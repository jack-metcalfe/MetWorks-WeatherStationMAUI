namespace MetWorks.Persistence.Logging;
internal static class LoggingSqlScripts
{
    const string DefaultTableName = DatabaseConstants.DefaultLoggerSqliteTableName;

    internal static IReadOnlyList<SqlScript> GetForTable(string table)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(table);

        return
        [
            new(
                Name: $"logger_sqlite_log:{table}",
                           Sql: $"""
                CREATE TABLE IF NOT EXISTS [{table}]
                (
                    id TEXT PRIMARY KEY NOT NULL,
                    timestamp_utc TEXT NOT NULL,
                    level TEXT NOT NULL,
                    message TEXT NOT NULL,
                    exception TEXT NULL,
                    properties TEXT NOT NULL,
                    installation_id TEXT NULL
                );
 
 CREATE INDEX IF NOT EXISTS idx_{table}_timestamp_utc ON [{table}](timestamp_utc);
 CREATE INDEX IF NOT EXISTS idx_{table}_installation_id ON [{table}](installation_id);
 """)
        ];
    }
}
