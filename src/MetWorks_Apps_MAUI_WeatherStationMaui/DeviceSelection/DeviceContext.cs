namespace MetWorks.Apps.MAUI.WeatherStationMaui.DeviceSelection;

using Microsoft.Maui.Devices;

/// <summary>
/// Snapshot of device + display characteristics used for selecting UI variants.
/// Prefer dp-based metrics for layout decisions; keep identity fields for overrides.
/// </summary>
public sealed record DeviceContext(
    string Platform,
    string Manufacturer,
    string Model,
    double Density,
    int WidthPx,
    int HeightPx,
    double WidthDp,
    double HeightDp,
    double MinDp,
    double MaxDp,
    DisplayOrientation Orientation
)
{
    public bool IsLandscape => Orientation == DisplayOrientation.Landscape;
    public bool IsPortrait => Orientation == DisplayOrientation.Portrait;

    public static DeviceContext Current()
    {
        var display = DeviceDisplay.Current.MainDisplayInfo;
        var device = DeviceInfo.Current;

        var widthPx = (int)display.Width;
        var heightPx = (int)display.Height;
        var density = display.Density;

        var widthDp = widthPx / density;
        var heightDp = heightPx / density;

        return new DeviceContext(
            Platform: device.Platform.ToString(),
            Manufacturer: device.Manufacturer ?? string.Empty,
            Model: device.Model ?? string.Empty,
            Density: density,
            WidthPx: widthPx,
            HeightPx: heightPx,
            WidthDp: widthDp,
            HeightDp: heightDp,
            MinDp: Math.Min(widthDp, heightDp),
            MaxDp: Math.Max(widthDp, heightDp),
            Orientation: display.Orientation
        );
    }
}