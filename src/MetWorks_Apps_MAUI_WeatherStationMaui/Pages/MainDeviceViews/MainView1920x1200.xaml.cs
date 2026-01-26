using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public partial class MainView1920x1200 : ContentPage
{
    private readonly WeatherViewModel _viewModel;

    public MainView1920x1200(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic
    )
    {
        InitializeComponent();
        _viewModel = new WeatherViewModel(iLogger, iSettingRepository, iEventRelayBasic);
        BindingContext = _viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel?.Dispose();
    }
}