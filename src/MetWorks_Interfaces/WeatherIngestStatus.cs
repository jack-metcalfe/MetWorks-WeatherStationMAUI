namespace MetWorks.Interfaces;

/// <summary>
/// UI-friendly status snapshot describing which ingest source is active and whether each input source is available/fresh.
/// Intended to be published via <see cref="IEventRelayBasic"/>.
/// </summary>
public sealed record WeatherIngestStatus(
    WeatherIngestSourceMode SourceMode,
    WeatherIngestSource ActiveSource,
    bool UdpAvailable,
    bool UdpIsFresh,
    DateTimeOffset? UdpLastReceivedUtc,
    bool RestAvailable,
    bool RestIsFresh,
    DateTimeOffset? RestLastRetrievedUtc,
    string? UdpLastError = null,
    string? RestLastError = null
);
