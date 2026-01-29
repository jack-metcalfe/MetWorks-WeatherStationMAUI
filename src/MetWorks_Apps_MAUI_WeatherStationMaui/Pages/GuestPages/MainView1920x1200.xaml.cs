namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

public partial class MainView1920x1200 : ContentView
{
    public MainView1920x1200(
        WeatherViewModel viewModel
    )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}