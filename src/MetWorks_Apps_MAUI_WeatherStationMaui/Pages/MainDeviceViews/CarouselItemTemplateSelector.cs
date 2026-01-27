namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public class CarouselItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? MainViewTemplate { get; set; }
    public DataTemplate? DetailsTemplate { get; set; }

    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
    {
        if (item is CarouselItemViewModel vm)
        {
            // Decide template based on PageKey
            if (vm.PageKey.StartsWith("MainView", StringComparison.OrdinalIgnoreCase))
                return MainViewTemplate;

            if (vm.PageKey.Equals(nameof(DetailsContent), StringComparison.OrdinalIgnoreCase) ||
                vm.PageKey.IndexOf("detail", StringComparison.OrdinalIgnoreCase) >= 0)
                return DetailsTemplate;
        }

        return MainViewTemplate;
    }
}
