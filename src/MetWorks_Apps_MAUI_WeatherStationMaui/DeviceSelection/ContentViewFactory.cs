namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;
public sealed class ContentViewFactory : IContentViewFactory
{
    readonly IServiceProvider _services;
    readonly IContentVariantCatalog _catalog;
    readonly IDeviceOverrideSource _overrides;

    public ContentViewFactory(
        IServiceProvider services, 
        IContentVariantCatalog catalog, 
        IDeviceOverrideSource overrides
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(overrides);

        _services = services;
        _catalog = catalog;
        _overrides = overrides;
    }

    public View Create(
        LogicalContentKey content, 
        DeviceContext deviceContext
    )
    {
        var variantKey = SelectVariantKey(content, deviceContext);

        if (
            !_catalog.TryGetViewType(
                content, variantKey, out var viewType
            )
        )
            throw new InvalidOperationException(
                $"No view type registered for {content} / {variantKey}."
            );

        var view = ActivatorUtilities.CreateInstance(
            _services, viewType
        ) as View;
        if (view is null)
            throw new InvalidOperationException(
                $"Failed to create view instance for {viewType.FullName}."
            );

        return view;
    }

    string SelectVariantKey(LogicalContentKey content, DeviceContext deviceContext)
    {
        if (_overrides.TryGetOverride(content, deviceContext, out var overridden))
            return overridden;

        return content switch
        {
            LogicalContentKey.LiveWind 
                => VariantKeys.Placeholder.Default,

            LogicalContentKey.HomePage
                => SelectDefaultWeatherVariant(
                        deviceContext
                   ),

            _ => throw new ArgumentOutOfRangeException(
                nameof(content), content, "Unknown logical content."
            )
        };
    }

    static string SelectDefaultWeatherVariant(DeviceContext deviceContext)
    {
        // Screen-class fallback (dp-based)
        var minDp = deviceContext.MinDp;
        if (minDp < 600)
            return VariantKeys.DefaultWeather.Compact;

        if (minDp < 840)
            return VariantKeys.DefaultWeather.Medium;

        if (minDp >= 840)
            return VariantKeys.DefaultWeather.Expanded;

        return VariantKeys.DefaultWeather.Adaptive;
    }
}
