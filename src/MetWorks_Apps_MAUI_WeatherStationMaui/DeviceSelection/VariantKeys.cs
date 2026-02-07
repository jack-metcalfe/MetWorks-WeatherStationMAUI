namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;

/// <summary>
/// Variant keys identify concrete layout implementations for a logical content view.
/// Keys are strings by design (composite identifiers).
/// </summary>
public static class VariantKeys
{
    /// <summary>
    /// As things evolved DefaultWeather became a poor name for this set of variants, something like HomePage variants would be more accurate.
    /// Renaming now would be a breaking change, so we keep the old name for compatibility.  Perhaps a rename down the road will be appropriate.
    /// </summary>
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

    public static class MetricsOne
    {
        // Current concrete implementations (initial curated set)
        public const string Win1920x1200 = "MetricsOne.Win1920x1200";
        public const string And2176x1812 = "MetricsOne.And2176x1812";
        public const string And2304x1440 = "MetricsOne.And2304x1440";
    }

    public static class LiveWind
    {
        // Current concrete implementations (initial curated set)
        public const string Win1920x1200 = "LiveWind.Win1920x1200";
        public const string And2176x1812 = "LiveWind.And2176x1812";
        public const string And2304x1440 = "LiveWind.And2304x1440";
    }

    public static class Placeholder
    {
        public const string Default = "Placeholder.Default";
    }
}
