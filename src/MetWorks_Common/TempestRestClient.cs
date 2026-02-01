namespace MetWorks.Common;
public sealed class TempestRestClient : ServiceBase, ITempestRestClient
{
    const string BaseUrl = "https://swd.weatherflow.com/swd/rest/";

    HttpClient? _httpClient;
    HttpClient HttpClient => NullPropertyGuard.Get(_isInitialized, _httpClient, nameof(HttpClient));

    public TempestRestClient() { }

    public Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        CancellationToken externalCancellation = default,
        HttpClient? httpClient = null
    )
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);

        InitializeBase(
            iLoggerResilient.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation
        );

        _httpClient = httpClient ?? new HttpClient
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

        var apiKeySettingPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildSettingPath(SettingConstants.Tempest_apiKey);
        var stationIdSettingPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildSettingPath(SettingConstants.Tempest_stationId);

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
}
