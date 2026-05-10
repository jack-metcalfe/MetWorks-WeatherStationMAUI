namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

public partial class MainView1920x1200 : ContentView
{
    public MainView1920x1200(
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