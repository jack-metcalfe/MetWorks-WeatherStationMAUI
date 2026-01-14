using Microsoft.Maui.ApplicationModel;

namespace MetWorksWeather;

public partial class App : Application
{
    private Task? _initializationTask;
    private CancellationTokenSource? _shutdownCts;
    
    public App()
    {
        InitializeComponent();

        _shutdownCts = new CancellationTokenSource();
    }
    
    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Select and load appropriate view for this device
        // Log device info for debugging
        System.Diagnostics.Debug.WriteLine("🔍 Device Detection:");
        System.Diagnostics.Debug.WriteLine(DeviceViewSelector.GetDeviceInfo());

        var mainPage = DeviceViewSelector.GetViewForCurrentDevice();
        var window = new Window(mainPage);

        window.Created += async (s, e) =>
        {
            try
            {
                _initializationTask = StartupInitializer.InitializeAsync();
                await _initializationTask;
            }
            catch (Exception exception)
            {
                await ShowErrorAsync(window, exception);
            }
        };
        
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