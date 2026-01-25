namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public partial class MainView1440x2304 : ContentPage
{
    private readonly WeatherViewModel _viewModel;

    public MainView1440x2304()
    {
        InitializeComponent();
        _viewModel = new ();
        BindingContext = _viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel?.Dispose();
    }
}