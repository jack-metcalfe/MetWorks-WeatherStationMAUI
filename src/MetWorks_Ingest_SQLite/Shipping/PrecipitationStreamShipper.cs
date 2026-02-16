using MetWorks.Persistence.StreamShipping;

namespace MetWorks.Ingest.SQLite.Shipping;
public sealed class PrecipitationStreamShipper : ServiceBase
{
    const string Source = "precipitation";
    const string Table = "precipitation";
    const string DdlScript = "Ingest/SQLite/precipitation.sql";

    const int DefaultShipIntervalSeconds = 30;
    const int DefaultMaxBatchRows = 500;

    string _installationId = string.Empty;

    string _endpointUrl = string.Empty;
    int _shipIntervalSeconds = DefaultShipIntervalSeconds;
    int _maxBatchRows = DefaultMaxBatchRows;

    HttpClient? _httpClient;

    IStreamShippingDatabaseReadiness? _streamShippingDatabaseReadiness;
    IStreamShippingRepository? _streamShippingRepository;

    public PrecipitationStreamShipper()
    {
    }

    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        IStreamShippingDatabaseReadiness streamShippingDatabaseReadiness,
        IStreamShippingRepository streamShippingRepository,
        HttpClient httpClient,
        CancellationToken externalCancellation,
        ProvenanceTracker provenanceTracker
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);
        ArgumentNullException.ThrowIfNull(streamShippingDatabaseReadiness);
        ArgumentNullException.ThrowIfNull(streamShippingRepository);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(provenanceTracker);

        InitializeBase(
            iLogger.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker
        );

        _httpClient = httpClient;

        _streamShippingDatabaseReadiness = streamShippingDatabaseReadiness;
        _streamShippingRepository = streamShippingRepository;

        var enabled = iSettingRepository.GetValueOrDefault<bool>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_enabled));

        if (!enabled)
        {
            ILogger.Information("PrecipitationStreamShipper is disabled via settings");
            try { MarkReady(); } catch { }
            return true;
        }

        _endpointUrl = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_endpointUrl));

        _shipIntervalSeconds = iSettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_shipIntervalSeconds));

        if (_shipIntervalSeconds <= 0)
            _shipIntervalSeconds = DefaultShipIntervalSeconds;

        _maxBatchRows = iSettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_maxBatchRows));

        if (_maxBatchRows <= 0)
            _maxBatchRows = DefaultMaxBatchRows;

        _installationId = iInstanceIdentifier.GetOrCreateInstallationId();

        if (string.IsNullOrWhiteSpace(_endpointUrl))
        {
            ILogger.Warning("PrecipitationStreamShipper endpointUrl is not configured; shipper will not run.");
            try { MarkReady(); } catch { }
            return true;
        }

        if (string.IsNullOrWhiteSpace(_installationId))
        {
            ILogger.Warning("PrecipitationStreamShipper has no installation id; shipper will not run.");
            try { MarkReady(); } catch { }
            return true;
        }

        StartBackground(ct => ShipLoopAsync(TimeSpan.FromSeconds(_shipIntervalSeconds), ct));

        try { MarkReady(); } catch { }
        ILogger.Information($"PrecipitationStreamShipper started (interval={_shipIntervalSeconds}s, maxBatchRows={_maxBatchRows})");
        return true;
    }

    async Task ShipLoopAsync(TimeSpan interval, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, token).ConfigureAwait(false);
                await ShipOnceAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex)
            {
                ILogger.Warning($"PrecipitationStreamShipper: HTTP failure: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                ILogger.Warning($"PrecipitationStreamShipper: failure: {ex.Message}");
            }
        }
    }

    async Task ShipOnceAsync(CancellationToken token)
    {
        if (_httpClient is null)
            throw new InvalidOperationException("HttpClient is not initialized.");

        var readiness = _streamShippingDatabaseReadiness;
        if (readiness is null)
            throw new InvalidOperationException("Stream shipping database readiness is not initialized.");

        var repo = _streamShippingRepository;
        if (repo is null)
            throw new InvalidOperationException("Stream shipping repository is not initialized.");

        if (string.IsNullOrWhiteSpace(_installationId))
            throw new InvalidOperationException("Installation id is not initialized.");

        await readiness.EnsureReadyAsync(token).ConfigureAwait(false);

        var state = await repo.TryGetStateAsync(Source, token).ConfigureAwait(false);
        var lastAcked = state?.LastAckedRowId ?? 0;

        var rows = await repo.ReadStandardReadingsBatchAsync(
            table: Table,
            installationId: _installationId,
            lastAckedRowId: lastAcked,
            maxRows: _maxBatchRows,
            cancellationToken: token).ConfigureAwait(false);

        if (rows.Count == 0)
            return;

        var maxRowId = rows[^1].RowId;

        var ackedUpTo = await ObservationStreamShipper.UploadNdjsonAsync(
            httpClient: _httpClient,
            endpointUrl: _endpointUrl,
            table: Table,
            installationId: _installationId,
            rows: rows,
            token: token).ConfigureAwait(false);

        if (ackedUpTo is null)
            return;

        await repo.UpsertShippingProgressAsync(
            source: Source,
            lastShippedRowId: maxRowId,
            lastAckedRowId: ackedUpTo.Value,
            cancellationToken: token).ConfigureAwait(false);
    }
}
