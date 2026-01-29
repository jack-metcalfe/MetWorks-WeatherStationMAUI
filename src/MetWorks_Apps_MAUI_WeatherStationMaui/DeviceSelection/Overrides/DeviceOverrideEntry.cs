namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection.Overrides;
public sealed class DeviceOverrideEntry
{
    public string? Id { get; init; }
    public string Platform { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;

    public Dictionary<string, object> Overrides { get; init; } = new(StringComparer.Ordinal);
    public string? Notes { get; init; }
}