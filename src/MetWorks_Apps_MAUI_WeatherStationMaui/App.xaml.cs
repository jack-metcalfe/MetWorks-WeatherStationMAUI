namespace MetWorks.Apps.MAUI.WeatherStationMaui;
public partial class App : Application
{
    Task? _initializationTask;
    CancellationTokenSource? _shutdownCts;
    ILoggerResilient _iLoggerResilient;
    ISettingRepository _iSettingRepository;
    IEventRelayBasic _iEventRelayBasic;
    
    public App(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic
    )
    {
        _iLoggerResilient = iLoggerResilient;
        _iSettingRepository = iSettingRepository;
        _iEventRelayBasic = iEventRelayBasic;  

        InitializeComponent();

        _shutdownCts = new CancellationTokenSource();
        // Start background initialization without blocking the UI thread.
        // Store the task so we can observe failures and optionally show an error to the user.
        _initializationTask = Task.Run(async () =>
        {
            try
            {
                await StartupInitializer.InitializeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Surface to UI thread for user-visible error
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var wnd = Application.Current?.Windows.FirstOrDefault();
                        if (wnd != null)
                        {
                            await ShowErrorAsync(wnd, ex).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
                }
                catch { /* swallow UI failures during error reporting */ }
            }
        });
    }
    
    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Select and load appropriate view for this device
        // Log device info for debugging
        Debug.WriteLine("🔍 Device Detection:");
        try
        {
            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
            var deviceInfo = DeviceInfo.Current;
            Debug.WriteLine($"Device: {deviceInfo.Manufacturer} {deviceInfo.Model}");
            Debug.WriteLine($"Platform: {deviceInfo.Platform} {deviceInfo.Version}");
            Debug.WriteLine($"Resolution: {displayInfo.Width}x{displayInfo.Height} px @ density {displayInfo.Density:F2}");
            Debug.WriteLine($"Orientation: {displayInfo.Orientation}");
        }
        catch { }

        // Use a single AppShell instance resolved from MAUI DI to avoid multiple Shell instances
        // and to ensure any dependencies are created consistently through the service provider.
        var appShell = activationState?.Context?.Services?.GetService<AppShell>() ?? new AppShell();
        var window = new Window(appShell);

        // Cleanup on window destruction
        window.Destroying += (s, e) =>
        {
            _shutdownCts?.Cancel();
            _shutdownCts?.Dispose();
        };

        return window;
    }
    
    private async Task ShowErrorAsync(Window window, Exception exception)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await window.Page.DisplayAlert(
                @"Initialization Error",
                $"Background services failed to start:\n{exception.Message}",
                @"OK"
            );
        });
    }
}