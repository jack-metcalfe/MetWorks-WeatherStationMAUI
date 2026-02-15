namespace MetWorks.Persistence.SQLite;

public sealed record SqliteDbSettings(
    string? ConnectionString,
    string DbPath,
    string JournalMode,
    int BusyTimeoutMs
);
