namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

public partial class MainView1440x2304 : ContentView
{
    public MainView1440x2304(
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
