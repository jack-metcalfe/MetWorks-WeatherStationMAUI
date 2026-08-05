namespace MetWorks.Persistence.StationMetadata;

public sealed record StationMetadataInsertRow(
    string Id,
    DateTime RetrievedUtc,
    long StationId,
    string? StationName,
    string? TempestDeviceName,
    double? Latitude,
    double? Longitude,
    double? ElevationMeters,
    string JsonDocumentOriginal,
    string? InstallationId);
