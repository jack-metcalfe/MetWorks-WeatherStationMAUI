namespace MetWorks.Data.Sqlite;

public sealed record SqliteDatabaseOptions
{
    public string ConnectionString { get; private set; } = string.Empty;
    public string? JournalMode { get; private set; } = "WAL";
    public int BusyTimeoutMs { get; private set; } = 5000;

    public Task<bool> InitializeAsync(
        string connectionString, 
        string? journalMode, 
        int? busyTimeoutMs, 
        CancellationToken cancellationToken
    )
    {
        ConnectionString = connectionString;
        JournalMode ??= journalMode;
        if (busyTimeoutMs is not null)
            BusyTimeoutMs = (int)busyTimeoutMs;
        return Task.FromResult(true);
    }
}
