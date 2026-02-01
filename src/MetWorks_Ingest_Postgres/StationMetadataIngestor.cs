namespace MetWorks.Ingest.Postgres;
public sealed class StationMetadataIngestor : ServiceBase, IStationMetadataPersister
{
    string _connectionString = string.Empty;
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
            LookupDictionaries.XMLToPostgreSQLGroupSettingsDefinition.BuildSettingPath(SettingConstants.XMLToPostgreSQL_connectionString)
        );

        var iid = iInstanceIdentifier.GetOrCreateInstallationId();
        if (!Guid.TryParse(iid, out _installationIdGuid))
            _installationIdGuid = Guid.Empty;

        // Subscribe to station metadata updates.
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

        if (string.IsNullOrWhiteSpace(_connectionString))
            return;

        cancellationToken.ThrowIfCancellationRequested();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await EnsureTableAsync(conn, cancellationToken).ConfigureAwait(false);

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = false });

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO public.station_metadata (
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
    @id,
    @app_ts,
    @station_id,
    @station_name,
    @tempest_device_name,
    @lat,
    @lon,
    @elev,
    @json,
    @installation_id
);";

        cmd.Parameters.AddWithValue("id", IdGenerator.CreateCombGuid().ToString());
        cmd.Parameters.AddWithValue("app_ts", metadata.RetrievedUtc.UtcDateTime);
        cmd.Parameters.AddWithValue("station_id", metadata.StationId);
        cmd.Parameters.AddWithValue("station_name", (object?)metadata.StationName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("tempest_device_name", (object?)metadata.TempestDeviceName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("lat", (object?)metadata.Latitude ?? DBNull.Value);
        cmd.Parameters.AddWithValue("lon", (object?)metadata.Longitude ?? DBNull.Value);
        cmd.Parameters.AddWithValue("elev", (object?)metadata.ElevationMeters ?? DBNull.Value);
        cmd.Parameters.AddWithValue("json", json);

        if (_installationIdGuid != Guid.Empty)
            cmd.Parameters.AddWithValue("installation_id", NpgsqlTypes.NpgsqlDbType.Uuid, _installationIdGuid);
        else
            cmd.Parameters.AddWithValue("installation_id", DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    static async Task EnsureTableAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS public.station_metadata
(
    id TEXT PRIMARY KEY,
    application_received_utc_timestampz TIMESTAMPTZ NOT NULL,
    database_received_utc_timestampz TIMESTAMPTZ DEFAULT now(),
    station_id BIGINT NOT NULL,
    station_name TEXT NULL,
    tempest_device_name TEXT NULL,
    latitude DOUBLE PRECISION NULL,
    longitude DOUBLE PRECISION NULL,
    elevation_meters DOUBLE PRECISION NULL,
    json_document_original JSON NOT NULL,
    json_document_jsonb JSONB GENERATED ALWAYS AS (json_document_original) STORED,
    installation_id UUID NULL
);
CREATE INDEX IF NOT EXISTS idx_station_metadata_station_id ON public.station_metadata(station_id);
CREATE INDEX IF NOT EXISTS idx_station_metadata_installation_id ON public.station_metadata(installation_id);
CREATE INDEX IF NOT EXISTS idx_station_metadata_application_received ON public.station_metadata(application_received_utc_timestampz);
";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
