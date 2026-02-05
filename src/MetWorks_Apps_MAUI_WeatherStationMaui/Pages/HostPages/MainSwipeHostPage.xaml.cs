using MetWorks.Apps.Maui.WeatherStationMaui.DeviceSelection;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.HostPages;
public partial class MainSwipeHostPage : ContentPage
{
    bool _contentSet;

    readonly IServiceProvider _services;
    readonly IContentViewFactory _contentViewFactory;
    readonly IHostCompositionCatalog _hostCompositionCatalog;

    readonly List<View> _slots = new();
    int _index;


#if WINDOWS
    DateTime _lastKeyNavigationUtc = DateTime.MinValue;
    static readonly TimeSpan KeyNavigationDebounce = TimeSpan.FromMilliseconds(150);
#endif

    public MainSwipeHostPage(
        IServiceProvider services, 
        IContentViewFactory contentViewFactory,
        IHostCompositionCatalog hostCompositionCatalog
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(contentViewFactory);
        ArgumentNullException.ThrowIfNull(hostCompositionCatalog);
        _services = services;
        _contentViewFactory = contentViewFactory;
        _hostCompositionCatalog = hostCompositionCatalog;
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

        _slots.Clear();
        _slots.Add(view);

        _index = 0;

        Host.Content = _slots[0];
        _contentSet = true;

#if WINDOWS
        btnLeft.IsVisible = true;
        btnRight.IsVisible = true;
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_contentSet) return;

        try
        {
            var ctx = DeviceContext.Current();

            if (!_hostCompositionCatalog.TryGetComposition(HostKey.MainSwipe, out var composition))
                throw new InvalidOperationException($"No host composition registered for {HostKey.MainSwipe}.");

            if (composition.Slots.Count < 1)
                throw new InvalidOperationException($"Host composition {HostKey.MainSwipe} must specify at least one slot.");

            _slots.Clear();
            foreach (var slot in composition.Slots)
            {
                var view = _contentViewFactory.Create(slot, ctx);
                _slots.Add(view);
            }

            if (_slots.Count == 0)
                throw new InvalidOperationException($"Host composition {HostKey.MainSwipe} produced no views.");

            _index = 0;

            Host.Content = _slots[0];
            _contentSet = true;

#if WINDOWS
        btnLeft.IsVisible = true;
        btnRight.IsVisible = true;
#endif
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
        var count = _slots.Count;
        if (count == 0)
            return;

        _index = (index % count + count) % count;

        var next = _slots[_index];

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
