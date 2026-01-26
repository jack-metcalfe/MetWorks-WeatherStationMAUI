namespace MetWorks.Apps.MAUI.WeatherStationMaui;
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    // Constructor used at runtime to inject services and show the splash as the initial Shell content
    public AppShell(ILogger iLogger, ISettingRepository iSettingRepository, IEventRelayBasic iEventRelayBasic)
    {
        InitializeComponent();
        RegisterRoutes();

        // Build initial Shell items programmatically so we can inject the splash page with DI
        try
        {
            var splashPage = new Views.InitializationSplashPage(iLogger, iSettingRepository, iEventRelayBasic);

            var splashContent = new ShellContent
            {
                Content = splashPage,
                Route = "splash",
                Title = "Starting"
            };

            var section = new ShellSection { Title = "Start" };
            section.Items.Add(splashContent);

            var item = new ShellItem();
            item.Items.Add(section);

            this.Items.Clear();
            this.Items.Add(item);
        }
        catch
        {
            // Ignore - Shell will be empty and pages can be opened later
        }
    }

    void RegisterRoutes()
    {
        try
        {
            Routing.RegisterRoute("MainView1920x1200", typeof(Pages.MainDeviceViews.MainView1920x1200));
            Routing.RegisterRoute("MainView2304x1440", typeof(Pages.MainDeviceViews.MainView2304x1440));
            Routing.RegisterRoute("MainView1440x2304", typeof(Pages.MainDeviceViews.MainView1440x2304));
            Routing.RegisterRoute("MainView1812x2176", typeof(Pages.MainDeviceViews.MainView1812x2176));
            Routing.RegisterRoute("MainView2176x1812", typeof(Pages.MainDeviceViews.MainView2176x1812));
        }
        catch { /* ignore routing registration failures */ }
    }
}
