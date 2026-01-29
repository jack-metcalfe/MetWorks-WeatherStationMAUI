namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection.Overrides;
public interface IDeviceOverrideSource
{
    bool TryGetOverride(LogicalContentKey content, DeviceContext deviceContext, out string variantKey);
}
