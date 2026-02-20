namespace MetWorks.Interfaces;
public interface ITempestRestClient
{
    Task<TempestStationSnapshot> GetStationSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the Tempest/WeatherFlow Better Forecast payload for the configured station.
    /// The payload is returned as raw JSON to avoid binding to a rigid external schema.
    /// </summary>
    Task<TempestBetterForecastSnapshot> GetBetterForecastAsync(CancellationToken cancellationToken = default);
}

public sealed record TempestStationSnapshot(
    long StationId,
    DateTimeOffset RetrievedUtc,
    string RawJson
);

public sealed record TempestBetterForecastSnapshot(
    long StationId,
    DateTimeOffset RetrievedUtc,
    string RawJson
);
