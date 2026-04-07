using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.StreamShipping;

public sealed class StreamShippingDatabaseReadiness : IStreamShippingDatabaseReadiness
{
    ISqliteDatabase? _sqliteDatabase;

    public StreamShippingDatabaseReadiness()
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
            throw new InvalidOperationException("StreamShippingDatabaseReadiness is not initialized (sqliteDatabase).");

        return sqliteDatabase;
    }

    public Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        var sqlScripts = StreamShippingSqlScripts.GetAll();
        return GetInitializedSqliteDatabase().ExecuteDdlAsync(sqlScripts, cancellationToken);
    } 
}
