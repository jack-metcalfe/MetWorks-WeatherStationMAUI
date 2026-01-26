namespace MetWorks.Apps.MAUI.WeatherStationMaui;
public partial class App : Application
{
    Task? _initializationTask;
    CancellationTokenSource? _shutdownCts;
    ILogger _iLogger;
    ISettingRepository _iSettingRepository;
    IEventRelayBasic _iEventRelayBasic;
    
    public App(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic
    )
    {
        _iLogger = iLogger;
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
        Debug.WriteLine(DeviceViewSelector.GetDeviceInfo());

        // Create AppShell (which hosts the splash as initial ShellContent) so only one splash instance is created
        var appShell = new AppShell(_iLogger, _iSettingRepository, _iEventRelayBasic);
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