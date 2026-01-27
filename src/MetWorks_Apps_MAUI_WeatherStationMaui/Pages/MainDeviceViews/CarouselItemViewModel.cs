namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

/// <summary>
/// Small descriptor used as the CarouselView item VM when binding ItemsSource to viewmodels.
/// </summary>
public sealed class CarouselItemViewModel
{
    public string PageKey { get; }
    public MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels.WeatherViewModel ViewModel { get; }
    public View? ViewInstance { get; set; }

    public CarouselItemViewModel(string pageKey, MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels.WeatherViewModel viewModel)
    {
        PageKey = pageKey ?? throw new ArgumentNullException(nameof(pageKey));
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
