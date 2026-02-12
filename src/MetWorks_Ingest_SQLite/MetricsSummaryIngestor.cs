namespace MetWorks.Ingest.SQLite;

using MetWorks.Common.Metrics;

public sealed class MetricsSummaryIngestor : ServiceBase, IMetricsSummaryPersister
{
    const int DefaultSchemaVersion = 1;

    string _connectionString = string.Empty;
    string _dbPath = string.Empty;
    string _tableName = "metrics_summary";
    bool _autoCreateTable;

    Guid _installationIdGuid;
    Guid _applicationIdGuid;

    int _tableEnsured;


    public Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        IMetricsLatestSnapshot iMetricsLatestSnapshot,
        CancellationToken externalCancellation,
        ProvenanceTracker provenanceTracker
    )
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);

        InitializeBase(
            iLoggerResilient.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker
        );

        _ = iMetricsLatestSnapshot;

        _connectionString = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_connectionString));

        _dbPath = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_dbPath));

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

        if (string.IsNullOrWhiteSpace(_connectionString)) return;
        if (string.IsNullOrWhiteSpace(jsonMetricsSummary)) return;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            if (_autoCreateTable)
                await EnsureTableOnceAsync(conn, cancellationToken).ConfigureAwait(false);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
INSERT INTO ""{_tableName}"" (
    comb_id,
    installation_id,
    captured_utc,
    capture_interval_seconds,
    application_id,
    schema_version,
    json_metrics_summary
)
VALUES (
    $comb_id,
    $installation_id,
    $captured_utc,
    $capture_interval_seconds,
    $application_id,
    $schema_version,
    $json_metrics_summary
);";

            cmd.Parameters.AddWithValue("$comb_id", IdGenerator.CreateCombGuid().ToString());
            cmd.Parameters.AddWithValue("$installation_id", _installationIdGuid != Guid.Empty ? _installationIdGuid.ToString() : DBNull.Value);
            cmd.Parameters.AddWithValue("$captured_utc", capturedUtc.ToUniversalTime().ToString("O"));
            cmd.Parameters.AddWithValue("$capture_interval_seconds", captureIntervalSeconds);
            cmd.Parameters.AddWithValue("$application_id", _applicationIdGuid != Guid.Empty ? _applicationIdGuid.ToString() : DBNull.Value);
            cmd.Parameters.AddWithValue("$schema_version", schemaVersion <= 0 ? DefaultSchemaVersion : schemaVersion);
            cmd.Parameters.AddWithValue("$json_metrics_summary", jsonMetricsSummary);

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SqliteException ex)
        {
            ILogger.Warning($"MetricsSummaryIngestorSqlite write failed: {ex.Message} (code={ex.SqliteErrorCode})");
        }
        catch (InvalidOperationException ex)
        {
            ILogger.Warning($"MetricsSummaryIngestorSqlite write failed: {ex.Message}");
        }
    }

    async Task EnsureTableOnceAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _tableEnsured, 1, 0) != 0) return;

        try
        {
            await EnsureTableAsync(conn, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            Interlocked.Exchange(ref _tableEnsured, 0);
            throw;
        }
    }

    async Task EnsureTableAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        var safeTableName = _tableName;
        if (!IsSafeIdentifier(safeTableName))
            throw new InvalidOperationException($"Invalid table name '{safeTableName}'.");

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
CREATE TABLE IF NOT EXISTS ""{safeTableName}""
(
    comb_id TEXT PRIMARY KEY,
    installation_id TEXT NULL,
    captured_utc TEXT NOT NULL,
    capture_interval_seconds INTEGER NOT NULL,
    application_id TEXT NULL,
    schema_version INTEGER NOT NULL,
    json_metrics_summary TEXT NOT NULL,
    database_received_utc_timestampz TEXT DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);
CREATE INDEX IF NOT EXISTS idx_{safeTableName}_captured_utc ON ""{safeTableName}""(captured_utc);
CREATE INDEX IF NOT EXISTS idx_{safeTableName}_installation_id ON ""{safeTableName}""(installation_id);
";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    static bool IsSafeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var ok =
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') ||
                c == '_';
            if (!ok) return false;
        }
        return true;
    }
}
