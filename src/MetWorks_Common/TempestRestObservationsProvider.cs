namespace MetWorks.Common;

using MetWorks.Constants;
using MetWorks.Interfaces;

public sealed class TempestRestObservationsProvider : ServiceBase, ITempestRestObservationsProvider
{
    const string ObservationsSnapshotFileName = "tempest.observations.snapshot.json";
    const string ObservationsSnapshotMetaFileName = "tempest.observations.snapshot.meta.json";

    const int DefaultRefreshIntervalMinutes = 15;
    const int MinRefreshIntervalMinutes = 5;
    const int MaxRefreshIntervalMinutes = 24 * 60;

    readonly SemaphoreSlim _lock = new(1, 1);
    readonly SemaphoreSlim _wakeSignal = new(0, 1);

    ITempestRestClient? _tempestRestClient;
    ITempestRestClient TempestRestClient => NullPropertyGuard.Get(_isInitialized, _tempestRestClient, nameof(TempestRestClient));

    IPlatformPaths? _platformPaths;
    IPlatformPaths PlatformPaths => _platformPaths ?? new DefaultPlatformPaths();

    TempestRestObservationsSnapshot? _latest;

    DateTimeOffset _lastSuccessUtc = DateTimeOffset.MinValue;
    DateTimeOffset _nextScheduledRefreshUtc = DateTimeOffset.MinValue;

    int _manualRefreshPending;

    public TempestRestObservationsProvider() { }

    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        ITempestRestClient iTempestRestClient,
        CancellationToken externalCancellation = default,
        IPlatformPaths? iPlatformPaths = null,
        ProvenanceTracker? provenanceTracker = null
    )
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
            provenanceTracker
        );

        _tempestRestClient = iTempestRestClient;
        _platformPaths = iPlatformPaths;

        _nextScheduledRefreshUtc = DateTimeOffset.UtcNow;

        StartBackground(ct => RefreshLoopAsync(ct));

        MarkReady();
        return Task.FromResult(true);
    }

    public ValueTask<TempestRestObservationsSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_latest);
    }

    public Task RequestRefreshAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Coalesce repeated requests so we don't enqueue unbounded refresh work.
        if (Interlocked.Exchange(ref _manualRefreshPending, 1) == 0)
        {
            try { _wakeSignal.Release(); } catch { }
        }

        return Task.CompletedTask;
    }

    async Task RefreshLoopAsync(CancellationToken token)
    {
        // Schedule refreshes on a cadence. On-demand refreshes do not shift the cadence.
        while (!token.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;

                var delay = _nextScheduledRefreshUtc > now ? _nextScheduledRefreshUtc - now : TimeSpan.Zero;
                if (delay > TimeSpan.Zero)
                {
                    // Wait for the next scheduled time OR a manual refresh request.
                    var delayTask = Task.Delay(delay, token);
                    var wakeTask = _wakeSignal.WaitAsync(token);
                    await Task.WhenAny(delayTask, wakeTask).ConfigureAwait(false);
                }

                token.ThrowIfCancellationRequested();

                // If we were woken by a manual request, do a refresh but do not adjust the schedule.
                if (Interlocked.Exchange(ref _manualRefreshPending, 0) == 1)
                {
                    try
                    {
                        // Drain any queued wake signal so we don't immediately loop again.
                        while (_wakeSignal.CurrentCount > 0)
                            _wakeSignal.Wait(0);
                    }
                    catch
                    {
                    }

                    await RefreshOnceAsync(DateTimeOffset.UtcNow, token).ConfigureAwait(false);
                    continue;
                }

                // Scheduled refresh.
                var interval = TimeSpan.FromMinutes(ReadRefreshIntervalMinutes(ISettingRepository));
                await RefreshOnceAsync(DateTimeOffset.UtcNow, token).ConfigureAwait(false);
                _nextScheduledRefreshUtc = DateTimeOffset.UtcNow + interval;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex)
            {
                try { ILogger.Warning("TempestRestObservationsProvider: HTTP failure", ex); } catch { }
            }
            catch (InvalidOperationException ex)
            {
                try { ILogger.Warning("TempestRestObservationsProvider: failure", ex); } catch { }
            }
            catch (Exception ex)
            {
                try { ILogger.Warning($"TempestRestObservationsProvider: unexpected failure. {ex.Message}"); } catch { }
            }
        }
    }

    async Task RefreshOnceAsync(DateTimeOffset nowUtc, CancellationToken token)
    {
        await _lock.WaitAsync(token).ConfigureAwait(false);
        try
        {
            TempestStationObservationsSnapshot? snapshot = null;

            try
            {
                snapshot = await TempestRestClient.GetStationObservationsAsync(token).ConfigureAwait(false);
                _lastSuccessUtc = nowUtc;
                TryPersistSnapshot(snapshot);
            }
            catch (HttpRequestException ex)
            {
                // Offline mode is expected; fall back to cached snapshot.
                try { ILogger.Warning($"TempestRestObservationsProvider: failed to fetch observations; will try cached snapshot. {ex.Message}"); } catch { }
            }
            catch (InvalidOperationException ex)
            {
                try { ILogger.Warning($"TempestRestObservationsProvider: failed to fetch observations; will try cached snapshot. {ex.Message}"); } catch { }
            }

            snapshot ??= TryLoadPersistedSnapshot();
            if (snapshot is null) return;

            var message = new TempestRestObservationsSnapshot(
                StationId: snapshot.StationId,
                RetrievedUtc: snapshot.RetrievedUtc,
                RawJson: snapshot.RawJson
            );

            _latest = message;

            try
            {
                IEventRelayBasic.Send(message);
            }
            catch (Exception ex)
            {
                // Event relay should be resilient, but do not allow failures to kill the loop.
                try { ILogger.Warning($"TempestRestObservationsProvider: failed to publish snapshot. {ex.Message}"); } catch { }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    sealed record PersistedMeta
    {
        public long StationId { get; init; }
        public DateTimeOffset RetrievedUtc { get; init; }

        // Explicit contract: persisted raw JSON snapshots are always metric and independent of user preferences.
        public string UnitsSystem { get; init; } = "metric";

        // Best-effort info about what the payload contains.
        public int? ObsRowCount { get; init; }
        public long? OldestEpochSeconds { get; init; }
        public long? NewestEpochSeconds { get; init; }
    }

    bool TryPersistSnapshot(TempestStationObservationsSnapshot snapshot)
    {
        try
        {
            var dir = PlatformPaths.AppDataDirectory;
            Directory.CreateDirectory(dir);

            var rawPath = Path.Combine(dir, ObservationsSnapshotFileName);
            var metaPath = Path.Combine(dir, ObservationsSnapshotMetaFileName);

            File.WriteAllText(rawPath, snapshot.RawJson);

            var meta = TryBuildMeta(snapshot);
            File.WriteAllText(metaPath, JsonSerializer.Serialize(meta));
            return true;
        }
        catch (IOException ex)
        {
            try { ILogger.Warning($"TempestRestObservationsProvider: failed to persist observations snapshot. {ex.Message}"); } catch { }
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            try { ILogger.Warning($"TempestRestObservationsProvider: failed to persist observations snapshot. {ex.Message}"); } catch { }
            return false;
        }
    }

    TempestStationObservationsSnapshot? TryLoadPersistedSnapshot()
    {
        try
        {
            var dir = PlatformPaths.AppDataDirectory;
            var rawPath = Path.Combine(dir, ObservationsSnapshotFileName);
            var metaPath = Path.Combine(dir, ObservationsSnapshotMetaFileName);

            if (!File.Exists(rawPath))
                return null;

            var rawJson = File.ReadAllText(rawPath);
            if (string.IsNullOrWhiteSpace(rawJson))
                return null;

            PersistedMeta? meta = null;
            if (File.Exists(metaPath))
            {
                try
                {
                    meta = JsonSerializer.Deserialize<PersistedMeta>(File.ReadAllText(metaPath));
                }
                catch (Exception ex)
                {
                    try { ILogger.Warning($"TempestRestObservationsProvider: failed to parse observations snapshot meta. {ex.Message}"); } catch { }
                }
            }

            return new TempestStationObservationsSnapshot(
                StationId: meta?.StationId ?? 0,
                RetrievedUtc: meta?.RetrievedUtc ?? DateTimeOffset.MinValue,
                RawJson: rawJson);
        }
        catch (IOException ex)
        {
            try { ILogger.Warning($"TempestRestObservationsProvider: failed to load cached observations snapshot. {ex.Message}"); } catch { }
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            try { ILogger.Warning($"TempestRestObservationsProvider: failed to load cached observations snapshot. {ex.Message}"); } catch { }
            return null;
        }
    }

    PersistedMeta TryBuildMeta(TempestStationObservationsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        int? obsRowCount = null;
        long? oldest = null;
        long? newest = null;

        try
        {
            using var doc = JsonDocument.Parse(snapshot.RawJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException("Root is not an object.");

            if (root.TryGetProperty("obs", out var obsEl) && obsEl.ValueKind == JsonValueKind.Array)
            {
                obsRowCount = obsEl.GetArrayLength();
                foreach (var row in obsEl.EnumerateArray())
                {
                    long? epoch = row.ValueKind switch
                    {
                        JsonValueKind.Array => TryReadEpochFromObsArray(row),
                        JsonValueKind.Object => TryReadEpochFromObsObject(row),
                        _ => null
                    };

                    if (epoch is null || epoch.Value <= 0)
                        continue;

                    oldest = oldest is null ? epoch : Math.Min(oldest.Value, epoch.Value);
                    newest = newest is null ? epoch : Math.Max(newest.Value, epoch.Value);
                }
            }
        }
        catch (Exception ex)
        {
            try { ILogger.Warning($"TempestRestObservationsProvider: failed to build observations snapshot meta. {ex.Message}"); } catch { }
        }

        return new PersistedMeta
        {
            StationId = snapshot.StationId,
            RetrievedUtc = snapshot.RetrievedUtc,
            ObsRowCount = obsRowCount,
            OldestEpochSeconds = oldest,
            NewestEpochSeconds = newest
        };

        static long? TryReadEpochFromObsArray(JsonElement row)
        {
            // Observed shape: obs is an array-of-arrays where index 0 is epoch seconds.
            if (row.ValueKind != JsonValueKind.Array)
                return null;

            if (row.GetArrayLength() <= 0)
                return null;

            var first = row[0];
            if (first.ValueKind != JsonValueKind.Number)
                return null;

            return first.TryGetInt64(out var seconds) ? seconds : null;
        }

        static long? TryReadEpochFromObsObject(JsonElement row)
        {
            // Observed shape: obs is an array-of-objects with a "timestamp" field.
            if (row.ValueKind != JsonValueKind.Object)
                return null;

            if (!row.TryGetProperty("timestamp", out var p) || p.ValueKind != JsonValueKind.Number)
                return null;

            return p.TryGetInt64(out var seconds) ? seconds : null;
        }
    }

    static int ReadRefreshIntervalMinutes(ISettingRepository settingRepository)
    {
        var minutes = settingRepository.GetValueOrDefault<int>(
            LookupDictionaries.TempestObservationsGroupSettingsDefinition.BuildPath(SettingConstants.TempestObservations_refreshIntervalMinutes));

        if (minutes <= 0)
            minutes = DefaultRefreshIntervalMinutes;

        if (minutes < MinRefreshIntervalMinutes)
            minutes = MinRefreshIntervalMinutes;
        else if (minutes > MaxRefreshIntervalMinutes)
            minutes = MaxRefreshIntervalMinutes;

        return minutes;
    }
}
