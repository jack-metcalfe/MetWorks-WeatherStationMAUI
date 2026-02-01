namespace MetWorks.Common;
public sealed class StationMetadataProvider : ServiceBase, IStationMetadataProvider
{
    readonly SemaphoreSlim _lock = new(1, 1);
    const string StationSnapshotFileName = "tempest.station.snapshot.json";
    double? _elevationMeters;
    StationMetadata? _metadata;
    DateTimeOffset _lastRefreshUtc = DateTimeOffset.MinValue;
    IPlatformPaths? _platformPaths;
    IPlatformPaths PlatformPaths => _platformPaths ?? new DefaultPlatformPaths();
    ITempestRestClient? _tempestRestClient;
    ITempestRestClient TempestRestClient => NullPropertyGuard.Get(_isInitialized, _tempestRestClient, nameof(TempestRestClient));
    public Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        ITempestRestClient iTempestRestClient,
        CancellationToken externalCancellation = default,
        IPlatformPaths? iPlatformPaths = null
    )
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iTempestRestClient);

        InitializeBase(
            iLoggerResilient.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation
        );

        _tempestRestClient = iTempestRestClient;
        _platformPaths = iPlatformPaths;

        MarkReady();
        return Task.FromResult(true);
    }

    public async ValueTask<double?> GetStationElevationMetersAsync(CancellationToken cancellationToken = default)
    {
        var md = await GetStationMetadataAsync(cancellationToken).ConfigureAwait(false);
        return md?.ElevationMeters;
    }

    public async ValueTask<StationMetadata?> GetStationMetadataAsync(CancellationToken cancellationToken = default)
    {
        await Ready.ConfigureAwait(false);

        // Cache for 12 hours; elevation shouldn't change.
        var now = DateTimeOffset.UtcNow;
        if (_metadata is not null && now - _lastRefreshUtc < TimeSpan.FromHours(12))
            return _metadata;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_metadata is not null && now - _lastRefreshUtc < TimeSpan.FromHours(12))
                return _metadata;

            // 1) Try online fetch for freshest snapshot.
            TempestStationSnapshot? snapshot = null;
            try
            {
                snapshot = await TempestRestClient.GetStationSnapshotAsync(cancellationToken).ConfigureAwait(false);
                TryPersistSnapshot(snapshot);
            }
            catch (Exception ex)
            {
                ILogger.Warning($"Failed to fetch Tempest station snapshot from REST API; will try cached snapshot. {ex.Message}");
            }

            // 2) Fall back to local snapshot.
            snapshot ??= TryLoadPersistedSnapshot();

            if (snapshot is null)
            {
                _lastRefreshUtc = now;
                return null;
            }

            var previous = _metadata;

            _metadata = TryExtractStationMetadata(snapshot.RawJson, retrievedUtc: now);
            _elevationMeters = _metadata?.ElevationMeters;
            _lastRefreshUtc = now;

            try
            {
                if (_metadata is not null && !Equals(_metadata, previous))
                    IEventRelayBasic.Send(_metadata);
            }
            catch { }

            return _metadata;
        }
        finally
        {
            _lock.Release();
        }
    }

    bool TryPersistSnapshot(TempestStationSnapshot snapshot)
    {
        try
        {
            var dir = PlatformPaths.AppDataDirectory;
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, StationSnapshotFileName);
            File.WriteAllText(path, snapshot.RawJson);
            return true;
        }
        catch (Exception ex)
        {
            ILogger.Warning($"Failed to persist station snapshot. {ex.Message}");
            return false;
        }
    }

    TempestStationSnapshot? TryLoadPersistedSnapshot()
    {
        try
        {
            var path = Path.Combine(PlatformPaths.AppDataDirectory, StationSnapshotFileName);
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            // StationId may be inside json, but we can treat it as optional for elevation extraction.
            return new TempestStationSnapshot(
                StationId: 0,
                RetrievedUtc: DateTimeOffset.MinValue,
                RawJson: json
            );
        }
        catch (Exception ex)
        {
            ILogger.Warning($"Failed to load cached station snapshot. {ex.Message}");
            return null;
        }
    }

    static StationMetadata? TryExtractStationMetadata(string rawJson, DateTimeOffset retrievedUtc)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return null;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (!TryGetFirstStation(root, out var station))
                return null;

            long stationId = 0;
            try
            {
                if (station.TryGetProperty("station_id", out var sid) && sid.TryGetInt64(out var sidVal))
                    stationId = sidVal;
            }
            catch { }

            var stationName = TryGetString(station, "name") ?? TryGetString(station, "public_name");
            var latitude = TryGetDouble(station, "latitude");
            var longitude = TryGetDouble(station, "longitude");

            double? elevation = null;
            try
            {
                if (station.TryGetProperty("station_meta", out var meta) && meta.ValueKind == System.Text.Json.JsonValueKind.Object)
                    elevation = TryGetDouble(meta, "elevation") ?? TryGetDouble(meta, "elevation_m") ?? TryGetDouble(meta, "elevation_meters");
            }
            catch { }

            var tempestDeviceName = TryExtractTempestDeviceName(station);

            return new StationMetadata(
                StationId: stationId,
                StationName: stationName,
                TempestDeviceName: tempestDeviceName,
                Latitude: latitude,
                Longitude: longitude,
                ElevationMeters: elevation,
                RetrievedUtc: retrievedUtc
            );
        }
        catch
        {
            return null;
        }

        static bool TryGetFirstStation(System.Text.Json.JsonElement root, out System.Text.Json.JsonElement station)
        {
            station = default;
            if (!root.TryGetProperty("stations", out var stations)) return false;
            if (stations.ValueKind != System.Text.Json.JsonValueKind.Array) return false;
            if (stations.GetArrayLength() < 1) return false;
            station = stations[0];
            return station.ValueKind == System.Text.Json.JsonValueKind.Object;
        }

        static double? TryGetDouble(System.Text.Json.JsonElement node, string propertyName)
        {
            if (!node.TryGetProperty(propertyName, out var p)) return null;
            if (p.ValueKind != System.Text.Json.JsonValueKind.Number) return null;
            return p.TryGetDouble(out var d) ? d : null;
        }

        static string? TryGetString(System.Text.Json.JsonElement node, string propertyName)
        {
            if (!node.TryGetProperty(propertyName, out var p)) return null;
            return p.ValueKind == System.Text.Json.JsonValueKind.String ? p.GetString() : null;
        }

        static string? TryExtractTempestDeviceName(System.Text.Json.JsonElement station)
        {
            // Prefer the Tempest device: device_type == "ST".
            if (!station.TryGetProperty("devices", out var devices) || devices.ValueKind != System.Text.Json.JsonValueKind.Array)
                return null;

            foreach (var d in devices.EnumerateArray())
            {
                if (d.ValueKind != System.Text.Json.JsonValueKind.Object)
                    continue;

                if (!d.TryGetProperty("device_type", out var dt) || dt.ValueKind != System.Text.Json.JsonValueKind.String)
                    continue;

                var deviceType = dt.GetString();
                if (!string.Equals(deviceType, "ST", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!d.TryGetProperty("device_meta", out var meta) || meta.ValueKind != System.Text.Json.JsonValueKind.Object)
                    continue;

                return TryGetString(meta, "name");
            }

            return null;
        }
    }
}
