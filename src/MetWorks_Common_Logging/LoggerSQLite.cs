namespace MetWorks.Common.Logging;

using Logger = Serilog.Core.Logger;
using ILogEventSink = Serilog.Core.ILogEventSink;

/// <summary>
/// Logger that writes to SQLite using a lightweight custom Serilog sink (no third-party Serilog sink).
/// - Initialization succeeds even if DB/file is unavailable.
/// - Sink exposes a health flag (IsHealthy) that reflects current ability to write to SQLite.
/// - Table creation runs in background with retry; writes are best-effort and will resume automatically when storage returns.
/// </summary>
public sealed class LoggerSQLite : ILoggerSQLite
{
    public Task<bool> InitializeAsync(
        ILoggerFile iLoggerFile,
        ISettingRepository iSettingRepository,
        IInstanceIdentifier iInstanceIdentifier,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(iLoggerFile);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);

        if (_isInitialized)
            throw new InvalidOperationException($"{nameof(LoggerSQLite)} is already initialized.");

        if (cancellationToken.IsCancellationRequested) return Task.FromResult(false);

        try
        {
            var dbPath = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.LoggerSQLiteGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerSQLite_dbPath
                )
            );

            var appDataDir = new DefaultPlatformPaths().AppDataDirectory;
            var resolvedDbPath = Path.IsPathRooted(dbPath)
                ? dbPath
                : Path.Combine(appDataDir, dbPath);

            var tableName = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.LoggerSQLiteGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerSQLite_tableName
                )
            );

            var minimumLevel = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.LoggerSQLiteGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerSQLite_minimumLevel
                )
            );

            var autoCreateTable = iSettingRepository.GetValueOrDefault<bool>(
                LookupDictionaries.LoggerSQLiteGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerSQLite_autoCreateTable
                )
            );

            _dbPath = resolvedDbPath;
            _tableName = tableName;

            var loggerCfg = new LoggerConfiguration()
                .MinimumLevel.Is(ParseLevel(minimumLevel));

            try
            {
                var installationId = iInstanceIdentifier?.GetOrCreateInstallationId();
                if (!string.IsNullOrWhiteSpace(installationId))
                    loggerCfg = loggerCfg.Enrich.WithProperty("InstallationId", installationId);
            }
            catch
            {
            }

            _iLogger = loggerCfg
                .WriteTo.Sink(new SqliteSink(_dbPath, _tableName, autoCreateTable, SetHealth))
                .CreateLogger();

            _isInitialized = true;
            _iLogger.Information("SQLite logger initialized for table {TableName}", tableName);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Failed to read settings configuration for SQLite logger.", exception);
        }

        return Task.FromResult(true);
    }

    bool _isInitialized = false;
    Logger? _iLogger = null;
    Logger ILogger => NullPropertyGuard.Get(_isInitialized, _iLogger, nameof(ILogger));

    string? _dbPath;
    public string DbPath => NullPropertyGuard.Get(_isInitialized, _dbPath, nameof(DbPath));

    string? _tableName;
    public string TableName => NullPropertyGuard.Get(_isInitialized, _tableName, nameof(TableName));

    int _isHealthy = 1;

    public bool IsHealthy => Interlocked.CompareExchange(ref _isHealthy, 1, 1) == 1;

    void SetHealth(bool healthy) => Interlocked.Exchange(ref _isHealthy, healthy ? 1 : 0);

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

    public ILogger ForContext(string contextName, object? value)
    {
        if (string.IsNullOrWhiteSpace(contextName)) return this;
        return new LoggerSQLiteContext(this, contextName, value);
    }

    public ILogger ForContext(Type sourceType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        return new LoggerSQLiteContext(this, sourceType);
    }

    sealed class LoggerSQLiteContext : ILogger
    {
        readonly LoggerSQLite _owner;
        readonly Serilog.ILogger _logger;

        public LoggerSQLiteContext(LoggerSQLite owner, string contextName, object? value)
        {
            ArgumentNullException.ThrowIfNull(owner);
            _owner = owner;
            _logger = owner.ILogger.ForContext(contextName, value, destructureObjects: true);
        }

        public LoggerSQLiteContext(LoggerSQLite owner, Type sourceType)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(sourceType);
            _owner = owner;
            _logger = owner.ILogger.ForContext(sourceType);
        }

        public void Information(string message) => _logger.Information(message);
        public void Warning(string message) => _logger.Warning(message);
        public void Error(string message, Exception exception) => _logger.Error(exception, message);
        public void Error(string message) => _logger.Error(message);
        public void Debug(string message) => _logger.Debug(message);
        public void Trace(string message) => _logger.Verbose(message);

        public Exception LogExceptionAndReturn(Exception exception) => _owner.LogExceptionAndReturn(exception);

        public Exception LogExceptionAndReturn(Exception exception, string message) => _owner.LogExceptionAndReturn(exception, message);

        public ILogger ForContext(string contextName, object? value)
        {
            if (string.IsNullOrWhiteSpace(contextName)) return this;
            return new LoggerSQLiteContextLogger(_owner, _logger.ForContext(contextName, value, destructureObjects: true));
        }

        public ILogger ForContext(Type sourceType)
        {
            ArgumentNullException.ThrowIfNull(sourceType);
            return new LoggerSQLiteContextLogger(_owner, _logger.ForContext(sourceType));
        }

        sealed class LoggerSQLiteContextLogger : ILogger
        {
            readonly LoggerSQLite _owner;
            readonly Serilog.ILogger _logger;

            public LoggerSQLiteContextLogger(LoggerSQLite owner, Serilog.ILogger logger)
            {
                ArgumentNullException.ThrowIfNull(owner);
                ArgumentNullException.ThrowIfNull(logger);
                _owner = owner;
                _logger = logger;
            }

            public void Information(string message) => _logger.Information(message);
            public void Warning(string message) => _logger.Warning(message);
            public void Error(string message, Exception exception) => _logger.Error(exception, message);
            public void Error(string message) => _logger.Error(message);
            public void Debug(string message) => _logger.Debug(message);
            public void Trace(string message) => _logger.Verbose(message);

            public Exception LogExceptionAndReturn(Exception exception) => _owner.LogExceptionAndReturn(exception);

            public Exception LogExceptionAndReturn(Exception exception, string message) => _owner.LogExceptionAndReturn(exception, message);

            public ILogger ForContext(string contextName, object? value)
            {
                if (string.IsNullOrWhiteSpace(contextName)) return this;
                return new LoggerSQLiteContextLogger(_owner, _logger.ForContext(contextName, value, destructureObjects: true));
            }

            public ILogger ForContext(Type sourceType)
            {
                ArgumentNullException.ThrowIfNull(sourceType);
                return new LoggerSQLiteContextLogger(_owner, _logger.ForContext(sourceType));
            }
        }
    }

    static LogEventLevel ParseLevel(string level) =>
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

    #region Lightweight SQLite sink

    sealed class SqliteSink : ILogEventSink, IDisposable
    {
        readonly string _dbPath;
        readonly string _tableName;
        readonly bool _autoCreateTable;
        readonly Action<bool>? _setHealth;
        readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = false };

        // Background ensure task cancellation
        readonly CancellationTokenSource _cts = new();
        Task? _ensureTask;

        static readonly Regex ValidIdentifier = new(@"^[A-Za-z0-9_]+$");

        public SqliteSink(string dbPath, string tableName, bool autoCreateTable, Action<bool>? setHealth = null)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentException("dbPath is required.", nameof(dbPath));

            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName is required.", nameof(tableName));

            if (!ValidIdentifier.IsMatch(tableName))
                throw new ArgumentException("Table name contains invalid characters. Only letters, digits and underscore are allowed.", nameof(tableName));

            _dbPath = dbPath;
            _tableName = tableName;
            _autoCreateTable = autoCreateTable;
            _setHealth = setHealth;

            if (_autoCreateTable)
            {
                _ensureTask = Task.Run(() => EnsureTableWithRetryAsync(_cts.Token));
            }
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                using var conn = new SqliteConnection(BuildConnectionString(_dbPath));
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
INSERT INTO ""{_tableName}"" (timestamp_utc, level, message, exception, properties, installation_id)
VALUES ($ts, $level, $message, $exception, json($properties), $installation_id);";

                cmd.Parameters.AddWithValue("$ts", logEvent.Timestamp.UtcDateTime.ToString("O"));
                cmd.Parameters.AddWithValue("$level", logEvent.Level.ToString());

                var rendered = logEvent.RenderMessage();
                cmd.Parameters.AddWithValue("$message", rendered ?? string.Empty);
                cmd.Parameters.AddWithValue("$exception", logEvent.Exception?.ToString() ?? (object)DBNull.Value);

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
                cmd.Parameters.AddWithValue("$properties", propsJson);

                if (!string.IsNullOrWhiteSpace(installationIdStr) && Guid.TryParse(installationIdStr, out var instGuid))
                {
                    cmd.Parameters.AddWithValue("$installation_id", instGuid.ToString());
                }
                else
                {
                    cmd.Parameters.AddWithValue("$installation_id", DBNull.Value);
                }

                cmd.ExecuteNonQuery();

                try { _setHealth?.Invoke(true); } catch { }
            }
            catch (Exception ex)
            {
                try { _setHealth?.Invoke(false); } catch { }
                try { SelfLog.WriteLine("SqliteSink.Emit failed: {0}", ex.Message); } catch { }
            }
        }

        Dictionary<string, string> PropertiesToDictionary(LogEvent evt)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in evt.Properties)
            {
                try { dict[kv.Key] = kv.Value.ToString() ?? string.Empty; }
                catch { dict[kv.Key] = string.Empty; }
            }
            return dict;
        }

        void EnsureTable()
        {
            using var conn = new SqliteConnection(BuildConnectionString(_dbPath));
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
CREATE TABLE IF NOT EXISTS ""{_tableName}"" (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp_utc TEXT NOT NULL,
    level TEXT NOT NULL,
    message TEXT,
    exception TEXT,
    properties TEXT,
    installation_id TEXT NULL
);
CREATE INDEX IF NOT EXISTS idx_{_tableName}_timestamp_utc ON ""{_tableName}""(timestamp_utc);
CREATE INDEX IF NOT EXISTS idx_{_tableName}_installation_id ON ""{_tableName}""(installation_id);
";
            cmd.ExecuteNonQuery();
        }

        async Task EnsureTableWithRetryAsync(CancellationToken token)
        {
            var delay = TimeSpan.FromSeconds(5);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    EnsureTable();
                    try { _setHealth?.Invoke(true); } catch { }
                    try { SelfLog.WriteLine("SqliteSink: EnsureTable succeeded for table {0}", _tableName); } catch { }
                    return;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    try { _setHealth?.Invoke(false); } catch { }
                    try { SelfLog.WriteLine("SqliteSink: EnsureTable failed: {0}. Retrying in {1}s", ex.Message, delay.TotalSeconds); } catch { }

                    try { await Task.Delay(delay, token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { return; }

                    delay = TimeSpan.FromSeconds(Math.Min(60, delay.TotalSeconds * 2));
                }
            }
        }

        static string BuildConnectionString(string dbPath)
        {
            var appDataDir = new DefaultPlatformPaths().AppDataDirectory;
            var resolvedDbPath = Path.IsPathRooted(dbPath)
                ? dbPath
                : Path.Combine(appDataDir, dbPath);

            return new SqliteConnectionStringBuilder
            {
                DataSource = resolvedDbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
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
            catch { }
            finally
            {
                _cts.Dispose();
            }
        }
    }

    #endregion
}
