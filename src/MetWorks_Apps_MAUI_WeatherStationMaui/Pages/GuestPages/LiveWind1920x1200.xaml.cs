namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

public partial class LiveWind1920x1200 : ContentView
{
    public LiveWind1920x1200(
        WeatherViewModel viewModel
    )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
