namespace MetWorks.Apps.MAUI.WeatherStationMaui;
public partial class AppShell : Shell
{
    // Constructor used at runtime to inject services and show the splash as the initial Shell content
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    void RegisterRoutes()
    {
        try
        {
            Routing.RegisterRoute(nameof(Pages.MainDeviceViews.MainViewPage), typeof(Pages.MainDeviceViews.MainViewPage));
            Routing.RegisterRoute("MainView", typeof(Pages.MainDeviceViews.MainViewPage));
        }
        catch { /* ignore routing registration failures */ }
    }
}
