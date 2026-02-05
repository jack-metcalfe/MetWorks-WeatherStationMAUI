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

        if (content == LogicalContentKey.LiveWind)
        {
            viewType = typeof(LiveWindAdaptive);
            return true;
        }

        if (content == LogicalContentKey.MetricsOne)
        {
            viewType = typeof(MetricsOne);
            return true;
        }

        if (content != LogicalContentKey.HomePage)
            return false;

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

            _ => null
        };

        return viewType is not null;
    }
}
