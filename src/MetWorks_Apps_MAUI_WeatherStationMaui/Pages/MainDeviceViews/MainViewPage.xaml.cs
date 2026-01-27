namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

using System.Collections.ObjectModel;

public partial class MainViewPage : ContentPage
{
    bool _contentSet;

    View? _deviceMainView;
    View? _secondView;
    int _index;


#if WINDOWS
    DateTime _lastKeyNavigationUtc = DateTime.MinValue;
    static readonly TimeSpan KeyNavigationDebounce = TimeSpan.FromMilliseconds(150);
#endif

    public MainViewPage()
    {
        InitializeComponent();

#if WINDOWS
        try
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        catch { }
#endif
    }

    public void SetContent(View view)
    {
        ArgumentNullException.ThrowIfNull(view);

        _deviceMainView = view;
        _secondView ??= new SecondWindowContent();
        _index = 0;

        Host.Content = _deviceMainView;
        _contentSet = true;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_contentSet) return;

        try
        {
            var view = DeviceSelection.DeviceViewSelector.GetViewForCurrentDevice();
            SetContent(view);

            btnLeft.IsVisible = true;
            btnRight.IsVisible = true;
        }
        catch (Exception ex)
        {
            try { Debug.WriteLine($"Failed to set device view content: {ex}"); } catch { }
            try
            {
                SetContent(new ContentView
                {
                    Content = new Label
                    {
                        Text = "Failed to load device view.",
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                });
            }
            catch { }
        }
    }

    void ShowIndex(int index)
    {
        var count = 2;
        if (count == 0) return;

        _index = (index % count + count) % count;

        var next = _index switch
        {
            0 => _deviceMainView,
            1 => _secondView,
            _ => _deviceMainView
        };

        if (next is null)
            return;

        if (!ReferenceEquals(Host.Content, next))
            Host.Content = next;
    }

    void OnLeftClicked(object? sender, EventArgs e)
    {
        ShowIndex(_index - 1);
    }

    void OnRightClicked(object? sender, EventArgs e)
    {
        ShowIndex(_index + 1);
    }

    void OnSwipedLeft(object? sender, SwipedEventArgs e) => ShowIndex(_index + 1);

    void OnSwipedRight(object? sender, SwipedEventArgs e) => ShowIndex(_index - 1);

#if WINDOWS
    void OnLoaded(object? sender, EventArgs e)
    {
        try
        {
            if (Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement fe)
            {
                fe.KeyDown -= OnPlatformKeyDown;
                fe.KeyDown += OnPlatformKeyDown;
            }
        }
        catch { }
    }

    void OnUnloaded(object? sender, EventArgs e)
    {
        try
        {
            if (Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement fe)
            {
                fe.KeyDown -= OnPlatformKeyDown;
            }
        }
        catch { }
    }

    void OnPlatformKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        try
        {
            var now = DateTime.UtcNow;
            if (now - _lastKeyNavigationUtc < KeyNavigationDebounce)
                return;

            if (e.Key == Windows.System.VirtualKey.Left)
            {
                e.Handled = true;
                _lastKeyNavigationUtc = now;
                ShowIndex(_index - 1);
            }
            else if (e.Key == Windows.System.VirtualKey.Right)
            {
                e.Handled = true;
                _lastKeyNavigationUtc = now;
                ShowIndex(_index + 1);
            }
        }
        catch { }
    }
#endif
}
