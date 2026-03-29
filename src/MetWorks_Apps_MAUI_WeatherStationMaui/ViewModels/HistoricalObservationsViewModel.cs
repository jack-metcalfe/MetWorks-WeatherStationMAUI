namespace MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

public sealed class HistoricalObservationsViewModel : INotifyPropertyChanged, IDisposable
{
    const int DefaultHoursBack = 0;
    const int DefaultHoursToShow = 12;
    const int DefaultMinHoursToShow = 6;
    const int DefaultMaxHoursToShow = 72;

    readonly MetWorks.Interfaces.ILogger _iLogger;
    readonly ISettingRepository _iSettingRepository;
    readonly IEventRelayBasic _iEventRelayBasic;
    readonly ISqliteDatabase _sqliteDatabase;
    readonly IInstanceIdentifier _instanceIdentifier;
    readonly CancellationToken _externalCancellation;

    readonly SemaphoreSlim _refreshGate = new(1, 1);

    int _hoursBack = DefaultHoursBack;
    int _hoursToShow = DefaultHoursToShow;

    int _hoursToShowMin = DefaultMinHoursToShow;
    int _hoursToShowMax = DefaultMaxHoursToShow;

    Amount? _minTemp;
    Amount? _maxTemp;
    Amount? _maxGust;

    string _statusLine = "Waiting for observations...";

    public HistoricalObservationsViewModel(
        MetWorks.Interfaces.ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        ISqliteDatabase sqliteDatabase,
        IInstanceIdentifier instanceIdentifier,
        CancellationToken externalCancellation
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(sqliteDatabase);
        ArgumentNullException.ThrowIfNull(instanceIdentifier);

        _iLogger = iLogger;
        _iSettingRepository = iSettingRepository;
        _iEventRelayBasic = iEventRelayBasic;
        _sqliteDatabase = sqliteDatabase;
        _instanceIdentifier = instanceIdentifier;
        _externalCancellation = externalCancellation;

        Hours = new ObservableCollection<HistoricalHourRow>();

        // Best-effort refresh when new readings arrive, but only when viewing the newest window.
        _iEventRelayBasic.Register<ObservationReading>(this, reading =>
        {
            _ = reading;

            if (HoursBack == 0)
                _ = RefreshAsync();
        });

        _ = RefreshAsync();
    }

    public ObservableCollection<HistoricalHourRow> Hours { get; }

    public int HoursBack
    {
        get => _hoursBack;
        set
        {
            var clamped = Math.Max(0, value);
            if (_hoursBack == clamped) return;
            _hoursBack = clamped;
            OnPropertyChanged();
            _ = RefreshAsync();
        }
    }

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
            if (_hoursToShow == clamped) return;
            _hoursToShow = clamped;
            OnPropertyChanged();
            _ = RefreshAsync();
        }
    }

    public Amount? MinTemp
    {
        get => _minTemp;
        private set
        {
            if (_minTemp == value) return;
            _minTemp = value;
            OnPropertyChanged();
        }
    }

    public Amount? MaxTemp
    {
        get => _maxTemp;
        private set
        {
            if (_maxTemp == value) return;
            _maxTemp = value;
            OnPropertyChanged();
        }
    }

    public Amount? MaxGust
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

    async Task RefreshAsync()
    {
        try
        {
            await _refreshGate.WaitAsync(_externalCancellation).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_externalCancellation.IsCancellationRequested)
        {
            return;
        }

        try
        {
            var installationId = _instanceIdentifier.GetOrCreateInstallationId();
            if (string.IsNullOrWhiteSpace(installationId))
                throw new InvalidOperationException("Installation id is empty.");

            var tempUnit = ReadPreferredUnit(SettingConstants.UnitOfMeasure_airTemperature, fallback: Unit.Parse("celsius"));
            var windUnit = ReadPreferredUnit(SettingConstants.UnitOfMeasure_windSpeed, fallback: Unit.Parse("m/s"));
            var rainUnit = ReadPreferredUnit(SettingConstants.UnitOfMeasure_rainAccumulation, fallback: Unit.Parse("mm"));

            var nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var hoursBack = HoursBack;
            var endExclusiveEpoch = AlignToNextHourEpoch(nowEpoch - (long)hoursBack * 3600);

            var hoursToShow = Clamp(HoursToShow, 1, DefaultMaxHoursToShow);
            var startEpoch = endExclusiveEpoch - (long)hoursToShow * 3600;

            const string sql = """
SELECT
    (o.device_received_utc_timestamp_epoch / 3600) * 3600 AS bucket_start_epoch,
    AVG(o.air_temperature_at_timestamp) AS air_temperature_avg,
    MAX(o.wind_speed_gust_in_wind_sample_interval) AS wind_gust_max,
    SUM(o.rain_accumulation_in_reporting_interval) AS rain_sum,
    AVG(o.uv_index_at_timestamp) AS uv_avg,
    AVG(o.relative_humidity_at_timestamp) AS relative_humidity_avg
FROM observation o
WHERE o.installation_id = $installation_id
  AND o.device_received_utc_timestamp_epoch >= $range_start_epoch
  AND o.device_received_utc_timestamp_epoch < $range_end_epoch
GROUP BY bucket_start_epoch
ORDER BY bucket_start_epoch DESC
LIMIT $limit;
""";

            await using var session = await _sqliteDatabase.OpenSessionAsync(_externalCancellation).ConfigureAwait(false);

            var rows = await session.QueryAsync(
                sql,
                [
                    new DbParam("$installation_id", installationId),
                    new DbParam("$range_start_epoch", startEpoch),
                    new DbParam("$range_end_epoch", endExclusiveEpoch),
                    new DbParam("$limit", hoursToShow),
                ],
                row =>
                {
                    _ = row.TryGetInt64("bucket_start_epoch", out var bucketEpoch);
                    _ = row.TryGetDouble("air_temperature_avg", out var tempC);
                    _ = row.TryGetDouble("wind_gust_max", out var gustMs);
                    _ = row.TryGetDouble("rain_sum", out var rainMm);
                    _ = row.TryGetDouble("uv_avg", out var uv);
                    _ = row.TryGetDouble("relative_humidity_avg", out var rh);

                    var timeLocal = DateTimeOffset.FromUnixTimeSeconds(bucketEpoch).ToLocalTime();
                    var hourDisplay = timeLocal.ToString("htt", CultureInfo.CurrentCulture).ToLowerInvariant();

                    static Amount? Convert(double? value, string fromUnitName, Unit preferredUnit)
                        => value is null ? null : new Amount(value.Value, fromUnitName).ConvertedTo(preferredUnit);

                    return new HistoricalHourRow(
                        TimeLocal: timeLocal,
                        HourDisplay: hourDisplay,
                        AirTemperature: Convert(tempC, "celsius", tempUnit),
                        WindGust: Convert(gustMs, "m/s", windUnit),
                        Rain: Convert(rainMm, "mm", rainUnit),
                        Uv: uv,
                        RelativeHumidity: rh
                    );
                },
                _externalCancellation
            ).ConfigureAwait(false);

            var temps = rows.Where(r => r.AirTemperature is not null).Select(r => r.AirTemperature!).ToArray();
            var gusts = rows.Where(r => r.WindGust is not null).Select(r => r.WindGust!).ToArray();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Hours.Clear();
                foreach (var r in rows)
                    Hours.Add(r);

                MinTemp = temps.Length > 0 ? temps.Min() : null;
                MaxTemp = temps.Length > 0 ? temps.Max() : null;
                MaxGust = gusts.Length > 0 ? gusts.Max() : null;

                HoursToShowMax = DefaultMaxHoursToShow;
                HoursToShowMin = Math.Max(1, Math.Min(DefaultMinHoursToShow, HoursToShowMax));

                StatusLine = rows.Count == 0
                    ? "No observations found in selected window."
                    : $"Showing {rows.Count}h ending ~{DateTimeOffset.FromUnixTimeSeconds(endExclusiveEpoch).ToLocalTime():g}";
            });
        }
        catch (OperationCanceledException) when (_externalCancellation.IsCancellationRequested)
        {
        }
        catch (UnknownUnitException ex)
        {
            _iLogger.Warning($"HistoricalObservationsViewModel: unit error. {ex.Message}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Hours.Clear();
                MinTemp = null;
                MaxTemp = null;
                MaxGust = null;
                StatusLine = $"Historical: {ex.Message}";
            });
        }
        catch (UnitConversionException ex)
        {
            _iLogger.Warning($"HistoricalObservationsViewModel: unit conversion error. {ex.Message}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Hours.Clear();
                MinTemp = null;
                MaxTemp = null;
                MaxGust = null;
                StatusLine = $"Historical: {ex.Message}";
            });
        }
        catch (InvalidOperationException ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Hours.Clear();
                MinTemp = null;
                MaxTemp = null;
                MaxGust = null;
                StatusLine = $"Historical: {ex.Message}";
            });
        }
        catch (DbException ex)
        {
            _iLogger.Warning($"HistoricalObservationsViewModel: SQLite error. {ex.Message}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Hours.Clear();
                MinTemp = null;
                MaxTemp = null;
                MaxGust = null;
                StatusLine = $"Historical: {ex.Message}";
            });
        }
        finally
        {
            try { _refreshGate.Release(); } catch { }
        }
    }

    Unit ReadPreferredUnit(string settingKey, Unit fallback)
    {
        try
        {
            var text = _iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(settingKey));

            if (string.IsNullOrWhiteSpace(text))
                return fallback;

            return Unit.Parse(text);
        }
        catch (InvalidOperationException ex)
        {
            _iLogger.Warning($"HistoricalObservationsViewModel: failed to read unit setting {settingKey}. {ex.Message}");
            return fallback;
        }
        catch (UnknownUnitException ex)
        {
            _iLogger.Warning($"HistoricalObservationsViewModel: unknown unit for setting {settingKey}. {ex.Message}");
            return fallback;
        }
    }

    static long AlignToNextHourEpoch(long epochSeconds)
    {
        var bucket = (epochSeconds / 3600) * 3600;
        return bucket + 3600;
    }

    static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public void Dispose()
    {
        try { _iEventRelayBasic.Unregister<ObservationReading>(this); } catch { }
        try { _refreshGate.Dispose(); } catch { }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public sealed record HistoricalHourRow(
        DateTimeOffset TimeLocal,
        string HourDisplay,
        Amount? AirTemperature,
        Amount? WindGust,
        Amount? Rain,
        double? Uv,
        double? RelativeHumidity
    );
}
