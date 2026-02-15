using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Ingest;

public sealed class RawPacketDatabaseReadiness : IRawPacketDatabaseReadiness
{
    ISqliteDatabase? _sqliteDatabase;

    public RawPacketDatabaseReadiness()
    {
    }

    public Task<bool> InitializeAsync(ISqliteDatabase sqliteDatabase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);

        _sqliteDatabase = sqliteDatabase;
        return Task.FromResult(true);
    }

    ISqliteDatabase GetInitializedSqliteDatabase() =>
        _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(RawPacketDatabaseReadiness)} has not been initialized.");

    public Task EnsureReadyAsync(CancellationToken cancellationToken)
        => GetInitializedSqliteDatabase().ExecuteDdlAsync(RawPacketSqlScripts.GetAll(), cancellationToken);
}
