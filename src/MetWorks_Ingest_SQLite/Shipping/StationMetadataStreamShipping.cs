namespace MetWorks.Ingest.SQLite.Shipping;
internal static class StationMetadataStreamShipping
{
    const string StationMetadataDdlScript = @"
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

    public static async Task ShipOnceAsync(
        SqliteConnection conn,
        Guid installationId,
        string source,
        string table,
        int maxBatchRows,
        HttpClient httpClient,
        string endpointUrl,
        CancellationToken token
    )
    {
        await StandardReadingsStreamShipping.ShipOnceAsync(
            conn,
            installationId: installationId,
            source: source,
            table: table,
            ddlScript: StationMetadataDdlScript,
            maxBatchRows: maxBatchRows,
            httpClient: httpClient,
            endpointUrl: endpointUrl,
            token).ConfigureAwait(false);
    }
}
