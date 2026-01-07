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
        var window = new Window(new AppShell());

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