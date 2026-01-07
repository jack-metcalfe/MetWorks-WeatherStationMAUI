namespace RawPacketRecordTypedInPostgresOut;
internal class PostgresInitializer
{
    static readonly List<string> _listOfDbScriptFilenames = new()
    {
        "lightning.sql",
        "observation.sql",
        "precipitation.sql",
        "wind.sql"
    };
    internal static async Task<bool> DatabaseInitializeAsync(
        IFileLogger iFileLogger,
        IDbConnection iDbConnection
    )
    {
        iFileLogger.Information("🔄 Beginning PostgreSQL schema initialization on background thread");
        using var npgSqlConnection = (NpgsqlConnection)iDbConnection;
        foreach (var scriptFilename in _listOfDbScriptFilenames)
        {
            try
            {
                var scriptPath = Path.Combine(typeof(ListenerSink).Name, scriptFilename);
                var script = IStaticDataStoreContract.GetResourceAsString(scriptPath.Replace('\\', '/'))
                    ?? throw new InvalidOperationException(
                        $"PostgreSQL script {scriptPath} not found in string provider.");
                iFileLogger.Information("Applying PostgreSQL DDL from {scriptPath}");
                await npgSqlConnection.ExecuteAsync(script);
            }
            catch (Exception exception)
            {
                throw iFileLogger
                    .LogExceptionAndReturn(exception, "Error executing PostgreSQL script {scriptKey}");
            }
        }

        iFileLogger.Information("✅ PostgreSQL schema initialization completed");
        return true;
    }
}
