namespace MetWorks.Interfaces;

/// <summary>
/// Request message published via <see cref="IEventRelayBasic"/> to ask the ingest mux to immediately
/// re-publish its latest cached canonical readings and status.
/// Intended to prevent UI "cold start" message loss when the UI registers after the first readings are published.
/// </summary>
public sealed record WeatherIngestWarmStartRequest;
