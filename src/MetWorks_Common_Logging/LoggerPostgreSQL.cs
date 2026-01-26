namespace MetWorks.Common.Logging;
using Logger = Serilog.Core.Logger;
using ILogEventSink = Serilog.Core.ILogEventSink;
/// <summary>
/// Logger that writes to PostgreSQL using a lightweight custom Serilog sink (no third-party Serilog sink).
/// - Initialization succeeds even if DB/network is unavailable.
/// - Sink exposes a health flag (IsHealthy) that reflects current ability to write to Postgres.
/// - Schema creation runs in background with retry; writes are best-effort and will resume automatically when network returns.
/// - Future: swap or augment writes to persist to local SQLite when unhealthy and flush when healthy.
/// </summary>
public class LoggerPostgreSQL : ILogger
{
    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IInstanceIdentifier iInstanceIdentifier,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);

        if (_isInitialized)
            throw new InvalidOperationException($"{nameof(LoggerPostgreSQL)} is already initialized.");

        if (cancellationToken.IsCancellationRequested) return Task.FromResult(false);

        try
        {
            var connectionString = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.LoggerPostgreSQLGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerPostgreSQL_connectionString
                )
            );

            var tableName = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.LoggerPostgreSQLGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerPostgreSQL_tableName
                )
            );

            var minimumLevel = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.LoggerPostgreSQLGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerPostgreSQL_minimumLevel
                )
            );

            var autoCreateTable = iSettingRepository.GetValueOrDefault<bool>(
                LookupDictionaries.LoggerPostgreSQLGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerPostgreSQL_autoCreateTable
                )
            );

            // Keep settings visible on the instance
            _connectionString = connectionString;
            _tableName = tableName;

            // Build Serilog logger with our custom sink.
            // Enrich with installation id when available to match LoggerFile behavior.
            var loggerCfg = new LoggerConfiguration()
                .MinimumLevel.Is(ParseLevel(minimumLevel));

            try
            {
                var installationId = iInstanceIdentifier?.GetOrCreateInstallationId();
                if (!string.IsNullOrWhiteSpace(installationId))
                {
                    loggerCfg = loggerCfg.Enrich.WithProperty("InstallationId", installationId);
                }
            }
            catch
            {
                // Ignore enrichment failures - logging should not block initialization
            }

            // Pass a health callback so the outer class can expose a health flag.
            _iLogger = loggerCfg
                .WriteTo.Sink(new PostgresSink(connectionString, tableName, autoCreateTable, SetHealth))
                .CreateLogger();

            _isInitialized = true;
            _iLogger.Information("PostgreSQL logger initialized for table {TableName}", tableName);
        }
        catch (Exception exception)
        {
            // Keep behavior consistent: throw a single understandable exception when config reading fails.
            throw new InvalidOperationException("Failed to read settings configuration for PostgreSQL logger.", exception);
        }

        return Task.FromResult(true);
    }
    bool _isInitialized = false;
    Logger? _iLogger = null;
    Logger ILogger => NullPropertyGuard.Get(_isInitialized, _iLogger, nameof(ILogger));
    string? _connectionString = null;
    public string ConnectionString => NullPropertyGuard.Get(_isInitialized, _connectionString, nameof(ConnectionString));
    string? _tableName = null;
    public string TableName => NullPropertyGuard.Get(_isInitialized, _tableName, nameof(TableName));
    // Health flag backing store (0 = false, 1 = true) to make updates atomic.
    int _isHealthy = 1;
    /// <summary>
    /// True when the sink believes it can successfully write to the Postgres backend.
    /// This is updated by the sink on successful writes/ensure and set to false on write failures.
    /// Consumers may observe IsHealthy and optionally route logs to a local store (SQLite) when false.
    /// </summary>
    public bool IsHealthy => Interlocked.CompareExchange(ref _isHealthy, 1, 1) == 1;
    void SetHealth(bool healthy) => Interlocked.Exchange(ref _isHealthy, healthy ? 1 : 0);
    public LoggerPostgreSQL()
    {
    }
    public void Information(string message)
    {
        SysDiagDebug.WriteLine(message);
        ILogger.Information(message);
    }
    public void Warning(string message)
    {
        SysDiagDebug.WriteLine(message);
        ILogger.Warning(message);
    }
    public void Error(string message, Exception exception)
    {
        SysDiagDebug.WriteLine(message);
        ILogger.Error(message, exception);
    }
    public void Error(string message)
    {
        SysDiagDebug.WriteLine(message);
        ILogger.Error(message);
    }
    public void Debug(string message)
    {
        SysDiagDebug.WriteLine(message);
        ILogger.Debug(message);
    }
    public void Trace(string message)
    {
        SysDiagDebug.WriteLine(message);
        ILogger.Verbose(message);
    }
    private static LogEventLevel ParseLevel(string level) =>
        Enum.TryParse<LogEventLevel>(level, true, out var parsed)
            ? parsed
            : LogEventLevel.Information;
    public Exception LogExceptionAndReturn(Exception exception)
    {
        ILogger.Error("An error occurred", exception);
        return exception;
    }
    public Exception LogExceptionAndReturn(Exception exception, string message)
    {
        ILogger.Error(message, exception);
        return exception;
    }
    #region Lightweight Postgres sink

    class PostgresSink : ILogEventSink, IDisposable
    {
        readonly string _connectionString;
        readonly string _tableName;
        readonly bool _autoCreateTable;
        readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = false };

        static readonly Regex ValidIdentifier = new(@"^[A-Za-z0-9_]+$");

        // Background ensure task cancellation
        readonly CancellationTokenSource _cts = new();
        Task? _ensureTask;

        // Callback to notify outer class of health changes
        readonly Action<bool>? _setHealth;

        public PostgresSink(string connectionString, string tableName, bool autoCreateTable, Action<bool>? setHealth = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString is required.", nameof(connectionString));

            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName is required.", nameof(tableName));

            if (!ValidIdentifier.IsMatch(tableName))
                throw new ArgumentException("Table name contains invalid characters. Only letters, digits and underscore are allowed.", nameof(tableName));

            _connectionString = connectionString;
            _tableName = tableName;
            _autoCreateTable = autoCreateTable;
            _setHealth = setHealth;

            // Do not perform blocking DB work in ctor. Run schema creation in background with retry.
            if (_autoCreateTable)
            {
                _ensureTask = Task.Run(() => EnsureTableWithRetryAsync(_cts.Token));
            }
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    INSERT INTO ""{_tableName}"" (timestamp, level, message, exception, properties, installation_id)
                    VALUES (@ts, @level, @message, @exception, @properties::jsonb, @installation_id)";

                cmd.Parameters.AddWithValue("ts", logEvent.Timestamp.UtcDateTime);
                cmd.Parameters.AddWithValue("level", logEvent.Level.ToString());
                var rendered = logEvent.RenderMessage();
                cmd.Parameters.AddWithValue("message", rendered ?? string.Empty);
                cmd.Parameters.AddWithValue("exception", logEvent.Exception?.ToString() ?? (object)DBNull.Value);

                // Extract installation id from properties if present, then serialize remaining properties to JSON.
                string? installationIdStr = null;
                if (logEvent.Properties.TryGetValue("InstallationId", out var installProp))
                {
                    try
                    {
                        if (installProp is Serilog.Events.ScalarValue sv)
                        {
                            if (sv.Value is Guid g) installationIdStr = g.ToString();
                            else if (sv.Value is string s) installationIdStr = s;
                            else installationIdStr = sv.ToString()?.Trim('"');
                        }
                        else
                        {
                            installationIdStr = installProp.ToString()?.Trim('"');
                        }
                    }
                    catch { installationIdStr = installProp.ToString()?.Trim('"'); }
                }

                var props = PropertiesToDictionary(logEvent);
                if (props.ContainsKey("InstallationId")) props.Remove("InstallationId");
                var propsJson = JsonSerializer.Serialize(props, _jsonOptions);
                cmd.Parameters.AddWithValue("properties", propsJson);

                if (!string.IsNullOrWhiteSpace(installationIdStr) && Guid.TryParse(installationIdStr, out var instGuid))
                {
                    cmd.Parameters.AddWithValue("installation_id", NpgsqlTypes.NpgsqlDbType.Uuid, instGuid);
                }
                else
                {
                    cmd.Parameters.AddWithValue("installation_id", DBNull.Value);
                }

                cmd.ExecuteNonQuery();

                // Write succeeded — mark healthy
                try { _setHealth?.Invoke(true); } catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                // Swallow exceptions to avoid breaking the logging pipeline,
                // but mark the sink as unhealthy and surface diagnostics via SelfLog.
                try
                {
                    _setHealth?.Invoke(false);
                }
                catch { /* ignore */ }

                try
                {
                    SelfLog.WriteLine("PostgresSink.Emit failed: {0}", ex.Message);
                }
                catch { /* ignore SelfLog failures */ }
            }
        }

        Dictionary<string, string> PropertiesToDictionary(LogEvent evt)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in evt.Properties)
            {
                // For simplicity serialize the rendered form of each property value.
                try
                {
                    dict[kv.Key] = kv.Value.ToString() ?? string.Empty;
                }
                catch
                {
                    dict[kv.Key] = string.Empty;
                }
            }
            return dict;
        }

        // Synchronous table creation call (kept for reuse)
        void EnsureTable()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            // Safe because table name is validated as a simple identifier above
            cmd.CommandText = $@"
                CREATE TABLE IF NOT EXISTS ""{_tableName}"" (
                    id bigserial PRIMARY KEY,
                    timestamp timestamptz NOT NULL,
                    level text NOT NULL,
                    message text,
                    exception text,
                    properties jsonb,
                    installation_id uuid NULL
                );";
            cmd.ExecuteNonQuery();
        }

        // Background retry loop for EnsureTable so ctor doesn't block or fail when DB/network is unavailable.
        async Task EnsureTableWithRetryAsync(CancellationToken token)
        {
            var delay = TimeSpan.FromSeconds(5);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    EnsureTable();
                    // success, mark healthy and stop retrying
                    try { _setHealth?.Invoke(true); } catch { }
                    try { SelfLog.WriteLine("PostgresSink: EnsureTable succeeded for table {0}", _tableName); } catch { }
                    return;
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    try { _setHealth?.Invoke(false); } catch { }
                    try { SelfLog.WriteLine("PostgresSink: EnsureTable failed: {0}. Retrying in {1}s", ex.Message, delay.TotalSeconds); } catch { }
                    try
                    {
                        await Task.Delay(delay, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { return; }
                    // cap backoff to avoid extremely long delays; simple exponential up to 1 minute
                    delay = TimeSpan.FromSeconds(Math.Min(60, delay.TotalSeconds * 2));
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _cts.Cancel();
                if (_ensureTask != null)
                {
                    try { _ensureTask.Wait(TimeSpan.FromSeconds(2)); } catch { }
                }
            }
            catch { /* swallow */ }
            finally
            {
                _cts.Dispose();
            }
        }
    }

    #endregion
}