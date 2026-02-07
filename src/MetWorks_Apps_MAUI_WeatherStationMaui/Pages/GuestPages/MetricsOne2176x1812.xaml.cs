namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

public partial class MetricsOne2176x1812 : ContentView
{
    public MetricsOne2176x1812(MetricsOneViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        InitializeComponent();
        BindingContext = vm;

        vm.ScrollJsonUp += (_, _) => _ = ScrollJsonAsync(-1);
        vm.ScrollJsonDown += (_, _) => _ = ScrollJsonAsync(1);

        btnJsonUp.Clicked += (_, _) => _ = ScrollJsonAsync(-1);
        btnJsonDown.Clicked += (_, _) => _ = ScrollJsonAsync(1);
    }

    async Task ScrollJsonAsync(int direction)
    {
        try
        {
            var delta = 180d * direction;
            await JsonScroll.ScrollToAsync(0, JsonScroll.ScrollY + delta, true);
        }
        catch { }
    }
}
