using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public partial class MainView2304x1440 : ContentPage
{
    private readonly WeatherViewModel _viewModel;

    public MainView2304x1440()
    {
        InitializeComponent();
        _viewModel = new WeatherViewModel();
        BindingContext = _viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel?.Dispose();
    }
}