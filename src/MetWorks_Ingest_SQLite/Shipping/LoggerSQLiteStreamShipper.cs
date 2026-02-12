namespace MetWorks.Ingest.SQLite.Shipping;

public sealed class LoggerSQLiteStreamShipper : ServiceBase
{
    const string Source = "logger_sqlite";

    const int DefaultShipIntervalSeconds = 30;
    const int DefaultMaxBatchRows = 500;

    string _connectionString = string.Empty;
    string _dbPath = string.Empty;
    string _tableName = "log";
    Guid _installationIdGuid;

    string _endpointUrl = string.Empty;
    int _shipIntervalSeconds = DefaultShipIntervalSeconds;
    int _maxBatchRows = DefaultMaxBatchRows;

    HttpClient? _httpClient;

    public LoggerSQLiteStreamShipper()
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
            ILogger.Information("LoggerSQLiteStreamShipper is disabled via settings");
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
            LookupDictionaries.LoggerSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.LoggerSQLite_dbPath));

        _tableName = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.LoggerSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.LoggerSQLite_tableName));

        if (string.IsNullOrWhiteSpace(_tableName))
            _tableName = "log";

        var iid = iInstanceIdentifier.GetOrCreateInstallationId();
        if (!Guid.TryParse(iid, out _installationIdGuid))
            _installationIdGuid = Guid.Empty;

        if (string.IsNullOrWhiteSpace(_endpointUrl))
        {
            ILogger.Warning("LoggerSQLiteStreamShipper endpointUrl is not configured; shipper will not run.");
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
            ILogger.Warning("LoggerSQLiteStreamShipper has no SQLite connection configured; shipper will not run.");
            try { MarkReady(); } catch { }
            return true;
        }

        StartBackground(ct => ShipLoopAsync(TimeSpan.FromSeconds(_shipIntervalSeconds), ct));

        try { MarkReady(); } catch { }
        ILogger.Information($"LoggerSQLiteStreamShipper started (interval={_shipIntervalSeconds}s, maxBatchRows={_maxBatchRows}, table={_tableName})");
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
                ILogger.Warning($"LoggerSQLiteStreamShipper: HTTP failure: {ex.Message}");
            }
            catch (SqliteException ex)
            {
                ILogger.Warning($"LoggerSQLiteStreamShipper: SQLite failure: {ex.Message} (code={ex.SqliteErrorCode})");
            }
            catch (InvalidOperationException ex)
            {
                ILogger.Warning($"LoggerSQLiteStreamShipper: failure: {ex.Message}");
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

        await LoggerSQLiteStreamShipping.ShipOnceAsync(
            conn,
            installationId: _installationIdGuid,
            source: Source,
            table: _tableName,
            maxBatchRows: _maxBatchRows,
            httpClient: _httpClient,
            endpointUrl: _endpointUrl,
            retention: LoggerSQLiteStreamShipping.DefaultRetention,
            token).ConfigureAwait(false);
    }
}
