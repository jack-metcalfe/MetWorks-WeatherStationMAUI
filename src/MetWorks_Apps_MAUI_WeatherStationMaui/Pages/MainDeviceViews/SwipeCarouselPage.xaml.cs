using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.MainDeviceViews;

public partial class SwipeCarouselPage : ContentPage
{
    private readonly WeatherViewModel _viewModel;
    private readonly Interfaces.ILogger? _iResilientLogger;

    public SwipeCarouselPage(WeatherViewModel viewModel, ILoggerResilient? iResilientLogger = null)
    {
        if (iResilientLogger is not null)
        {
            if (!iResilientLogger.IsReady)
            {
                iResilientLogger.Ready.ConfigureAwait(false).GetAwaiter().GetResult();
                _iResilientLogger = iResilientLogger?.ForContext(typeof(SwipeCarouselPage));
                _iResilientLogger.Information("TestLog");
            }
        }
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Show left/right buttons so users can navigate on desktop and touch devices
        btnLeft.IsVisible = true;
        btnRight.IsVisible = true;

#if WINDOWS
        // Page-level key handling so left/right works without focusing a specific control.
        try
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        catch
        {
            // Ignore; key handling is best-effort.
        }
#endif

        // Warm-up logger asynchronously if provided
        _ = Task.Run(async () =>
        {
            try
            {
                if (iResilientLogger is not null)
                {
                    await iResilientLogger.Ready.ConfigureAwait(false);
                }
            }
            catch { }
        });

        // Prepare ItemsSource-backed pages (MVVM friendly) using Views directly
        var pages = new ObservableCollection<View>();

        try
        {
            // Create the device-specific view (ContentView preferred)
            var deviceView = DeviceViewSelector.GetViewForCurrentDevice();
            if (deviceView != null)
            {
                deviceView.BindingContext = _viewModel;
                pages.Add(deviceView);
            }
            else
            {
                var fallback = new MainViewContent { BindingContext = _viewModel };
                pages.Add(fallback);
            }
        }
        catch (Exception ex)
        {
            this._iResilientLogger?.Error("Error creating device-specific page", ex);
            Debug.WriteLine($"Error creating device-specific page: {ex}");
        }

        // Add the details page as the second carousel item
        var details = new DetailsContent { BindingContext = _viewModel };
        pages.Add(details);

        // Use the view collection as the ItemsSource; ItemTemplate presents the View instances
        carousel.ItemsSource = pages;
        carousel.Position = 0;
    }

#if WINDOWS
    private void OnLoaded(object? sender, EventArgs e)
    {
        try
        {
            if (Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement fe)
            {
                fe.KeyDown += OnPlatformKeyDown;
                fe.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            }
        }
        catch { }
    }

    private void OnUnloaded(object? sender, EventArgs e)
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

    private void OnPlatformKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        try
        {
            if (e.Key == Windows.System.VirtualKey.Left)
            {
                e.Handled = true;
                OnLeftClicked(sender, EventArgs.Empty);
            }
            else if (e.Key == Windows.System.VirtualKey.Right)
            {
                e.Handled = true;
                OnRightClicked(sender, EventArgs.Empty);
            }
        }
        catch { }
    }
#endif

    private void OnLeftClicked(object? sender, EventArgs e)
    {
        if (carousel.Position > 0)
            carousel.Position -= 1;
    }


    private void OnRightClicked(object? sender, EventArgs e)
    {
        int count = 0;
        if (carousel.ItemsSource is IList items)
        {
            count = items.Count;
        }
        else if (carousel.ItemsSource is ICollection col)
        {
            count = col.Count;
        }
        else if (carousel.ItemsSource != null)
        {
            // fallback: enumerate
            count = carousel.ItemsSource.Cast<object>().Count();
        }
        else
        {
            // fallback for static XAML items: we expect two pages defined in XAML
            count = 2;
        }

        if (carousel.Position < count - 1)
            carousel.Position += 1;
    }


    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel?.Dispose();
    }
}
