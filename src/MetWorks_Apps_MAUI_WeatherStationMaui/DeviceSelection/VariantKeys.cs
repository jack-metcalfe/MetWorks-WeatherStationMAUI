namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;

/// <summary>
/// Variant keys identify concrete layout implementations for a logical content view.
/// Keys are strings by design (composite identifiers).
/// </summary>
public static class VariantKeys
{
    public static class DefaultWeather
    {
        public const string Adaptive = "HomePage.Adaptive";

        public const string Compact = "HomePage.Compact";
        public const string Medium = "HomePage.Medium";
        public const string Expanded = "HomePage.Expanded";

        // Current concrete implementations (initial curated set)
        public const string Win1920x1200 = "HomePage.Win1920x1200";
        public const string And2176x1812 = "HomePage.And2176x1812";
        public const string And2304x1440 = "HomePage.And2304x1440";
    }
    public static class Placeholder
    {
        public const string Default = "Placeholder.Default";
    }
}
