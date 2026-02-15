using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.StationMetadata;

public sealed class StationMetadataRepository : IStationMetadataRepository
{
    ISqliteDatabase? _sqliteDatabase;

    public StationMetadataRepository()
    {
    }

    public Task<bool> InitializeAsync(ISqliteDatabase sqliteDatabase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);

        _sqliteDatabase = sqliteDatabase;
        return Task.FromResult(true);
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
