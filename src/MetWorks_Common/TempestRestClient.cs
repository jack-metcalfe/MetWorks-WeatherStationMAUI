namespace MetWorks.Common;
public sealed class TempestRestClient : ServiceBase, ITempestRestClient
{
    const string BaseUrl = "https://swd.weatherflow.com/swd/rest/";

    HttpClient? _httpClient;
    HttpClient HttpClient => NullPropertyGuard.Get(_isInitialized, _httpClient, nameof(HttpClient));

    public TempestRestClient() { }

    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        CancellationToken externalCancellation
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);

        InitializeBase(
            iLogger.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation
        );

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(10)
        };

        MarkReady();
        return Task.FromResult(true);
    }

    public async Task<TempestStationSnapshot> GetStationSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await Ready.ConfigureAwait(false);

        var apiKeySettingPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_apiKey);
        var stationIdSettingPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_stationId);

        var apiKey = ISettingRepository.GetValueOrDefault<string>(apiKeySettingPath);
        var stationIdString = ISettingRepository.GetValueOrDefault<string>(stationIdSettingPath);

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "00000000-0000-0000-0000-000000000000")
            throw new InvalidOperationException($"Tempest API key is not configured (setting: '{apiKeySettingPath}').");

        if (!long.TryParse(stationIdString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stationId) || stationId <= 0)
            throw new InvalidOperationException($"Tempest station id is not configured (setting: '{stationIdSettingPath}').");

        // Prefer the station details endpoint so we can capture *all* station metadata (elevation, devices, etc.).
        // Swagger: GET /stations/{station_id}
        var url = $"stations/{stationId.ToString(CultureInfo.InvariantCulture)}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var res = await HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        long parsedStationId = stationId;

        // Response typically is: { "stations": [ { "station_id": ... } ], "status": {...} }
        try
        {
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("stations", out var stations)
                && stations.ValueKind == JsonValueKind.Array
                && stations.GetArrayLength() > 0
            )
            {
                var first = stations[0];
                if (first.ValueKind == JsonValueKind.Object
                    && first.TryGetProperty("station_id", out var sid)
                    && sid.TryGetInt64(out var sidVal))
                {
                    parsedStationId = sidVal;
                }
            }
        }
        catch { }

        var rawJson = root.GetRawText();
        return new TempestStationSnapshot(
            StationId: parsedStationId,
            RetrievedUtc: DateTimeOffset.UtcNow,
            RawJson: rawJson
        );
    }

    public async Task<TempestBetterForecastSnapshot> GetBetterForecastAsync(CancellationToken cancellationToken = default)
    {
        await Ready.ConfigureAwait(false);

        var apiKeySettingPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_apiKey);
        var stationIdSettingPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_stationId);

        var apiKey = ISettingRepository.GetValueOrDefault<string>(apiKeySettingPath);
        var stationIdString = ISettingRepository.GetValueOrDefault<string>(stationIdSettingPath);

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "00000000-0000-0000-0000-000000000000")
            throw new InvalidOperationException($"Tempest API key is not configured (setting: '{apiKeySettingPath}').");

        if (!long.TryParse(stationIdString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stationId) || stationId <= 0)
            throw new InvalidOperationException($"Tempest station id is not configured (setting: '{stationIdSettingPath}').");

        // Swagger: GET /better_forecast
        // Auth: apiKey in query param 'token'
        var query = new List<string>
        {
            $"station_id={stationId.ToString(CultureInfo.InvariantCulture)}",
            $"token={Uri.EscapeDataString(apiKey)}"
        };

        AppendUnitQueryParams(query);
        var url = "better_forecast?" + string.Join("&", query);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);

        using var res = await HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var rawJson = doc.RootElement.GetRawText();
        return new TempestBetterForecastSnapshot(
            StationId: stationId,
            RetrievedUtc: DateTimeOffset.UtcNow,
            RawJson: rawJson
        );

        void AppendUnitQueryParams(List<string> queryParts)
        {
            // Best-effort mapping from our existing unit settings to Tempest query enums.
            // If a mapping is unknown, omit the query param so Tempest defaults apply.

            TryAdd("units_temp", MapUnitsTemp);
            TryAdd("units_wind", MapUnitsWind);
            TryAdd("units_pressure", MapUnitsPressure);
            TryAdd("units_precip", MapUnitsPrecip);
            TryAdd("units_distance", MapUnitsDistance);

            void TryAdd(string key, Func<string?, string?> map)
            {
                var raw = ISettingRepository.GetValueOrDefault<string>(
                    LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(key switch
                    {
                        "units_temp" => SettingConstants.UnitOfMeasure_airTemperature,
                        "units_wind" => SettingConstants.UnitOfMeasure_windSpeed,
                        "units_pressure" => SettingConstants.UnitOfMeasure_airPressure,
                        "units_precip" => SettingConstants.UnitOfMeasure_rainAccumulation,
                        "units_distance" => SettingConstants.UnitOfMeasure_lightningDistance,
                        _ => throw new InvalidOperationException("Unexpected unit key.")
                    }));

                var mapped = map(raw);
                if (!string.IsNullOrWhiteSpace(mapped))
                    queryParts.Add($"{key}={Uri.EscapeDataString(mapped)}");
            }

            static string? MapUnitsTemp(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                return raw.Trim().ToLowerInvariant() switch
                {
                    "degree fahrenheit" => "f",
                    "degree celsius" => "c",
                    _ => null
                };
            }

            static string? MapUnitsWind(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                return raw.Trim().ToLowerInvariant() switch
                {
                    "mile/hour" => "mph",
                    "kilometer/hour" => "kph",
                    "knot" => "kts",
                    "meter/second" => "mps",
                    _ => null
                };
            }

            static string? MapUnitsPressure(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                return raw.Trim().ToLowerInvariant() switch
                {
                    "inch of mercury" => "inhg",
                    "hectopascal" => "hpa",
                    "millibar" => "mb",
                    _ => null
                };
            }

            static string? MapUnitsPrecip(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                return raw.Trim().ToLowerInvariant() switch
                {
                    "inch" => "in",
                    "centimeter" => "cm",
                    "millimeter" => "mm",
                    _ => null
                };
            }

            static string? MapUnitsDistance(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                return raw.Trim().ToLowerInvariant() switch
                {
                    "mile" => "mi",
                    "kilometer" => "km",
                    _ => null
                };
            }
        }
    }
}
