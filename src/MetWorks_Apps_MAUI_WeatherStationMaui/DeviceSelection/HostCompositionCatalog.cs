namespace MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;

public sealed class HostCompositionCatalog : IHostCompositionCatalog
{
    static readonly HostComposition MainSwipe = new(
        HostKey.MainSwipe,
        new[] { LogicalContentKey.HomePage, LogicalContentKey.LiveWind, LogicalContentKey.MetricsOne }
    );

    public bool TryGetComposition(HostKey hostKey, out HostComposition composition)
    {
        if (hostKey == HostKey.MainSwipe)
        {
            composition = MainSwipe;
            return true;
        }

        composition = default!;
        return false;
    }
}
