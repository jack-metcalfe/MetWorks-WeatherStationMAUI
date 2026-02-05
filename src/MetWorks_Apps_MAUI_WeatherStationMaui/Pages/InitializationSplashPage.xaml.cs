namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages;
public partial class InitializationSplashPage : ContentPage
{
    bool _subscribed;
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

        SubscribeToStartupEvents();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // If initialization already completed before we subscribed, don't wait for events.
        if (StartupInitializer.IsInitialized)
        {
            _ = NavigateToMainAsync();
            return;
        }

        // Kick initialization if it hasn't started (or if app was launched without it).
        _ = Task.Run(async () =>
        {
            try
            {
                await StartupInitializer.InitializeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ShowFailure(ex);
            }
        });
    }

    void SubscribeToStartupEvents()
    {
        if (_subscribed)
            return;
        _subscribed = true;

        StartupInitializer.StatusChanged += status =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = status;
            });
        };

        StartupInitializer.Initialized += () =>
        {
            _ = NavigateToMainAsync();
        };

        StartupInitializer.InitializationFailed += ex =>
        {
            ShowFailure(ex);
        };
    }

    async Task NavigateToMainAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            Spinner.IsRunning = false;
            RetryButton.IsVisible = false;

            try
            {
                if (Shell.Current is null)
                    throw new InvalidOperationException("Shell.Current is null during post-initialization navigation.");

                await Shell.Current.GoToAsync("///Weather/MainSwipeHostPage");
            }
            catch (Exception ex)
            {
                try { Debug.WriteLine($"Failed to navigate to startup page: {ex.Message}"); } catch { }
                Spinner.IsRunning = false;
                RetryButton.IsVisible = true;
                DetailsLabel.IsVisible = true;
                DetailsLabel.Text = ex.Message;
            }
        });
    }
    void ShowFailure(Exception ex)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Spinner.IsRunning = false;
            RetryButton.IsVisible = true;
            DetailsLabel.IsVisible = true;
            DetailsLabel.Text = ex.Message;
        });
    }
}
