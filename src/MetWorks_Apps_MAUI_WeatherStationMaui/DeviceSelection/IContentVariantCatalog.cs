namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;
/// <summary>
/// Resolves a (logical content key, variant key) pair to a concrete MAUI View type.
/// Backed by code/DI inventory (and later DDI generation).
/// </summary>
public interface IContentVariantCatalog
{
    bool TryGetViewType(LogicalContentKey content, string variantKey, out Type viewType);
}
