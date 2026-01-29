namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

public partial class LiveWindAdaptive : ContentView
{
    public LiveWindAdaptive(
        WeatherViewModel viewModel
    )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
