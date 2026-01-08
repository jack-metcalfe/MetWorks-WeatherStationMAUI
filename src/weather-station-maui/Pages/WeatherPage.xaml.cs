using MetWorksWeather.ViewModels;

namespace MetWorksWeather.Pages;

public partial class WeatherPage : ContentPage
{
    private readonly WeatherViewModel _viewModel;

    public WeatherPage()
    {
        InitializeComponent();
        
        // No dependency injection needed - ViewModel uses ISingletonEventRelay
        _viewModel = new WeatherViewModel();
        BindingContext = _viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel?.Dispose();
    }
}
