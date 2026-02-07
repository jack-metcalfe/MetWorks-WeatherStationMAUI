namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

public partial class LiveWind2304x1440 : ContentView
{
    public LiveWind2304x1440(
        WeatherViewModel viewModel
    )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
