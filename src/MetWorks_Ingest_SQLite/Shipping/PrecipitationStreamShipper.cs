namespace MetWorks.Ingest.SQLite.Shipping;
public sealed class PrecipitationStreamShipper : ServiceBase
{
    const string Source = "precipitation";
    const string Table = "precipitation";
    const string DdlScript = "Ingest/SQLite/precipitation.sql";

    const int DefaultShipIntervalSeconds = 30;
    const int DefaultMaxBatchRows = 500;

    string _connectionString = string.Empty;
    string _dbPath = string.Empty;
    Guid _installationIdGuid;

    string _endpointUrl = string.Empty;
    int _shipIntervalSeconds = DefaultShipIntervalSeconds;
    int _maxBatchRows = DefaultMaxBatchRows;

    HttpClient? _httpClient;

    public PrecipitationStreamShipper()
    {
    }

    public async Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        HttpClient httpClient,
        CancellationToken externalCancellation,
        ProvenanceTracker provenanceTracker
    )
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(provenanceTracker);

        InitializeBase(
            iLoggerResilient.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker
        );

        _httpClient = httpClient;

        var enabled = iSettingRepository.GetValueOrDefault<bool>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildSettingPath(SettingConstants.StreamShipping_enabled));

        if (!enabled)
        {
            ILogger.Information("PrecipitationStreamShipper is disabled via settings");
            try { MarkReady(); } catch { }
            return true;
        }

        _endpointUrl = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildSettingPath(SettingConstants.StreamShipping_endpointUrl));

        _shipIntervalSeconds = iSettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildSettingPath(SettingConstants.StreamShipping_shipIntervalSeconds));

        if (_shipIntervalSeconds <= 0)
            _shipIntervalSeconds = DefaultShipIntervalSeconds;

        _maxBatchRows = iSettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildSettingPath(SettingConstants.StreamShipping_maxBatchRows));

        if (_maxBatchRows <= 0)
            _maxBatchRows = DefaultMaxBatchRows;

        _connectionString = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_connectionString));

        _dbPath = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_dbPath));

        var iid = iInstanceIdentifier.GetOrCreateInstallationId();
        if (!Guid.TryParse(iid, out _installationIdGuid))
            _installationIdGuid = Guid.Empty;

        if (string.IsNullOrWhiteSpace(_endpointUrl))
        {
            ILogger.Warning("PrecipitationStreamShipper endpointUrl is not configured; shipper will not run.");
            try { MarkReady(); } catch { }
            return true;
        }

        if (string.IsNullOrWhiteSpace(_connectionString) && !string.IsNullOrWhiteSpace(_dbPath))
        {
            var appDataDir = new DefaultPlatformPaths().AppDataDirectory;
            var resolvedDbPath = Path.IsPathRooted(_dbPath)
                ? _dbPath
                : Path.Combine(appDataDir, _dbPath);

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = resolvedDbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            ILogger.Warning("PrecipitationStreamShipper has no SQLite connection configured; shipper will not run.");
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
            catch (SqliteException ex)
            {
                ILogger.Warning($"PrecipitationStreamShipper: SQLite failure: {ex.Message} (code={ex.SqliteErrorCode})");
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

        if (_installationIdGuid == Guid.Empty)
            throw new InvalidOperationException("Installation id is not initialized.");

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(token).ConfigureAwait(false);

        await StandardReadingsStreamShipping.ShipOnceAsync(
            conn,
            installationId: _installationIdGuid,
            source: Source,
            table: Table,
            ddlScript: DdlScript,
            maxBatchRows: _maxBatchRows,
            httpClient: _httpClient,
            endpointUrl: _endpointUrl,
            token).ConfigureAwait(false);
    }
}
