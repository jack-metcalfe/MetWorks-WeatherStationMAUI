namespace MetWorks.Interfaces;

/// <summary>
/// Periodically fetches Tempest/WeatherFlow station observations over REST and publishes them via the event relay.
/// Also supports on-demand refresh without shifting the scheduled polling cadence.
/// </summary>
public interface ITempestRestObservationsProvider
{
    /// <summary>
    /// Returns the latest REST observations snapshot the provider has successfully fetched, if any.
    /// This is the superset message intended for publication via <see cref="IEventRelayBasic"/>.
    /// </summary>
    ValueTask<TempestRestObservationsSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests an immediate refresh of observations. Implementations should not delay the next scheduled refresh.
    /// </summary>
    Task RequestRefreshAsync(CancellationToken cancellationToken = default);
}
