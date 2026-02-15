using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Logging;

public sealed class LoggingDatabaseReadiness : ILoggingDatabaseReadiness
{
    ISqliteDatabase? _sqliteDatabase;
    int _isInitialized;

    public LoggingDatabaseReadiness() { }

    public Task<bool> InitializeAsync(ISqliteDatabase sqliteDatabase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);

        if (Interlocked.Exchange(ref _isInitialized, 1) == 1)
            throw new InvalidOperationException($"{nameof(LoggingDatabaseReadiness)} is already initialized.");

        if (cancellationToken.IsCancellationRequested) return Task.FromResult(false);

        _sqliteDatabase = sqliteDatabase;
        return Task.FromResult(true);
    }

    public Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized != 1)
            throw new InvalidOperationException($"{nameof(LoggingDatabaseReadiness)} is not initialized.");

        var sqliteDatabase = _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(LoggingDatabaseReadiness)} is not initialized.");
        return sqliteDatabase.ExecuteDdlAsync(LoggingSqlScripts.GetAll(), cancellationToken);
    }
}
