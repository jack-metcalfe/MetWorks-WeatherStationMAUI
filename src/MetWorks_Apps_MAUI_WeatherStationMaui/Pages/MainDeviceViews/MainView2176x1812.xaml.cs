using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public partial class MainView2176x1812 : ContentView
{
    private readonly WeatherViewModel _viewModel;

    public MainView2176x1812(
        WeatherViewModel viewModel
    )
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}
