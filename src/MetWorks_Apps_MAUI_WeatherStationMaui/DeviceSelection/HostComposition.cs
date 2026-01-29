namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;

/// <summary>
/// Describes which logical guest pages (content keys) a host page should present.
/// The order of <see cref="Slots"/> is the presentation order.
/// </summary>
public sealed record HostComposition(HostKey HostKey, IReadOnlyList<LogicalContentKey> Slots);
