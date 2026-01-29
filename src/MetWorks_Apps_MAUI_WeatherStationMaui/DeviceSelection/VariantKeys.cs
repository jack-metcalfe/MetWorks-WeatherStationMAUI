namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;

/// <summary>
/// Variant keys identify concrete layout implementations for a logical content view.
/// Keys are strings by design (composite identifiers).
/// </summary>
public static class VariantKeys
{
    public static class DefaultWeather
    {
        public const string Adaptive = "DefaultWeather.Adaptive";

        public const string Compact = "DefaultWeather.Compact";
        public const string Medium = "DefaultWeather.Medium";
        public const string Expanded = "DefaultWeather.Expanded";

        // Current concrete implementations (initial curated set)
        public const string Win1920x1200 = "DefaultWeather.Win1920x1200";
        public const string AndroidZFold4Landscape = "DefaultWeather.Android.ZFold4.Landscape";
    }

    public static class Placeholder
    {
        public const string Default = "Placeholder.Default";
    }
}
