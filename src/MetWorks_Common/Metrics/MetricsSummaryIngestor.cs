namespace MetWorks.Common.Metrics;

using MetWorks.Constants;
using MetWorks.Interfaces;
using Npgsql;

public sealed class MetricsSummaryIngestor : ServiceBase
{
    const int DefaultSchemaVersion = 1;

    string _connectionString = string.Empty;
    string _tableName = "metrics_summary";
    bool _autoCreateTable;

    Guid _installationIdGuid;
    Guid _applicationIdGuid;

    int _tableEnsured;

    MetricsLatestSnapshotStore? _latest;

    public Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        string installationId,
        IMetricsLatestSnapshot? metricsLatestSnapshotStore = null,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null)
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(installationId);

        InitializeBase(
            iLoggerResilient.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker);

        _latest = metricsLatestSnapshotStore as MetricsLatestSnapshotStore;

        _connectionString = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_connectionString));

        _tableName = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_tableName));

        if (string.IsNullOrWhiteSpace(_tableName))
            _tableName = "metrics_summary";

        _autoCreateTable = iSettingRepository.GetValueOrDefault<bool>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_autoCreateTable));

        if (!Guid.TryParse(installationId, out _installationIdGuid))
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
        CancellationToken cancellationToken = default)
    {
        await Ready.ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(_connectionString))
            return;

        if (string.IsNullOrWhiteSpace(jsonMetricsSummary))
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var attemptUtc = DateTime.UtcNow;
        _latest?.RecordPersistAttempt(attemptUtc);

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            if (_autoCreateTable)
                await EnsureTableOnceAsync(conn, cancellationToken).ConfigureAwait(false);

            var combId = IdGenerator.CreateCombGuid();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
INSERT INTO public.{_tableName} (
    comb_id,
    installation_id,
    captured_utc,
    capture_interval_seconds,
    application_id,
    schema_version,
    json_metrics_summary
)
VALUES (
    @comb_id,
    @installation_id,
    @captured_utc,
    @capture_interval_seconds,
    @application_id,
    @schema_version,
    @json_metrics_summary
);";

            cmd.Parameters.AddWithValue("comb_id", NpgsqlTypes.NpgsqlDbType.Uuid, combId);

            if (_installationIdGuid != Guid.Empty)
                cmd.Parameters.AddWithValue("installation_id", NpgsqlTypes.NpgsqlDbType.Uuid, _installationIdGuid);
            else
                cmd.Parameters.AddWithValue("installation_id", DBNull.Value);

            cmd.Parameters.AddWithValue("captured_utc", NpgsqlTypes.NpgsqlDbType.TimestampTz, capturedUtc);
            cmd.Parameters.AddWithValue("capture_interval_seconds", NpgsqlTypes.NpgsqlDbType.Integer, captureIntervalSeconds);
            cmd.Parameters.AddWithValue("application_id", NpgsqlTypes.NpgsqlDbType.Uuid, _applicationIdGuid);
            cmd.Parameters.AddWithValue("schema_version", NpgsqlTypes.NpgsqlDbType.Integer, schemaVersion <= 0 ? DefaultSchemaVersion : schemaVersion);
            cmd.Parameters.AddWithValue("json_metrics_summary", NpgsqlTypes.NpgsqlDbType.Jsonb, jsonMetricsSummary);

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            _latest?.RecordPersistSuccess(attemptUtc);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (NpgsqlException ex)
        {
            ILogger.Warning($"MetricsSummaryIngestor write failed: {ex.Message}");
            _latest?.RecordPersistFailure(attemptUtc, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ILogger.Warning($"MetricsSummaryIngestor write failed: {ex.Message}");
            _latest?.RecordPersistFailure(attemptUtc, ex.Message);
        }
    }

    async Task EnsureTableOnceAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _tableEnsured, 1, 0) != 0)
            return;

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

    async Task EnsureTableAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        var safeTableName = _tableName;
        if (!IsSafeIdentifier(safeTableName))
            throw new InvalidOperationException($"Invalid table name '{safeTableName}'.");

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
CREATE TABLE IF NOT EXISTS public.{safeTableName}
(
    comb_id UUID PRIMARY KEY,
    installation_id UUID NULL,
    captured_utc TIMESTAMPTZ NOT NULL,
    capture_interval_seconds INT NOT NULL,
    application_id UUID NOT NULL,
    schema_version INT NOT NULL,
    json_metrics_summary JSONB NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_{safeTableName}_captured_utc ON public.{safeTableName}(captured_utc);
CREATE INDEX IF NOT EXISTS idx_{safeTableName}_installation_id ON public.{safeTableName}(installation_id);
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
