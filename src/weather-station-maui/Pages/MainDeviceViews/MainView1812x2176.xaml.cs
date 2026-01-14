using MetWorksWeather.ViewModels;

namespace MetWorksWeather.Pages.MainDeviceViews;

public partial class MainView1812x2176 : ContentPage
{
    private readonly LargeFormatWeatherViewModel _viewModel;

    public MainView1812x2176()
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