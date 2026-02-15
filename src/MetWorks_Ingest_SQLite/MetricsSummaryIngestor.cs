namespace MetWorks.Ingest.SQLite;

using MetWorks.Common.Metrics;

public sealed class MetricsSummaryIngestor : ServiceBase, IMetricsSummaryPersister
{
    const int DefaultSchemaVersion = 1;
    string _tableName = string.Empty;
    bool _autoCreateTable;

    Guid _installationIdGuid;
    Guid _applicationIdGuid;

    int _tableEnsured;

    MetWorks.Persistence.Metrics.IMetricsDatabaseReadiness? _databaseReadiness;
    MetWorks.Persistence.Metrics.IMetricsSummaryRepository? _repository;


    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        IMetricsLatestSnapshot iMetricsLatestSnapshot,
        MetWorks.Persistence.Metrics.IMetricsDatabaseReadiness metricsDatabaseReadiness,
        MetWorks.Persistence.Metrics.IMetricsSummaryRepository metricsSummaryRepository,
        CancellationToken externalCancellation,
        ProvenanceTracker provenanceTracker
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);
        ArgumentNullException.ThrowIfNull(metricsDatabaseReadiness);
        ArgumentNullException.ThrowIfNull(metricsSummaryRepository);

        InitializeBase(
            iLogger.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker
        );

        _ = iMetricsLatestSnapshot;

        _databaseReadiness = metricsDatabaseReadiness;
        _repository = metricsSummaryRepository;

        _tableName = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_tableName));

        if (string.IsNullOrWhiteSpace(_tableName))
            _tableName = "metrics_summary";

        _autoCreateTable = iSettingRepository.GetValueOrDefault<bool>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_autoCreateTable));

        var installationIdRaw = iInstanceIdentifier.GetOrCreateInstallationId();
        if (!Guid.TryParse(installationIdRaw, out _installationIdGuid))
            _installationIdGuid = Guid.Empty;

        var appIdRaw = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_applicationId));

        if (!Guid.TryParse(appIdRaw, out _applicationIdGuid))
            _applicationIdGuid = Guid.Empty;

        MarkReady();
        return Task.FromResult(true);
    }

    public async Task PersistAsync(
        DateTime capturedUtc,
        int captureIntervalSeconds,
        int schemaVersion,
        string jsonMetricsSummary,
        CancellationToken cancellationToken = default
    )
    {
        await Ready.ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonMetricsSummary)) return;

        cancellationToken.ThrowIfCancellationRequested();

        var readiness = _databaseReadiness;
        if (readiness is null)
            throw new InvalidOperationException($"{nameof(MetWorks.Persistence.Metrics.IMetricsDatabaseReadiness)} is not initialized.");

        var repository = _repository;
        if (repository is null)
            throw new InvalidOperationException($"{nameof(MetWorks.Persistence.Metrics.IMetricsSummaryRepository)} is not initialized.");

        try
        {
            if (_autoCreateTable)
                await EnsureTableOnceAsync(readiness, cancellationToken).ConfigureAwait(false);

            await repository.InsertAsync(
                new MetWorks.Persistence.Metrics.MetricsSummaryInsertRow(
                    Table: _tableName,
                    CombId: IdGenerator.CreateCombGuid().ToString(),
                    InstallationId: _installationIdGuid == Guid.Empty ? null : _installationIdGuid.ToString(),
                    CapturedUtc: capturedUtc,
                    CaptureIntervalSeconds: captureIntervalSeconds,
                    ApplicationId: _applicationIdGuid == Guid.Empty ? null : _applicationIdGuid.ToString(),
                    SchemaVersion: schemaVersion <= 0 ? DefaultSchemaVersion : schemaVersion,
                    JsonMetricsSummary: jsonMetricsSummary),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            ILogger.Warning($"MetricsSummaryIngestorSqlite write failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            ILogger.Warning($"MetricsSummaryIngestorSqlite write failed: {ex.Message}");
        }
    }

    async Task EnsureTableOnceAsync(MetWorks.Persistence.Metrics.IMetricsDatabaseReadiness readiness, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _tableEnsured, 1, 0) != 0) return;

        try
        {
            await readiness.EnsureReadyAsync(_tableName, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            Interlocked.Exchange(ref _tableEnsured, 0);
            throw;
        }
    }
}
