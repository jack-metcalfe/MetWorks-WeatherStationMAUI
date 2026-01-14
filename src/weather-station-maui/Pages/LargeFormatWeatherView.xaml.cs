using MetWorksWeather.ViewModels;

namespace MetWorksWeather.Pages;

public partial class LargeFormatWeatherView : ContentPage
{
    private readonly LargeFormatWeatherViewModel _viewModel;

    public LargeFormatWeatherView()
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