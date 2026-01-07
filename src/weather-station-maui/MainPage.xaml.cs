using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

namespace MetWorksWeather;

public partial class MainPage : ContentPage
{
    private int count = 0;
    private Timer? _statusCheckTimer;

    public MainPage()
    {
        InitializeComponent();
        StartServiceStatusMonitoring();
    }

    private void StartServiceStatusMonitoring()
    {
        // Check service status every 5 seconds
        _statusCheckTimer = new Timer(
            UpdateServiceStatus,
            null,
            TimeSpan.Zero,  // Start immediately
            TimeSpan.FromSeconds(5));
    }

    private void UpdateServiceStatus(object? state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var isInitialized = StartupInitializer.IsInitialized;
                var isDatabaseAvailable = StartupInitializer.IsDatabaseAvailable;
                
                var serviceStatus = isInitialized ? "✅ Running" : "⚠️ Initializing";
                var dbStatus = isDatabaseAvailable ? "💚 Connected" : "🔶 Degraded";
                
                Debug.WriteLine($"Service Status: {serviceStatus} | Database: {dbStatus}");
                
                // Update UI elements if you have them
                // Example: StatusLabel.Text = $"{serviceStatus} | DB: {dbStatus}";
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Error checking service status: {exception}");
            }
        });
    }

    void OnCounterClicked(object? sender, EventArgs e)
    {
        // Check if services are ready before doing work
        if (!StartupInitializer.IsInitialized)
        {
            DisplayAlert("Services Not Ready", 
                "Background services are still initializing. Please wait a moment.", 
                "OK");
            return;
        }

        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
        
        // Show database status on every 5th click
        if (count % 5 == 0)
        {
            var dbStatus = StartupInitializer.IsDatabaseAvailable 
                ? "Database: Connected ✅" 
                : "Database: Degraded Mode 🔶\n(Auto-reconnecting...)";
            
            DisplayAlert("System Status", dbStatus, "OK");
        }
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _statusCheckTimer?.Dispose();
        _statusCheckTimer = null;
    }
}
