namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

public partial class MainView1812x2176 : ContentView
{
    public MainView1812x2176(
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
