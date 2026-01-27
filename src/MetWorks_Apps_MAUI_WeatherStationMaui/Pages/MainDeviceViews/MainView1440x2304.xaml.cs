namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public partial class MainView1440x2304 : ContentView
{
    private readonly WeatherViewModel _viewModel;

    public MainView1440x2304(
        WeatherViewModel viewModel
    )
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}
