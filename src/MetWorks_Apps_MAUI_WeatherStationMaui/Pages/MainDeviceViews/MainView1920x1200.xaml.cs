using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public partial class MainView1920x1200 : ContentPage
{
    private readonly WeatherViewModel _viewModel;

    public MainView1920x1200()
    {
        InitializeComponent();
        _viewModel = new();
        BindingContext = _viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel?.Dispose();
    }
}