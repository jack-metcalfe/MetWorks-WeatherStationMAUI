namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;

/// <summary>
/// Provides the set of logical guest pages (content keys) associated with each host page.
/// This is a data/catalog layer used by host pages to determine which logical content to request.
/// </summary>
public interface IHostCompositionCatalog
{
    bool TryGetComposition(HostKey hostKey, out HostComposition composition);
}
