namespace MetWorks.Interfaces;
public interface IStationMetadataProvider
{
    ValueTask<double?> GetStationElevationMetersAsync(CancellationToken cancellationToken = default);

    ValueTask<StationMetadata?> GetStationMetadataAsync(CancellationToken cancellationToken = default);
}

public sealed record StationMetadata(
    long StationId,
    string? StationName,
    long? TempestDeviceId,
    string? TempestDeviceName,
    double? Latitude,
    double? Longitude,
    double? ElevationMeters,
    DateTimeOffset RetrievedUtc
);
