namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;
public sealed class ContentVariantCatalog : IContentVariantCatalog
{
    public bool TryGetViewType(
        LogicalContentKey content, 
        string variantKey, 
        out Type viewType
    )
    {
        ArgumentNullException.ThrowIfNull(variantKey);

        viewType = default!;

        if (content != LogicalContentKey.HomePage && content != LogicalContentKey.MetricsOne && content != LogicalContentKey.LiveWind && content != LogicalContentKey.ForecastHours)
            return false;

        // Baseline fallback mapping for unknown devices or missing curated overrides.
        // This prevents crashes when the selector chooses Placeholder.Default.
        if (variantKey == VariantKeys.Placeholder.Default)
        {
            viewType = content switch
            {
                LogicalContentKey.HomePage => typeof(MainView1920x1200),
                LogicalContentKey.LiveWind => typeof(LiveWindAdaptive),
                LogicalContentKey.MetricsOne => typeof(MetricsOne1920x1200),
                LogicalContentKey.ForecastHours => typeof(ForecastHoursAdaptive),
                _ => null
            };

            return viewType is not null;
        }

        viewType = variantKey switch
        {
            var k when k == VariantKeys.DefaultWeather.Win1920x1200 => typeof(MainView1920x1200),
            var k when k == VariantKeys.DefaultWeather.And2176x1812 => typeof(MainView2176x1812),
            var k when k == VariantKeys.DefaultWeather.And2304x1440 => typeof(MainView2304x1440),

            // For now, adaptive + screen-class share the existing default layout
            var k when k == VariantKeys.DefaultWeather.Adaptive => typeof(MainView1920x1200),
            var k when k == VariantKeys.DefaultWeather.Compact => typeof(MainView1920x1200),
            var k when k == VariantKeys.DefaultWeather.Medium => typeof(MainView1920x1200),
            var k when k == VariantKeys.DefaultWeather.Expanded => typeof(MainView1920x1200),

            var k when k == VariantKeys.LiveWind.Win1920x1200 => typeof(LiveWind1920x1200),
            var k when k == VariantKeys.LiveWind.And2176x1812 => typeof(LiveWind2176x1812),
            var k when k == VariantKeys.LiveWind.And2304x1440 => typeof(LiveWind2304x1440),

            var k when k == VariantKeys.MetricsOne.Win1920x1200 => typeof(MetricsOne1920x1200),
            var k when k == VariantKeys.MetricsOne.And2176x1812 => typeof(MetricsOne2176x1812),
            var k when k == VariantKeys.MetricsOne.And2304x1440 => typeof(MetricsOne2304x1440),

            _ => null
        };

        return viewType is not null;
    }
}
