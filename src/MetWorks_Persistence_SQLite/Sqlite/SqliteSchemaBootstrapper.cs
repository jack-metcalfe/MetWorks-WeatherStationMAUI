namespace MetWorks.Persistence.SQLite;
internal static class SqliteSchemaBootstrapper
{
    internal static async Task<int> ApplyAllAsync(
        ILogger iLogger,
        SqliteConnection connection,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(connection);

        var applied = 0;

        applied += await ApplyMetaAsync(connection, cancellationToken).ConfigureAwait(false);
        applied += await ApplyPacketTablesAsync(iLogger, connection, cancellationToken).ConfigureAwait(false);
        applied += await ApplyLogTableAsync(connection, cancellationToken).ConfigureAwait(false);
        applied += await ApplyShipperTablesAsync(connection, cancellationToken).ConfigureAwait(false);
        applied += await ApplyStationMetadataAsync(connection, cancellationToken).ConfigureAwait(false);
        applied += await ApplyMetricsSummaryAsync(connection, cancellationToken).ConfigureAwait(false);

        return applied;
    }

    static async Task<int> ApplyMetaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS metworks_meta (
                key TEXT PRIMARY KEY NOT NULL,
                value TEXT NULL
            );
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return 1;
    }

    static async Task<int> ApplyPacketTablesAsync(ILogger iLogger, SqliteConnection connection, CancellationToken cancellationToken)
    {
        var attempted = 0;
        foreach (var udpPacketTableEntry in UdpPacketTableData.PacketTableDataMap.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scriptPath = Path.Combine(@"Ingest", "SQLite", udpPacketTableEntry.TableScriptName);
            var script = IResourceProvider.GetString(scriptPath.Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(script))
            {
                iLogger.Warning($"sqlite.bootstrap ddl missing: script='{scriptPath}'");
                continue;
            }

            attempted++;
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = script;
            cmd.CommandTimeout = 60;
            _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return attempted;
    }

    static async Task<int> ApplyLogTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string tableName = MetWorks.Constants.DatabaseConstants.DefaultLoggerSqliteTableName;

        var sql = $@"
CREATE TABLE IF NOT EXISTS ""{tableName}"" (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp_utc TEXT NOT NULL,
    level TEXT NOT NULL,
    message TEXT,
    exception TEXT,
    properties TEXT,
    installation_id TEXT NULL
);
CREATE INDEX IF NOT EXISTS idx_{tableName}_timestamp_utc ON ""{tableName}""(timestamp_utc);
CREATE INDEX IF NOT EXISTS idx_{tableName}_installation_id ON ""{tableName}""(installation_id);
";

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return 1;
    }

    static async Task<int> ApplyShipperTablesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string scriptPath = "Ingest/SQLite/shipper_state.sql";
        var script = IResourceProvider.GetString(scriptPath);
        if (string.IsNullOrWhiteSpace(script))
        {
            return 0;
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = script;
        cmd.CommandTimeout = 60;
        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return 1;
    }

    static async Task<int> ApplyStationMetadataAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
CREATE TABLE IF NOT EXISTS station_metadata
(
    id TEXT PRIMARY KEY,
    application_received_utc_timestampz TEXT NOT NULL,
    database_received_utc_timestampz TEXT DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    station_id INTEGER NOT NULL,
    station_name TEXT NULL,
    tempest_device_name TEXT NULL,
    latitude REAL NULL,
    longitude REAL NULL,
    elevation_meters REAL NULL,
    json_document_original TEXT NOT NULL,
    json_document_original_json AS (json(json_document_original)) STORED,
    installation_id TEXT NULL
);
CREATE INDEX IF NOT EXISTS idx_station_metadata_station_id ON station_metadata(station_id);
CREATE INDEX IF NOT EXISTS idx_station_metadata_installation_id ON station_metadata(installation_id);
CREATE INDEX IF NOT EXISTS idx_station_metadata_application_received ON station_metadata(application_received_utc_timestampz);
""";

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return 1;
    }

    static async Task<int> ApplyMetricsSummaryAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string tableName = "metrics_summary";

        var sql = $@"
CREATE TABLE IF NOT EXISTS ""{tableName}"" (
    comb_id TEXT PRIMARY KEY,
    installation_id TEXT NULL,
    captured_utc TEXT NOT NULL,
    capture_interval_seconds INTEGER NOT NULL,
    application_id TEXT NULL,
    schema_version INTEGER NOT NULL,
    json_metrics_summary TEXT NOT NULL,
    database_received_utc_timestampz TEXT DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);
CREATE INDEX IF NOT EXISTS idx_{tableName}_captured_utc ON ""{tableName}""(captured_utc);
CREATE INDEX IF NOT EXISTS idx_{tableName}_installation_id ON ""{tableName}""(installation_id);
";

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return 1;
    }
}
