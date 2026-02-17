using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.StationMetadata;

public sealed class StationMetadataDatabaseReadiness : IStationMetadataDatabaseReadiness
{
    ISqliteDatabase? _sqliteDatabase;

    public StationMetadataDatabaseReadiness()
    {
    }

    public Task<bool> InitializeAsync(
        ISqliteDatabase sqliteDatabase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);

        _sqliteDatabase = sqliteDatabase;
        return Task.FromResult(true);
    }

    ISqliteDatabase GetInitializedSqliteDatabase()
    {
        var sqliteDatabase = _sqliteDatabase;
        if (sqliteDatabase is null)
            throw new InvalidOperationException("StationMetadataDatabaseReadiness is not initialized (sqliteDatabase).");

        return sqliteDatabase;
    }

    public Task EnsureReadyAsync(CancellationToken cancellationToken)
    { 
        try
        {
            var sqlScripts = StationMetadataSqlScripts.GetAll();
            var sql = sqlScripts[0].Sql;
            return GetInitializedSqliteDatabase().ExecuteDdlAsync(sqlScripts, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to ensure StationMetadata database readiness.", ex);
        }
    }
}
