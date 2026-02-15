using Microsoft.Data.Sqlite;

namespace MetWorks.Data.Sqlite;

public sealed class SqliteDatabase : ISqliteDatabase
{
    SqliteDatabaseOptions? _options;

    public SqliteDatabase()
    {
    }

    public Task<bool> InitializeAsync(SqliteDatabaseOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        return Task.FromResult(true);
    }

    SqliteDatabaseOptions GetInitializedOptions() =>
        _options ?? throw new InvalidOperationException($"{nameof(SqliteDatabase)} has not been initialized.");

    public async Task<ISqliteSession> OpenSessionAsync(CancellationToken cancellationToken)
    {
        var options = GetInitializedOptions();

        var conn = new SqliteConnection(options.ConnectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await ApplyConnectionPragmasAsync(conn, cancellationToken).ConfigureAwait(false);

        return new SqliteSession(conn);
    }

    public async Task ExecuteDdlAsync(IReadOnlyList<SqlScript> scripts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scripts);

        await using var session = await OpenSessionAsync(cancellationToken).ConfigureAwait(false);

        await session.ExecuteInTransactionAsync(async (s, ct) =>
        {
            foreach (var script in scripts)
            {
                if (string.IsNullOrWhiteSpace(script.Sql))
                    continue;

                await s.ExecuteAsync(script.Sql, parameters: null, ct).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task ApplyConnectionPragmasAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        var options = GetInitializedOptions();
        var journalMode = string.IsNullOrWhiteSpace(options.JournalMode) ? "WAL" : options.JournalMode;
        var busyTimeoutMs = options.BusyTimeoutMs <= 0 ? 5000 : options.BusyTimeoutMs;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA journal_mode={journalMode}; PRAGMA busy_timeout={busyTimeoutMs};";
        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
