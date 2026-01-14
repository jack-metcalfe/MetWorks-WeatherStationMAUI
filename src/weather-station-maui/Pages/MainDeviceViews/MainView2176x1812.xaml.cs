using MetWorksWeather.ViewModels;

namespace MetWorksWeather.Pages.MainDeviceViews;

public partial class MainView2176x1812 : ContentPage
{
    private readonly LargeFormatWeatherViewModel _viewModel;

    public MainView2176x1812()
    {
        InitializeComponent();
        _viewModel = new LargeFormatWeatherViewModel();
        BindingContext = _viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel?.Dispose();
    }
}