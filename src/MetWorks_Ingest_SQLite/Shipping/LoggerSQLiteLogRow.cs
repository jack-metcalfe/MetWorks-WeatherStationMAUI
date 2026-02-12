namespace MetWorks.Ingest.SQLite.Shipping;

internal sealed record LoggerSQLiteLogRow(
    long Id,
    string TimestampUtc,
    string Level,
    string Message,
    string? Exception,
    string? PropertiesJson,
    string? InstallationId
);
