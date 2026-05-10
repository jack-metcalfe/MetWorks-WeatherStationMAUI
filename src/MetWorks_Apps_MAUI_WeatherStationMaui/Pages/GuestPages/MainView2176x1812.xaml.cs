namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

public partial class MainView2176x1812 : ContentView
{
    public MainView2176x1812(
        WeatherViewModel viewModel
    )
    {
        InitializeComponent();
        BindingContext = viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;
        TextMeasure.ApplyDateTimeWidths(LabelDayOfWeek, LabelDate, LabelTime);
    }
}
