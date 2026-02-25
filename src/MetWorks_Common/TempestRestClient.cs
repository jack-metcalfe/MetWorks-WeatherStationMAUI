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

    public async Task<TempestStationObservationsSnapshot> GetStationObservationsAsync(CancellationToken cancellationToken = default
    )
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

        // Swagger (expected): GET /observations/station/{station_id}
        // Auth (expected): apiKey in query param 'token' (consistent with /better_forecast).
        var query = new List<string>
        {
            $"token={Uri.EscapeDataString(apiKey)}"
        };

        // Persisted observations snapshots should be stable and independent of user preferences.
        // Always request metric units from Tempest and perform user-preference conversions later.
        AppendMetricUnitQueryParams(query);

        var url = $"observations/station/{stationId.ToString(CultureInfo.InvariantCulture)}?{string.Join("&", query)}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);

        using var res = await HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var rawJson = doc.RootElement.GetRawText();
        return new TempestStationObservationsSnapshot(
            StationId: stationId,
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

        AppendMetricUnitQueryParams(query);
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
    }

    static void AppendMetricUnitQueryParams(List<string> queryParts)
    {
        ArgumentNullException.ThrowIfNull(queryParts);

        // Persisted Tempest snapshots should be stable and preference-independent.
        // Always request metric units from the API; do conversions later when constructing `Amount`.
        queryParts.Add("units_temp=c");
        queryParts.Add("units_wind=mps");
        queryParts.Add("units_pressure=mb");
        queryParts.Add("units_precip=mm");
        queryParts.Add("units_distance=km");
    }
}
