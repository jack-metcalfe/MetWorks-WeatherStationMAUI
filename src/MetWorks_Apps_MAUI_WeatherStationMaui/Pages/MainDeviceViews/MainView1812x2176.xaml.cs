using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public partial class MainView1812x2176 : ContentView
{
    private readonly WeatherViewModel _viewModel;

    public MainView1812x2176(
        WeatherViewModel viewModel
    )
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}
