namespace MetWorks.Ingest.SQLite;
/// <summary>
/// SQLite listener sink with robust lifecycle and cooperative cancellation support.
/// </summary>
public sealed class RawPacketIngestor : ServiceBase
{
    bool _isDatabaseAvailable = false;
    int _isInitializing = 0;
    DateTime _lastConnectionAttempt = DateTime.MinValue;
    DateTime _lastSuccessfulWrite = DateTime.MinValue;
    int _failureCount = 0;

    const int MaxConsecutiveFailures = 5;
    const int ReconnectionIntervalSeconds = 30;

    ConcurrentQueue<IRawPacketRecordTyped>? _messageBuffer;
    const int MaxBufferSize = 1000;
    bool _bufferingEnabled = false;

    Timer? _healthCheckTimer;
    Timer? _reconnectionTimer;

    string _connectionString = string.Empty;
    string _dbPath = string.Empty;

    IInstanceIdentifier? _instanceIdentifier;
    Guid _installationIdGuid;

    public RawPacketIngestor()
    {
    }

    public async Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null
    )
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);

        iLoggerResilient.Information($"RawPacketIngestor(SQLite).InitializeAsync() starting - thread={Environment.CurrentManagedThreadId}");

        try
        {
            InitializeBase(
                iLoggerResilient.ForContext(GetType()),
                iSettingRepository,
                iEventRelayBasic,
                externalCancellation,
                provenanceTracker
            );

            _bufferingEnabled = iSettingRepository.GetValueOrDefault<bool>(
                LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_enableBuffering)
            );

            if (_bufferingEnabled)
            {
                _messageBuffer = new ConcurrentQueue<IRawPacketRecordTyped>();
                ILogger.Information("📦 Message buffering enabled for SQLite listener");
            }

            _connectionString = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_connectionString)
            );

            _dbPath = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_dbPath)
            );

            _instanceIdentifier = iInstanceIdentifier;
            var iid = _instanceIdentifier.GetOrCreateInstallationId();
            if (!Guid.TryParse(iid, out _installationIdGuid))
            {
                ILogger.Error($"Installation id '{iid}' is not a valid GUID. Aborting initialization.");
                return false;
            }

            // If a full connection string isn't provided, build a sensible default using the dbPath.
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                if (string.IsNullOrWhiteSpace(_dbPath))
                {
                    ILogger.Warning("⚠️ SQLite dbPath not configured. Running in degraded mode.");
                    await StartDegradedAsync().ConfigureAwait(false);
                    return true;
                }

                var resolvedDbPath = Path.IsPathRooted(_dbPath)
                    ? _dbPath
                    : Path.Combine(AppContext.BaseDirectory, _dbPath);

                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = resolvedDbPath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Cache = SqliteCacheMode.Shared
                };

                _connectionString = builder.ToString();
            }

            var connected = await TryEstablishConnectionAsync(_connectionString).ConfigureAwait(false);
            if (!connected)
            {
                ILogger.Error("❌ SQLite initial connection failed during initialization. Aborting SQLite listener startup.");
                return false;
            }

            await StartAsync().ConfigureAwait(false);

            try { MarkReady(); } catch { }
            return true;
        }
        catch (Exception exception)
        {
            iLoggerResilient.Error($"❌ Error during SQLite listener initialization: {exception.Message}");
            iLoggerResilient.Warning("⚠️ Starting SQLite listener in degraded mode");
            await StartDegradedAsync().ConfigureAwait(false);
            return true;
        }
    }

    static async Task<SqliteConnection> CreateOpenConnectionAsync(
        string connectionString,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            return conn;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Failed to create and open SQLite connection asynchronously.", exception);
        }
    }

    async Task<bool> TryEstablishConnectionAsync(string connectionString)
    {
        if (LinkedCancellationToken.IsCancellationRequested)
        {
            ILogger.Warning("⚠️ Connection attempt cancelled by external shutdown");
            return false;
        }

        if (Interlocked.CompareExchange(ref _isInitializing, 1, 0) != 0)
        {
            ILogger.Debug("⏳ Connection attempt already in progress, skipping");
            return false;
        }

        _lastConnectionAttempt = DateTime.UtcNow;

        try
        {
            using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedTestCts = CancellationTokenSource.CreateLinkedTokenSource(testCts.Token, LinkedCancellationToken);

            await using var testConnection = await CreateOpenConnectionAsync(connectionString, linkedTestCts.Token).ConfigureAwait(false);

            // Reduce contention for concurrent readers/writers.
            await using (var pragmaCmd = testConnection.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                _ = await pragmaCmd.ExecuteNonQueryAsync(linkedTestCts.Token).ConfigureAwait(false);
            }

            // Make sure JSON1 functions are available.
            await using (var jsonProbe = testConnection.CreateCommand())
            {
                jsonProbe.CommandText = "SELECT json_extract('{\"a\":1}', '$.a');";
                _ = await jsonProbe.ExecuteScalarAsync(linkedTestCts.Token).ConfigureAwait(false);
            }

            // Double check generated columns are supported at runtime (Android SQLite build can differ).
            try
            {
                var supportsGenerated = await SqliteFeatureProbe.SupportsGeneratedColumnsAsync(testConnection, linkedTestCts.Token).ConfigureAwait(false);
                ILogger.Information($"🧪 SQLite feature probe: generated columns supported={supportsGenerated}");
            }
            catch (Exception exProbe)
            {
                ILogger.Error($"❌ SQLite generated columns probe failed: {exProbe.Message}");
                return false;
            }

            try
            {
                using var initCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var linkedInitCts = CancellationTokenSource.CreateLinkedTokenSource(initCts.Token, LinkedCancellationToken);

                await using var schemaConnection = await CreateOpenConnectionAsync(connectionString, linkedInitCts.Token).ConfigureAwait(false);
                await Initializer.DatabaseInitializeAsync(ILogger, schemaConnection, linkedInitCts.Token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                ILogger.Error("❌ Database initialization failed.", exception);
                return false;
            }

            _isDatabaseAvailable = true;
            _failureCount = 0;
            _lastSuccessfulWrite = DateTime.UtcNow;

            if (_bufferingEnabled && _messageBuffer is not null && !_messageBuffer.IsEmpty)
            {
                StartBackground(_ => ProcessBufferedMessagesAsync());
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            _isDatabaseAvailable = false;
            ILogger.Warning("⚠️ Connection attempt cancelled by external shutdown");
            return false;
        }
        catch (SqliteException sqliteException)
        {
            _isDatabaseAvailable = false;
            _lastConnectionAttempt = DateTime.UtcNow;
            _failureCount++;
            ILogger.Warning($"⚠️ Failed to establish SQLite connection: {sqliteException.Message} (ErrorCode: {sqliteException.SqliteErrorCode})");
            return false;
        }
        catch (Exception exception)
        {
            _isDatabaseAvailable = false;
            _lastConnectionAttempt = DateTime.UtcNow;
            _failureCount++;
            ILogger.Warning($"⚠️ Failed to establish SQLite connection: {exception.Message}");
            return false;
        }
        finally
        {
            Interlocked.Decrement(ref _isInitializing);
        }
    }

    async Task StartDegradedAsync()
    {
        ILogger.Warning("🔶 SQLite listener running in DEGRADED MODE");
        IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);
        StartReconnectionTimer();
        StartHealthMonitoring();
        await Task.CompletedTask;
    }

    async Task<bool> StartAsync()
    {
        ILogger.Information("✅ SQLite Listener started in ACTIVE mode");
        IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);
        StartHealthMonitoring();
        StartReconnectionTimer();
        return await Task.FromResult(true);
    }

    void StartHealthMonitoring()
    {
        if (_healthCheckTimer is not null) return;

        _healthCheckTimer = new Timer(
            HealthCheckCallback,
            null,
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60)
        );

        ILogger.Information("🏥 Health monitoring started for SQLite listener");
    }

    void StartReconnectionTimer()
    {
        if (_reconnectionTimer is not null) return;

        _reconnectionTimer = new Timer(
            ReconnectionCallback,
            null,
            TimeSpan.FromSeconds(ReconnectionIntervalSeconds),
            TimeSpan.FromSeconds(ReconnectionIntervalSeconds)
        );

        ILogger.Information("🔄 Reconnection timer started for SQLite listener");
    }

    void HealthCheckCallback(object? state)
    {
        try
        {
            if (LinkedCancellationToken.IsCancellationRequested) return;

            var status = _isDatabaseAvailable ? "ACTIVE" : "DEGRADED";
            var timeSinceLastWrite = DateTime.UtcNow - _lastSuccessfulWrite;
            var bufferSize = _messageBuffer?.Count ?? 0;

            if (_isDatabaseAvailable)
            {
                if (timeSinceLastWrite > TimeSpan.FromMinutes(5) && _lastSuccessfulWrite != DateTime.MinValue)
                {
                    ILogger.Warning($"⚠️ SQLite [{status}] No writes in {timeSinceLastWrite.TotalMinutes:F1} minutes. Buffer: {bufferSize} messages");
                }
                else
                {
                    ILogger.Debug($"💚 SQLite [{status}] Healthy - Last write: {timeSinceLastWrite.TotalSeconds:F0}s ago. Failures: {_failureCount}");
                }
            }
            else
            {
                var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
                ILogger.Warning($"🔶 SQLite [{status}] Unavailable - Last attempt: {timeSinceLastAttempt.TotalSeconds:F0}s ago. Buffer: {bufferSize} messages. Failures: {_failureCount}");
            }

            if (_failureCount >= MaxConsecutiveFailures && _isDatabaseAvailable)
            {
                ILogger.Error($"❌ SQLite has {_failureCount} consecutive failures. Marking as UNAVAILABLE.");
                _isDatabaseAvailable = false;
            }
        }
        catch (Exception exception)
        {
            ILogger.Error($"❌ Error in health check: {exception.Message}");
        }
    }

    void ReconnectionCallback(object? state)
    {
        if (_isDatabaseAvailable) return;
        if (LinkedCancellationToken.IsCancellationRequested) return;

        var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
        if (timeSinceLastAttempt < TimeSpan.FromSeconds(ReconnectionIntervalSeconds - 5))
            return;

        StartBackground(async token =>
        {
            try
            {
                if (token.IsCancellationRequested) return;
                if (string.IsNullOrWhiteSpace(_connectionString)) return;

                var connected = await TryEstablishConnectionAsync(_connectionString).ConfigureAwait(false);
                if (connected)
                    ILogger.Information("✅ SQLite reconnection SUCCESSFUL! Resuming normal operation.");
                else
                    ILogger.Warning("❌ SQLite reconnection failed. Will retry in 30 seconds.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                ILogger.Information("🔌 Reconnection task cancelled by external shutdown");
            }
            catch (Exception ex)
            {
                ILogger.Warning($"⚠️ Reconnection task failed: {ex.Message}");
            }
        });
    }

    void ReceiveHandler(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        StartBackground(_ => ProcessMessage(iRawPacketRecordTyped));
    }

    async Task ProcessMessage(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        if (!_isDatabaseAvailable && _bufferingEnabled && _messageBuffer is not null)
        {
            if (_messageBuffer.Count < MaxBufferSize)
            {
                _messageBuffer.Enqueue(iRawPacketRecordTyped);
                return;
            }

            return;
        }

        if (!_isDatabaseAvailable)
            return;

        await WriteToDatabase(iRawPacketRecordTyped).ConfigureAwait(false);
    }

    async Task WriteToDatabase(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        var tableString = UdpPacketTableData.PacketTableDataMap[iRawPacketRecordTyped.PacketEnum].TableName;

        var sql = $"INSERT INTO \"{tableString}\" (id, json_document_original, application_received_utc_timestampz, installation_id) "
            + "VALUES ($id, $json_document_original, $application_received_utc_timestampz, $installation_id);";

        try
        {
            await using var conn = await CreateOpenConnectionAsync(_connectionString, LinkedCancellationToken).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            cmd.Parameters.AddWithValue("$id", iRawPacketRecordTyped.Id);
            cmd.Parameters.AddWithValue("$json_document_original", iRawPacketRecordTyped.RawPacketJson);
            cmd.Parameters.AddWithValue("$application_received_utc_timestampz", iRawPacketRecordTyped.ReceivedUtcUnixEpochSecondsAsLong);
            cmd.Parameters.AddWithValue("$installation_id", _installationIdGuid.ToString());

            var rowsAffected = await cmd.ExecuteNonQueryAsync(LinkedCancellationToken).ConfigureAwait(false);

            _lastSuccessfulWrite = DateTime.UtcNow;
            _failureCount = 0;

            ProvenanceTracker?.LinkDatabaseRecord(iRawPacketRecordTyped.Id, iRawPacketRecordTyped.Id);

            ILogger.Information($"✅ Wrote to SQLite '{tableString}' - {iRawPacketRecordTyped.PacketEnum} id '{iRawPacketRecordTyped.Id}' ({rowsAffected} row)");
        }
        catch (OperationCanceledException) when (LinkedCancellationToken.IsCancellationRequested)
        {
            _failureCount++;
            _lastConnectionAttempt = DateTime.UtcNow;

            if (_bufferingEnabled && _messageBuffer is not null && _messageBuffer.Count < MaxBufferSize)
                _messageBuffer.Enqueue(iRawPacketRecordTyped);
        }
        catch (SqliteException sqliteException)
        {
            _failureCount++;
            _lastConnectionAttempt = DateTime.UtcNow;

            ILogger.Error($"❌ SQLite error writing to '{tableString}' (failure #{_failureCount}): {sqliteException.Message} (ErrorCode: {sqliteException.SqliteErrorCode})");

            ProvenanceTracker?.RecordError(
                iRawPacketRecordTyped.Id,
                "ListenerSink",
                "Database Write",
                sqliteException);

            // On many SQLITE_BUSY/IO errors, treat as transient but mark unavailable so reconnection loop can re-init.
            _isDatabaseAvailable = false;

            if (_bufferingEnabled && _messageBuffer is not null && _messageBuffer.Count < MaxBufferSize)
                _messageBuffer.Enqueue(iRawPacketRecordTyped);
        }
        catch (Exception exception)
        {
            _failureCount++;
            _lastConnectionAttempt = DateTime.UtcNow;

            ProvenanceTracker?.RecordError(
                iRawPacketRecordTyped.Id,
                "ListenerSink",
                "Database Write",
                exception);

            _isDatabaseAvailable = false;

            if (_bufferingEnabled && _messageBuffer is not null && _messageBuffer.Count < MaxBufferSize)
                _messageBuffer.Enqueue(iRawPacketRecordTyped);
        }
    }

    async Task ProcessBufferedMessagesAsync()
    {
        if (_messageBuffer == null || _messageBuffer.IsEmpty)
            return;

        int processed = 0;
        int failed = 0;

        while (_messageBuffer.TryDequeue(out var message) && _isDatabaseAvailable && !LinkedCancellationToken.IsCancellationRequested)
        {
            try
            {
                await WriteToDatabase(message).ConfigureAwait(false);
                processed++;

                if (processed % 10 == 0)
                {
                    try { await Task.Delay(100, LinkedCancellationToken).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                }
            }
            catch (OperationCanceledException) when (LinkedCancellationToken.IsCancellationRequested)
            {
                _messageBuffer.Enqueue(message);
                break;
            }
            catch (Exception)
            {
                failed++;

                if (failed >= 5)
                {
                    _messageBuffer.Enqueue(message);
                    _isDatabaseAvailable = false;
                    break;
                }
            }
        }

        ILogger.Information($"✅ Buffer processing complete. Processed: {processed}, Failed: {failed}, Remaining: {_messageBuffer.Count}");
    }

    protected override async Task OnDisposeAsync()
    {
        try
        {
            try { _healthCheckTimer?.Dispose(); } catch { }
            _healthCheckTimer = null;
            try { _reconnectionTimer?.Dispose(); } catch { }
            _reconnectionTimer = null;

            try
            {
                IEventRelayBasic.Unregister<IRawPacketRecordTyped>(this);
            }
            catch { }

            try { ILogger.Information("🧹 SQLite listener disposed"); } catch { }
        }
        catch { }

        await Task.CompletedTask;
    }
}
