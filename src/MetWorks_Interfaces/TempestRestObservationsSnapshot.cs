namespace MetWorks.Interfaces;

/// <summary>
/// Superset observations snapshot fetched from the Tempest/WeatherFlow REST API.
/// This message is intended for publication via <see cref="IEventRelayBasic"/> and preserves the full JSON payload.
/// </summary>
public sealed record TempestRestObservationsSnapshot(
    long StationId,
    DateTimeOffset RetrievedUtc,
    string RawJson
);
