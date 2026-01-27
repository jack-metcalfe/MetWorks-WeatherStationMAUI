namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

/// <summary>
/// Descriptor used as the CarouselView ItemsSource item.
/// Carries the desired view type name and the ViewModel to bind.
/// </summary>
public sealed class CarouselItemDescriptor
{
    public string ViewTypeName { get; init; }
    public View? ViewInstance { get; set; }
    public WeatherViewModel ViewModel { get; init; }

    public CarouselItemDescriptor(string viewTypeName, WeatherViewModel viewModel)
    {
        ViewTypeName = viewTypeName ?? throw new ArgumentNullException(nameof(viewTypeName));
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
