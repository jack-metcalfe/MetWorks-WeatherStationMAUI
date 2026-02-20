using Microsoft.Maui.Dispatching;

namespace MetWorks.Apps.MAUI.WeatherStationMaui.Pages.GuestPages;

public partial class ForecastHoursAdaptive : ContentView
{
    readonly IDispatcherTimer _autoScrollTimer;
    bool _autoScrollEnabled;
    int _autoScrollIndex;

    public ForecastHoursAdaptive(ForecastHoursViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        BindingContext = viewModel;

        _autoScrollTimer = Dispatcher.CreateTimer();
        _autoScrollTimer.Interval = TimeSpan.FromSeconds(6);
        _autoScrollTimer.Tick += (_, _) => AutoScrollTick();

        AutoScrollButton.Clicked += (_, _) => ToggleAutoScroll();

        Unloaded += (_, _) =>
        {
            try
            {
                _autoScrollTimer.Stop();
            }
            catch (Exception ex)
            {
                try { Debug.WriteLine($"ForecastHoursAdaptive: failed to stop auto-scroll timer. {ex.Message}"); } catch { }
            }
        };
    }

    void ToggleAutoScroll()
    {
        _autoScrollEnabled = !_autoScrollEnabled;

        AutoScrollButton.Text = _autoScrollEnabled
            ? "Auto scroll: On"
            : "Auto scroll: Off";

        _autoScrollIndex = 0;

        if (_autoScrollEnabled)
            _autoScrollTimer.Start();
        else
            _autoScrollTimer.Stop();
    }

    void AutoScrollTick()
    {
        try
        {
            if (!_autoScrollEnabled)
                return;

            if (BindingContext is not ForecastHoursViewModel vm)
                return;

            var count = vm.Hours.Count;
            if (count == 0)
                return;

            _autoScrollIndex = (_autoScrollIndex + 1) % count;

            HoursList.ScrollTo(
                _autoScrollIndex,
                position: ScrollToPosition.Start,
                animate: true
            );
        }
        catch (Exception ex)
        {
            try { Debug.WriteLine($"ForecastHoursAdaptive: auto-scroll failed. {ex.Message}"); } catch { }
        }
    }
}
