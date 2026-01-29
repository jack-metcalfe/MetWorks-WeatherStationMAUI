using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

public partial class MainView2304x1440 : ContentView
{
    private readonly WeatherViewModel _viewModel;

    public MainView2304x1440(
        WeatherViewModel viewModel
    )
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}