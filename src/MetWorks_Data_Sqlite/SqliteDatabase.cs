namespace MetWorks.Data.Sqlite;
public sealed class SqliteDatabase : ISqliteDatabase
{
    SqliteDatabaseOptions? _options;
    bool _isInitialized = false;
    ILogger? _iLogger = null;
    ILogger ILogger => NullPropertyGuard.Get(_isInitialized, _iLogger, nameof(ILogger));

    public SqliteDatabase()
    {
    }

    public Task<bool> InitializeAsync(
        ILogger iLogger,
        SqliteDatabaseOptions options, 
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(options);

        _iLogger = iLogger;
        _options = options;
        _isInitialized = true;
        return Task.FromResult(true);
    }

    SqliteDatabaseOptions GetInitializedOptions() =>
        _options ?? throw new InvalidOperationException($"{nameof(SqliteDatabase)} has not been initialized.");

    public async Task<ISqliteSession> OpenSessionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var options = GetInitializedOptions();

            var conn = new SqliteConnection(options.ConnectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            await ApplyConnectionPragmasAsync(conn, cancellationToken).ConfigureAwait(false);

            return new SqliteSession(conn);
        }
        catch (Exception exception)
        {
            var message = "Failed to open a database session.";
            ILogger.Error(message, exception);
            throw new InvalidOperationException(message, exception);
        }
    }

    public async Task ExecuteDdlAsync(IReadOnlyList<SqlScript> scripts, CancellationToken cancellationToken)
    {
        SqlScript? currentScript = null;
        try
        {
            ArgumentNullException.ThrowIfNull(scripts);

            await using var session = await OpenSessionAsync(cancellationToken).ConfigureAwait(false);

            await session.ExecuteInTransactionAsync(async (s, ct) =>
            {
                foreach (var script in scripts)
                {
                    currentScript = script;
                    if (string.IsNullOrWhiteSpace(script.Sql))
                        continue;

                    await s.ExecuteAsync(script.Sql, parameters: null, ct).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var message = currentScript is not null
                ? $"Failed to execute DDL script '{currentScript.Name}'."
                : "Failed to execute DDL scripts.";
            ILogger.Error(message, exception);
            if (currentScript is not null)
                ILogger.Error($"Failed SQL: {currentScript.Sql}");
            throw new InvalidOperationException(message, exception);
        }
    }

    async Task ApplyConnectionPragmasAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        try
        {
            var options = GetInitializedOptions();
            var journalMode = string.IsNullOrWhiteSpace(options.JournalMode) ? "WAL" : options.JournalMode;
            var busyTimeoutMs = options.BusyTimeoutMs <= 0 ? 5000 : options.BusyTimeoutMs;

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA journal_mode={journalMode}; PRAGMA busy_timeout={busyTimeoutMs};";
            _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var message = "Failed to apply connection pragmas.";
            ILogger.Error(message, exception);
            throw new InvalidOperationException(message, exception);
        }
    }
}
