namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection.Overrides;
public sealed class DeviceOverridesDocument
{
    public int Version { get; init; } = 1;
    public List<DeviceOverrideEntry> Devices { get; init; } = new();
}
