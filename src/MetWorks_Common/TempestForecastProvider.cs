namespace MetWorks.Common;

using System.Text.Json;
using MetWorks.Constants;
using MetWorks.Interfaces;

public sealed class TempestForecastProvider : ServiceBase, ITempestForecastProvider
{
    const string ForecastSnapshotFileName = "tempest.forecast.snapshot.json";

    const int DefaultRefreshIntervalMinutes = 60;
    const int MinRefreshIntervalMinutes = 5;
    const int MaxRefreshIntervalMinutes = 24 * 60;

    readonly SemaphoreSlim _lock = new(1, 1);

    DateTimeOffset _lastRefreshUtc = DateTimeOffset.MinValue;
    TempestForecast? _forecast;

    IPlatformPaths? _platformPaths;
    IPlatformPaths PlatformPaths => _platformPaths ?? new DefaultPlatformPaths();

    ITempestRestClient? _tempestRestClient;
    ITempestRestClient TempestRestClient => NullPropertyGuard.Get(_isInitialized, _tempestRestClient, nameof(TempestRestClient));

    TimeSpan _refreshInterval = TimeSpan.FromMinutes(DefaultRefreshIntervalMinutes);

    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        ITempestRestClient iTempestRestClient,
        CancellationToken externalCancellation = default,
        IPlatformPaths? iPlatformPaths = null,
        ProvenanceTracker? provenanceTracker = null)
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iTempestRestClient);

        InitializeBase(
            iLogger.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker);

        _tempestRestClient = iTempestRestClient;
        _platformPaths = iPlatformPaths;

        _refreshInterval = TimeSpan.FromMinutes(ReadRefreshIntervalMinutes(iSettingRepository));

        StartBackground(ct => RefreshLoopAsync(ct));

        MarkReady();
        return Task.FromResult(true);
    }

    public async ValueTask<TempestForecast?> GetForecastAsync(CancellationToken cancellationToken = default)
    {
        await Ready.ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        if (_forecast is not null && now - _lastRefreshUtc < _refreshInterval)
            return _forecast;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            now = DateTimeOffset.UtcNow;
            if (_forecast is not null && now - _lastRefreshUtc < _refreshInterval)
                return _forecast;

            await RefreshOnceAsync(now, cancellationToken).ConfigureAwait(false);
            return _forecast;
        }
        finally
        {
            _lock.Release();
        }
    }

    async Task RefreshLoopAsync(CancellationToken token)
    {
        // Refresh regularly regardless of consumer pull patterns so UI can subscribe to events.
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_refreshInterval, token).ConfigureAwait(false);

                var now = DateTimeOffset.UtcNow;
                await _lock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    await RefreshOnceAsync(now, token).ConfigureAwait(false);
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex)
            {
                ILogger.Warning("TempestForecastProvider: HTTP failure", ex);
            }
            catch (InvalidOperationException ex)
            {
                ILogger.Warning("TempestForecastProvider: failure", ex);
            }
        }
    }

    async Task RefreshOnceAsync(DateTimeOffset now, CancellationToken token)
    {
        TempestBetterForecastSnapshot? snapshot = null;

        try
        {
            snapshot = await TempestRestClient.GetBetterForecastAsync(token).ConfigureAwait(false);
            TryPersistSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            // Offline mode is expected; fall back to cached snapshot.
            ILogger.Warning($"TempestForecastProvider: failed to fetch Better Forecast; will try cached snapshot. {ex.Message}");
        }

        snapshot ??= TryLoadPersistedSnapshot();

        if (snapshot is null)
        {
            _lastRefreshUtc = now;
            return;
        }

        var previous = _forecast;
        _forecast = TryExtractForecast(snapshot.StationId, snapshot.RawJson, retrievedUtc: now);
        _lastRefreshUtc = now;

        try
        {
            if (_forecast is not null && !Equals(_forecast, previous))
                IEventRelayBasic.Send(_forecast);
        }
        catch
        {
        }
    }

    bool TryPersistSnapshot(TempestBetterForecastSnapshot snapshot)
    {
        try
        {
            var dir = PlatformPaths.AppDataDirectory;
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, ForecastSnapshotFileName);
            File.WriteAllText(path, snapshot.RawJson);
            return true;
        }
        catch (Exception ex)
        {
            ILogger.Warning($"TempestForecastProvider: failed to persist forecast snapshot. {ex.Message}");
            return false;
        }
    }

    TempestBetterForecastSnapshot? TryLoadPersistedSnapshot()
    {
        try
        {
            var path = Path.Combine(PlatformPaths.AppDataDirectory, ForecastSnapshotFileName);
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return new TempestBetterForecastSnapshot(
                StationId: 0,
                RetrievedUtc: DateTimeOffset.MinValue,
                RawJson: json);
        }
        catch (Exception ex)
        {
            ILogger.Warning($"TempestForecastProvider: failed to load cached forecast snapshot. {ex.Message}");
            return null;
        }
    }

    static TempestForecast? TryExtractForecast(long stationId, string rawJson, DateTimeOffset retrievedUtc)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return null;

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return null;

            var latitude = TryGetDouble(root, "latitude");
            var longitude = TryGetDouble(root, "longitude");
            var timeZone = TryGetString(root, "timezone");
            var tzOffsetMinutes = TryGetInt(root, "timezone_offset_minutes");

            var daily = new List<TempestForecastDay>();
            var hourly = new List<TempestForecastHour>();

            if (root.TryGetProperty("forecast", out var forecastEl) && forecastEl.ValueKind == JsonValueKind.Object)
            {
                if (forecastEl.TryGetProperty("daily", out var dailyEl) && dailyEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var d in dailyEl.EnumerateArray())
                    {
                        if (d.ValueKind != JsonValueKind.Object)
                            continue;

                        daily.Add(new TempestForecastDay(
                            DayStartLocal: TryGetEpochSecondsAsLocalOffset(d, "day_start_local", tzOffsetMinutes),
                            DayNum: TryGetInt(d, "day_num"),
                            MonthNum: TryGetInt(d, "month_num"),
                            Conditions: TryGetString(d, "conditions"),
                            Icon: TryGetString(d, "icon"),
                            SunriseLocal: TryGetEpochSecondsAsLocalOffset(d, "sunrise", tzOffsetMinutes),
                            SunsetLocal: TryGetEpochSecondsAsLocalOffset(d, "sunset", tzOffsetMinutes),
                            AirTempHigh: TryGetDouble(d, "air_temp_high"),
                            AirTempLow: TryGetDouble(d, "air_temp_low"),
                            PrecipProbability: TryGetInt(d, "precip_probability"),
                            PrecipIcon: TryGetString(d, "precip_icon"),
                            PrecipType: TryGetString(d, "precip_type")
                        ));
                    }
                }

                if (forecastEl.TryGetProperty("hourly", out var hourlyEl) && hourlyEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var h in hourlyEl.EnumerateArray())
                    {
                        if (h.ValueKind != JsonValueKind.Object)
                            continue;

                        hourly.Add(new TempestForecastHour(
                            TimeLocal: TryGetEpochSecondsAsLocalOffset(h, "time", tzOffsetMinutes),
                            LocalHour: TryGetInt(h, "local_hour"),
                            LocalDay: TryGetInt(h, "local_day"),
                            Conditions: TryGetString(h, "conditions"),
                            Icon: TryGetString(h, "icon"),
                            AirTemperature: TryGetDouble(h, "air_temperature"),
                            FeelsLike: TryGetDouble(h, "feels_like"),
                            SeaLevelPressure: TryGetDouble(h, "sea_level_pressure"),
                            RelativeHumidity: TryGetInt(h, "relative_humidity"),
                            Precip: TryGetInt(h, "precip"),
                            PrecipProbability: TryGetInt(h, "precip_probability"),
                            WindAvg: TryGetDouble(h, "wind_avg"),
                            WindGust: TryGetDouble(h, "wind_gust"),
                            WindDirection: TryGetDouble(h, "wind_direction"),
                            WindDirectionCardinal: TryGetString(h, "wind_direction_cardinal"),
                            Uv: TryGetDouble(h, "uv")
                        ));
                    }
                }
            }

            return new TempestForecast(
                StationId: stationId,
                RetrievedUtc: retrievedUtc,
                Latitude: latitude,
                Longitude: longitude,
                TimeZone: timeZone,
                TimeZoneOffsetMinutes: tzOffsetMinutes,
                Daily: daily,
                Hourly: hourly);
        }
        catch
        {
            return null;
        }

        static double? TryGetDouble(JsonElement node, string propertyName)
        {
            if (!node.TryGetProperty(propertyName, out var p)) return null;
            if (p.ValueKind != JsonValueKind.Number) return null;
            return p.TryGetDouble(out var d) ? d : null;
        }

        static int? TryGetInt(JsonElement node, string propertyName)
        {
            if (!node.TryGetProperty(propertyName, out var p)) return null;
            if (p.ValueKind != JsonValueKind.Number) return null;
            return p.TryGetInt32(out var i) ? i : null;
        }

        static string? TryGetString(JsonElement node, string propertyName)
        {
            if (!node.TryGetProperty(propertyName, out var p)) return null;
            return p.ValueKind == JsonValueKind.String ? p.GetString() : null;
        }

        static DateTimeOffset? TryGetEpochSecondsAsLocalOffset(JsonElement node, string propertyName, int? tzOffsetMinutes)
        {
            if (!node.TryGetProperty(propertyName, out var p)) return null;
            if (p.ValueKind != JsonValueKind.Number) return null;
            if (!p.TryGetInt64(out var seconds)) return null;
            if (seconds <= 0) return null;

            try
            {
                var utc = DateTimeOffset.FromUnixTimeSeconds(seconds);

                // Convert to the station's local offset if available; otherwise fall back to device local time.
                // DateTimeOffset.ToString formats using its embedded offset, so this directly affects UI display.
                return tzOffsetMinutes is not null
                    ? utc.ToOffset(TimeSpan.FromMinutes(tzOffsetMinutes.Value))
                    : utc.ToLocalTime();
            }
            catch { return null; }
        }
    }

    static int ReadRefreshIntervalMinutes(ISettingRepository settingRepository)
    {
        var minutes = settingRepository.GetValueOrDefault<int>(
            LookupDictionaries.TempestForecastGroupSettingsDefinition.BuildPath(SettingConstants.TempestForecast_refreshIntervalMinutes));

        if (minutes <= 0)
            minutes = DefaultRefreshIntervalMinutes;

        if (minutes < MinRefreshIntervalMinutes)
            minutes = MinRefreshIntervalMinutes;
        else if (minutes > MaxRefreshIntervalMinutes)
            minutes = MaxRefreshIntervalMinutes;

        return minutes;
    }
}
