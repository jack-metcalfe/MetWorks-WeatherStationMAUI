using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.StationMetadata;

public sealed class StationMetadataRepository : IStationMetadataRepository
{
    ISqliteDatabase? _sqliteDatabase;
    IStationMetadataDatabaseReadiness? _databaseReadiness;
    readonly SemaphoreSlim _readinessGate = new(1, 1);
    bool _schemaEnsured = false;

    public StationMetadataRepository()
    {
    }

    public Task<bool> InitializeAsync(
        ISqliteDatabase sqliteDatabase,
        IStationMetadataDatabaseReadiness stationMetadataDatabaseReadiness,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);
        ArgumentNullException.ThrowIfNull(stationMetadataDatabaseReadiness);

        _sqliteDatabase = sqliteDatabase;
        _databaseReadiness = stationMetadataDatabaseReadiness;
        return Task.FromResult(true);
    }

    async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_schemaEnsured)
            return;

        await _readinessGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_schemaEnsured)
                return;

            var readiness = _databaseReadiness
                ?? throw new InvalidOperationException("StationMetadataRepository is not initialized (databaseReadiness).");

            await readiness.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            _schemaEnsured = true;
        }
        finally
        {
            _readinessGate.Release();
        }
    }

    ISqliteDatabase GetInitializedSqliteDatabase() =>
        _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(StationMetadataRepository)} has not been initialized.");

    public async Task InsertAsync(StationMetadataInsertRow row, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (string.IsNullOrWhiteSpace(row.Id))
            throw new ArgumentException("Id is required.", nameof(row));

        if (row.StationId < 0)
            throw new ArgumentOutOfRangeException(nameof(row), "StationId must be non-negative.");

        if (string.IsNullOrWhiteSpace(row.JsonDocumentOriginal))
            throw new ArgumentException("JsonDocumentOriginal is required.", nameof(row));

        const string sql = """
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
);
""";

        var sqliteDatabase = GetInitializedSqliteDatabase();
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var session = await sqliteDatabase.OpenSessionAsync(cancellationToken).ConfigureAwait(false);

        _ = await session.ExecuteAsync(
            sql,
            [
                new DbParam("$id", row.Id),
                new DbParam("$app_ts", row.RetrievedUtc.ToUniversalTime().ToString("O")),
                new DbParam("$station_id", row.StationId),
                new DbParam("$station_name", row.StationName is null ? DBNull.Value : row.StationName),
                new DbParam("$tempest_device_name", row.TempestDeviceName is null ? DBNull.Value : row.TempestDeviceName),
                new DbParam("$lat", row.Latitude is null ? DBNull.Value : row.Latitude.Value),
                new DbParam("$lon", row.Longitude is null ? DBNull.Value : row.Longitude.Value),
                new DbParam("$elev", row.ElevationMeters is null ? DBNull.Value : row.ElevationMeters.Value),
                new DbParam("$json", row.JsonDocumentOriginal),
                new DbParam("$installation_id", string.IsNullOrWhiteSpace(row.InstallationId) ? DBNull.Value : row.InstallationId),
            ],
            cancellationToken).ConfigureAwait(false);
    }
}
