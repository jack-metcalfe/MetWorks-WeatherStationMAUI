namespace MetWorks.Ingest.SQLite;
public sealed class StationMetadataIngestor : ServiceBase, IStationMetadataPersister
{
    string _connectionString = string.Empty;
    string _dbPath = string.Empty;
    Guid _installationIdGuid;

    public Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        CancellationToken externalCancellation = default
    )
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);

        InitializeBase(
            iLoggerResilient.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation
        );

        _connectionString = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_connectionString)
        );

        _dbPath = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_dbPath)
        );

        if (string.IsNullOrWhiteSpace(_connectionString) && !string.IsNullOrWhiteSpace(_dbPath))
        {
            var resolvedDbPath = Path.IsPathRooted(_dbPath)
                ? _dbPath
                : Path.Combine(AppContext.BaseDirectory, _dbPath);

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = resolvedDbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
        }

        var iid = iInstanceIdentifier.GetOrCreateInstallationId();
        if (!Guid.TryParse(iid, out _installationIdGuid))
            _installationIdGuid = Guid.Empty;

        IEventRelayBasic.Register<StationMetadata>(this, md =>
        {
            StartBackground(ct => PersistAsync(md, ct));
        });

        MarkReady();
        return Task.FromResult(true);
    }

    public async Task PersistAsync(StationMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        await Ready.ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(_connectionString)) return;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            await EnsureTableAsync(conn, cancellationToken).ConfigureAwait(false);

            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = false });

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO station_metadata (
    id,
    application_received_utc_timestampz,
    station_id,
    station_name,
    tempest_device_name,
    latitude,
    longitude,
    elevation_meters,
    json_document_original,
    installation_id
)
VALUES (
    $id,
    $app_ts,
    $station_id,
    $station_name,
    $tempest_device_name,
    $lat,
    $lon,
    $elev,
    $json,
    $installation_id
);";

            cmd.Parameters.AddWithValue("$id", IdGenerator.CreateCombGuid().ToString());
            cmd.Parameters.AddWithValue("$app_ts", metadata.RetrievedUtc.UtcDateTime);
            cmd.Parameters.AddWithValue("$station_id", metadata.StationId);
            cmd.Parameters.AddWithValue("$station_name", (object?)metadata.StationName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$tempest_device_name", (object?)metadata.TempestDeviceName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$lat", (object?)metadata.Latitude ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$lon", (object?)metadata.Longitude ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$elev", (object?)metadata.ElevationMeters ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$json", json);
            cmd.Parameters.AddWithValue("$installation_id", _installationIdGuid != Guid.Empty ? _installationIdGuid.ToString() : DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            ILogger.Error("Error persisting station metadata: {Message}", exception);
        }
    }

    static async Task EnsureTableAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
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
";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
