using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Logging;

internal static class LoggingSqlScripts
{
    internal static IReadOnlyList<SqlScript> GetAll() =>
    [
        new(
            Name: "logger_sqlite_log",
            Sql: """
CREATE TABLE IF NOT EXISTS logger_sqlite_log
(
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp_utc TEXT NOT NULL,
    level TEXT NOT NULL,
    message TEXT NOT NULL,
    exception TEXT NULL,
    properties TEXT NOT NULL,
    installation_id TEXT NULL
);

CREATE INDEX IF NOT EXISTS idx_logger_sqlite_log_timestamp_utc ON logger_sqlite_log(timestamp_utc);
CREATE INDEX IF NOT EXISTS idx_logger_sqlite_log_installation_id ON logger_sqlite_log(installation_id);
""")
    ];
}
