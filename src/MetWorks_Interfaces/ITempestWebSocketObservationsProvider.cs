namespace MetWorks.Interfaces;

/// <summary>
/// Maintains a single Tempest hosted WebSocket connection (when enabled) and publishes received messages via the event relay.
/// </summary>
public interface ITempestWebSocketObservationsProvider
{
    /// <summary>
    /// Returns the most recent WebSocket message snapshot received, if any.
    /// </summary>
    ValueTask<TempestWebSocketObservationsSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default);
}
