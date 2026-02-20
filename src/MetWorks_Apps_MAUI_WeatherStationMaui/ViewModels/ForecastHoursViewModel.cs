namespace MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

using System.Collections.ObjectModel;

public sealed class ForecastHoursViewModel : INotifyPropertyChanged, IDisposable
{
    const int DefaultHoursToShow = 12;
    const int DefaultMinHoursToShow = 8;
    const int DefaultMaxHoursToShow = 24;

    readonly MetWorks.Interfaces.ILogger _iLogger;
    readonly IEventRelayBasic _iEventRelayBasic;
    readonly ITempestForecastProvider _iTempestForecastProvider;

    readonly CancellationToken _externalCancellation;

    TempestForecast? _currentForecast;
    int _hoursToShow = DefaultHoursToShow;
    int _hoursToShowMin = DefaultMinHoursToShow;
    int _hoursToShowMax = DefaultMaxHoursToShow;

    double? _minTemp;
    double? _maxTemp;
    double? _maxGust;

    string _statusLine = "Waiting for forecast...";

    public ForecastHoursViewModel(
        MetWorks.Interfaces.ILogger iLogger,
        IEventRelayBasic iEventRelayBasic,
        ITempestForecastProvider iTempestForecastProvider,
        CancellationToken externalCancellation)
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iTempestForecastProvider);

        _iLogger = iLogger;
        _iEventRelayBasic = iEventRelayBasic;
        _iTempestForecastProvider = iTempestForecastProvider;
        _externalCancellation = externalCancellation;

        Hours = new ObservableCollection<ForecastHourRow>();

        _iEventRelayBasic.Register<TempestForecast>(this, OnForecastReceived);

        _ = RefreshFromProviderAsync();
    }

    public ObservableCollection<ForecastHourRow> Hours { get; }

    public int HoursToShowMin
    {
        get => _hoursToShowMin;
        private set
        {
            if (_hoursToShowMin == value) return;
            _hoursToShowMin = value;
            OnPropertyChanged();
        }
    }

    public int HoursToShowMax
    {
        get => _hoursToShowMax;
        private set
        {
            if (_hoursToShowMax == value) return;
            _hoursToShowMax = value;
            OnPropertyChanged();
        }
    }

    public int HoursToShow
    {
        get => _hoursToShow;
        set
        {
            var clamped = Clamp(value, HoursToShowMin, HoursToShowMax);
            if (_hoursToShow == clamped)
                return;

            _hoursToShow = clamped;
            OnPropertyChanged();
            RebuildRows();
        }
    }

    public double? MinTemp
    {
        get => _minTemp;
        private set
        {
            if (_minTemp == value) return;
            _minTemp = value;
            OnPropertyChanged();
        }
    }

    public double? MaxTemp
    {
        get => _maxTemp;
        private set
        {
            if (_maxTemp == value) return;
            _maxTemp = value;
            OnPropertyChanged();
        }
    }

    public double? MaxGust
    {
        get => _maxGust;
        private set
        {
            if (_maxGust == value) return;
            _maxGust = value;
            OnPropertyChanged();
        }
    }

    public string StatusLine
    {
        get => _statusLine;
        private set
        {
            if (string.Equals(_statusLine, value, StringComparison.Ordinal)) return;
            _statusLine = value;
            OnPropertyChanged();
        }
    }

    async Task RefreshFromProviderAsync()
    {
        try
        {
            var forecast = await _iTempestForecastProvider.GetForecastAsync(_externalCancellation);
            if (forecast is not null)
                UpdateForecast(forecast);
        }
        catch (OperationCanceledException) when (_externalCancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _iLogger.Warning($"ForecastHoursViewModel: failed to fetch initial forecast. {ex.Message}");
        }
    }

    void OnForecastReceived(TempestForecast forecast)
    {
        ArgumentNullException.ThrowIfNull(forecast);
        UpdateForecast(forecast);
    }

    void UpdateForecast(TempestForecast forecast)
    {
        _currentForecast = forecast;
        StatusLine = $"Updated: {forecast.RetrievedUtc.LocalDateTime:G}";
        RebuildRows();
    }

    void RebuildRows()
    {
        var forecast = _currentForecast;
        if (forecast is null || forecast.Hourly.Count == 0)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Hours.Clear();
                MinTemp = null;
                MaxTemp = null;
                MaxGust = null;
            });
            return;
        }

        var ordered = forecast.Hourly
            .Where(h => h.TimeLocal is not null)
            .OrderBy(h => h.TimeLocal)
            .ToArray();

        if (ordered.Length == 0)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Hours.Clear();
                MinTemp = null;
                MaxTemp = null;
                MaxGust = null;
            });
            return;
        }

        // Compare "now" using the same offset as the forecast hours to avoid skew when
        // the station/device timezone differs.
        var now = DateTimeOffset.Now.ToOffset(ordered[0].TimeLocal!.Value.Offset);

        // Start the display as close to "now" as possible by picking the nearest
        // forecast hour to the current local time, then showing subsequent rows.
        var startIndex = Array.FindIndex(ordered, h => h.TimeLocal >= now);
        if (startIndex < 0)
            startIndex = ordered.Length - 1;

        if (startIndex > 0)
        {
            var prev = ordered[startIndex - 1].TimeLocal!.Value;
            var curr = ordered[startIndex].TimeLocal!.Value;

            var prevDelta = now - prev;
            var currDelta = curr - now;

            if (prevDelta <= currDelta)
                startIndex--;
        }

        var available = Math.Max(1, ordered.Length - startIndex);
        HoursToShowMax = available;
        HoursToShowMin = Math.Max(1, Math.Min(DefaultMinHoursToShow, available));

        // If the available window changes (new forecast pull), keep HoursToShow in range.
        var effectiveHoursToShow = Clamp(_hoursToShow, HoursToShowMin, HoursToShowMax);
        if (effectiveHoursToShow != _hoursToShow)
        {
            _hoursToShow = effectiveHoursToShow;
            OnPropertyChanged(nameof(HoursToShow));
        }

        var candidates = ordered
            .Skip(startIndex)
            .Take(_hoursToShow)
            .ToArray();

        var rows = candidates.Select(h => ForecastHourRow.From(h)).ToArray();

        var temps = candidates
            .Select(h => h.AirTemperature)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToArray();

        var gusts = candidates
            .Select(h => h.WindGust)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToArray();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Hours.Clear();
            foreach (var r in rows)
                Hours.Add(r);

            MinTemp = temps.Length > 0 ? temps.Min() : null;
            MaxTemp = temps.Length > 0 ? temps.Max() : null;
            MaxGust = gusts.Length > 0 ? gusts.Max() : null;
        });
    }

    static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public void Dispose()
    {
        try
        {
            _iEventRelayBasic.Unregister<TempestForecast>(this);
        }
        catch (Exception ex)
        {
            _iLogger.Warning("ForecastHoursViewModel: failed to unregister TempestForecast handler.", ex);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public sealed record ForecastHourRow(
        DateTimeOffset TimeLocal,
        string HourDisplay,
        double? AirTemperature,
        double? WindGust,
        int? PrecipProbability,
        double? Uv,
        int? RelativeHumidity)
    {
        public static ForecastHourRow From(TempestForecastHour hour)
        {
            var time = hour.TimeLocal ?? DateTimeOffset.MinValue;
            var hourDisplay = time != DateTimeOffset.MinValue
                ? time.ToString("htt", CultureInfo.CurrentCulture).ToLowerInvariant()
                : "--";

            return new ForecastHourRow(
                TimeLocal: time,
                HourDisplay: hourDisplay,
                AirTemperature: hour.AirTemperature,
                WindGust: hour.WindGust,
                PrecipProbability: hour.PrecipProbability,
                Uv: hour.Uv,
                RelativeHumidity: hour.RelativeHumidity
            );
        }
    }
}
