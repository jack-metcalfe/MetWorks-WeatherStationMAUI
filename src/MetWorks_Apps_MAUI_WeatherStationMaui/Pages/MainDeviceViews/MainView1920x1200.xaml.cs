namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public partial class MainView1920x1200 : ContentView
{
    private readonly WeatherViewModel _viewModel;

    public MainView1920x1200(
        WeatherViewModel viewModel,
        ILoggerResilient? iResilientLogger = null
    )
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        // Example: await resilient logger readiness if the page needs logging guaranteed to be available
        _ = Task.Run(async () =>
        {
            try
            {
                if (iResilientLogger is not null)
                {
                    await iResilientLogger.Ready.ConfigureAwait(false);
                }
            }
            catch { }
        });
    }
}