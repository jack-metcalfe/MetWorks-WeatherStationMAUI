using MetWorks.Apps.MAUI.WeatherStationMaui.DeviceSelection;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Views;
public partial class InitializationSplashPage : ContentPage
{
    ILoggerResilient _iLoggerResilient;
    ISettingRepository _iSettingRepository;
    IEventRelayBasic _iEventRelayBasic;

    public InitializationSplashPage(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic
    )
    {
        _iLoggerResilient = iLoggerResilient;
        _iSettingRepository = iSettingRepository;
        _iEventRelayBasic = iEventRelayBasic;
        InitializeComponent();

        // Wire events
        RetryButton.Clicked += async (_, __) =>
        {
            RetryButton.IsVisible = false;
            Spinner.IsRunning = true;
            StatusLabel.Text = "Retrying initialization...";
            await Task.Run(async () => await StartupInitializer.InitializeAsync().ConfigureAwait(false));
        };

        // Subscribe to startup events
        StartupInitializer.StatusChanged += status =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = status;
            });
        };

        StartupInitializer.Initialized += () =>
        {
            // Use InvokeOnMainThreadAsync so we can await Shell navigation
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Spinner.IsRunning = false;
                RetryButton.IsVisible = false;
                // Navigate to main page determined by device profile
                try
                {
                    var route = DeviceViewSelector.GetRouteForCurrentDevice();
                    try
                    {
                        // Ensure an AppShell is the active page on the main window so Shell.Current is available.
                        if (Shell.Current is null)
                        {
                            var mainWindow = Application.Current?.Windows.FirstOrDefault();
                            if (mainWindow != null)
                            {
                                // Replace the current page with AppShell (this makes Shell.Current non-null)
                                mainWindow.Page = new AppShell();
                            }
                            else
                            {
                                // No window available - open a new one containing AppShell
                                Application.Current?.OpenWindow(new Window(new AppShell()));
                            }
                        }

                        // Try shell navigation to the device route
                        if (Shell.Current is not null)
                        {
                            await Shell.Current.GoToAsync(route).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // Final fallback: ensure AppShell is visible
                        try { Application.Current?.OpenWindow(new Window(new AppShell())); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { Debug.WriteLine($"Failed to navigate to device-specific startup page: {ex.Message}"); } catch { }
                    Application.Current?.OpenWindow(new Window(new AppShell()));
                }
            });
        };

        StartupInitializer.InitializationFailed += ex =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Spinner.IsRunning = false;
                RetryButton.IsVisible = true;
                DetailsLabel.IsVisible = true;
                DetailsLabel.Text = ex.Message;
            });
        };
    }
}
