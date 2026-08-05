using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Rollups;

public sealed class RollupsDatabaseReadiness : IRollupsDatabaseReadiness
{
    ISqliteDatabase? _sqliteDatabase;

    public RollupsDatabaseReadiness()
    {
    }

    public Task<bool> InitializeAsync(ISqliteDatabase sqliteDatabase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);

        _sqliteDatabase = sqliteDatabase;
        return Task.FromResult(true);
    }

    ISqliteDatabase GetInitializedSqliteDatabase() =>
        _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(RollupsDatabaseReadiness)} has not been initialized.");

    public Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        return GetInitializedSqliteDatabase().ExecuteDdlAsync(RollupsSqlScripts.GetAll(), cancellationToken);
    }
}
