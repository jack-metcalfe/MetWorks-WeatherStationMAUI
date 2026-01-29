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
        // ShellContent routes defined in AppShell.xaml don't require Route registration.
        // Only register routes for pages you navigate to that are NOT ShellContent.
        // Keeping this method in case additional non-shell routes are added later.
    }
}
