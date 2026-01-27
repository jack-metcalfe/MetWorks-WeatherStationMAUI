namespace MetWorks.Apps.MAUI.WeatherStationMaui.Views;
public partial class InitializationSplashPage : ContentPage
{
    public InitializationSplashPage()
    {
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
                    // Navigate within the existing Shell.
                    if (Shell.Current is null)
                    {
                        throw new InvalidOperationException("Shell.Current is null during post-initialization navigation.");
                    }

                    await Shell.Current.GoToAsync("/MainView").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    try { Debug.WriteLine($"Failed to navigate to device-specific startup page: {ex.Message}"); } catch { }

                    // Surface a retry UI rather than spawning additional windows.
                    Spinner.IsRunning = false;
                    RetryButton.IsVisible = true;
                    DetailsLabel.IsVisible = true;
                    DetailsLabel.Text = ex.Message;
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
