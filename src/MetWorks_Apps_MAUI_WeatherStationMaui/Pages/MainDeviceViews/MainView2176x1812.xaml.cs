using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public partial class MainView2176x1812 : ContentPage
{
    private readonly WeatherViewModel _viewModel;

    public MainView2176x1812()
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