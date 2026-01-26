 /// <summary>
/// PostgreSQL listener sink with robust lifecycle and cooperative cancellation support.
/// </summary>
namespace MetWorks.Ingest.Postgres;
public class RawPacketIngestor : ServiceBase
{
    static Dictionary<PacketEnum, string> PacketToTableDictionary = new()
    {
        { PacketEnum.Lightning, "lightning" },
        { PacketEnum.Observation, "observation" },
        { PacketEnum.Precipitation, "precipitation" },
        { PacketEnum.Wind, "wind" }
    };
    // Connection state tracking
    bool _isDatabaseAvailable = false;
    int _isInitializing = 0;
    DateTime _lastConnectionAttempt = DateTime.MinValue;
    DateTime _lastSuccessfulWrite = DateTime.MinValue;
    int _failureCount = 0;
    const int MaxConsecutiveFailures = 5;
    const int ReconnectionIntervalSeconds = 30;
    // Message buffering (optional)
    ConcurrentQueue<IRawPacketRecordTyped>? _messageBuffer;
    const int MaxBufferSize = 1000;
    bool _bufferingEnabled = false;
    // Health monitoring
    Timer? _healthCheckTimer;
    Timer? _reconnectionTimer;
    string _connectionString = string.Empty;
    IInstanceIdentifier? _instanceIdentifier;
    Guid _installationIdGuid; // cached parsed installation id for DB writes
    // NOTE: PostgresConnectionFactory has been inlined into this class.
    // PostgresConnectionFactory remains in the repo for now but is unused.
    public RawPacketIngestor()
    {
    }

    // No schema pre-checks. Any Postgres action that fails will be treated as a fatal initialization error.
    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null
    )
    {
        iLogger.Information($"RawPacketIngestor.InitializeAsync() starting - thread={Environment.CurrentManagedThreadId}");
        try
        {
            // Use ServiceBase helper to wire logger and linked cancellation
            InitializeBase(
                iLogger,
                iSettingRepository,
                iEventRelayBasic,
                externalCancellation,
                provenanceTracker
            );

            iLogger.Information($"🔍 Provenance tracking {(HaveProvenanceTracker ? string.Empty : "NOT")}enabled for PostgreSQL listener");

            _bufferingEnabled = iSettingRepository.GetValueOrDefault<bool>(
                LookupDictionaries.XMLToPostgreSQLGroupSettingsDefinition.BuildSettingPath(SettingConstants.XMLToPostgreSQL_enableBuffering)
            );

            if (_bufferingEnabled)
            {
                _messageBuffer = new ConcurrentQueue<IRawPacketRecordTyped>();
                iLogger.Information("📦 Message buffering enabled for PostgreSQL listener");
            }

            _connectionString = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.XMLToPostgreSQLGroupSettingsDefinition.BuildSettingPath(SettingConstants.XMLToPostgreSQL_connectionString)
            );

            // Store instance id for DB writes and cache parsed GUID (installation id should be stable)
            _instanceIdentifier = iInstanceIdentifier;
            if (_instanceIdentifier is null)
            {
                ILogger.Error("InstanceIdentifier is required for Postgres writes but was not provided. Aborting initialization.");
                return false;
            }
            var iid = _instanceIdentifier.GetOrCreateInstallationId();
            if (!Guid.TryParse(iid, out _installationIdGuid))
            {
                ILogger.Error($"Installation id '{iid}' is not a valid GUID. Aborting initialization.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                iLogger.Warning("⚠️ PostgreSQL connection string not configured. Running in degraded mode.");
                await StartDegradedAsync();
                return true; // Return true to allow app to continue
            }

            // Attempt initial connection
            iLogger.Information($"Calling TryEstablishConnectionAsync fom RawPacketIngestor.InitializeAsync() - thread={Environment.CurrentManagedThreadId}");
            var connected = await TryEstablishConnectionAsync(_connectionString).ConfigureAwait(false);
            iLogger.Information($"Back from calling TryEstablishConnectionAsync fom RawPacketIngestor.InitializeAsync() - thread={Environment.CurrentManagedThreadId}");

            // Fail fast: if any Postgres action fails during initialization, abort startup.
            if (!connected)
            {
                iLogger.Error("❌ PostgreSQL initial connection failed during initialization. Aborting Postgres listener startup.");
                return false;
            }

            iLogger.Information("✅ PostgreSQL listener initialized with active connection");
            await StartAsync().ConfigureAwait(false);

            iLogger.Information($"RawPacketIngestor.InitializeAsync() completed, returning  - thread={Environment.CurrentManagedThreadId}");
            return true; // Always return true - we handle failures gracefully
        }
        catch (Exception exception)
        {
            iLogger.Error($"❌ Error during PostgreSQL listener initialization: {exception.Message}");
            iLogger.Warning("⚠️ Starting PostgreSQL listener in degraded mode");
            await StartDegradedAsync().ConfigureAwait(false);
            return true; // Don't throw - allow app to continue
        }
    }
    async Task<NpgsqlConnection> CreateOpenConnectionAsync(
        string connectionString,
        CancellationToken cancellationToken,
        bool pooling = true
    )
    {
        try
        {
            var csb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);

            // Bound connection establishment and command execution even if cancellation isn't honored promptly.
            // Timeout: connect timeout (seconds). CommandTimeout: default command timeout (seconds).
            if (csb.Timeout <= 0 || csb.Timeout > 10)
                csb.Timeout = 5;

            if (csb.CommandTimeout <= 0 || csb.CommandTimeout > 30)
                csb.CommandTimeout = 5;

            csb.Pooling = pooling;

            var npgsqlConnection = new NpgsqlConnection(csb.ConnectionString);

            ILogger.Information($"🔌 PostgreSQL OpenAsync starting (Timeout={csb.Timeout}s, CommandTimeout={csb.CommandTimeout}s, Pooling={csb.Pooling}) - thread={Environment.CurrentManagedThreadId}");
            await npgsqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            ILogger.Information($"🔌 PostgreSQL OpenAsync completed (State={npgsqlConnection.State}) - thread={Environment.CurrentManagedThreadId}");
            return npgsqlConnection;
        }
        catch (OperationCanceledException operationCanceledException)
        {
            ILogger.Warning($"⚠️ PostgreSQL connection opening was canceled: {operationCanceledException.Message}");
            throw;
        }
        catch (Exception exception)
        {
            throw ILogger.LogExceptionAndReturn(
                new InvalidOperationException(
                    "Failed to create and open PostgreSQL connection asynchronously.", exception));
        }
    }
    async Task<bool> TryEstablishConnectionAsync(string connectionString)
    {
        ILogger.Information($"TryEstablishConnectionAsync starting - thread={Environment.CurrentManagedThreadId}");
        // Respect external cancellation before starting
        if (LinkedCancellationToken.IsCancellationRequested)
        {
            ILogger.Warning("⚠️ Connection attempt cancelled by external shutdown");
            return false;
        }

        // Prevent overlapping connection attempts (thread-safe)
        if (Interlocked.CompareExchange(ref _isInitializing, 1, 0) != 0)
        {
            ILogger.Debug("⏳ Connection attempt already in progress, skipping");
            return false;
        }

        _lastConnectionAttempt = DateTime.UtcNow;

        try
        {
            ILogger.Information("🔌 Attempting to establish PostgreSQL connection...");
            // Test the connection using short timeouts
            using (var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5))
            )
            {
                using (var linkedTestCts = CancellationTokenSource.CreateLinkedTokenSource(testCts.Token, LinkedCancellationToken)
                )
                {
                    ILogger.Information($"Calling CreateOpenConnectionAsync from TryEstablishConnectionAsync - thread={Environment.CurrentManagedThreadId}");
                    var testConnection = await CreateOpenConnectionAsync(connectionString, linkedTestCts.Token, pooling: false)
                        .ConfigureAwait(false);
                    try
                    {
                        ILogger.Information($"In using after CreateOpenConnectionAsync call - thread={Environment.CurrentManagedThreadId}");
                        await using var versionCommand = new NpgsqlCommand("SELECT version()", testConnection)
                        {
                            CommandTimeout = 5
                        };

                        ILogger.Information("🧪 ExecuteScalarAsync starting: SELECT version() (CommandTimeout=5)");
                        var version = await versionCommand.ExecuteScalarAsync(linkedTestCts.Token).ConfigureAwait(false);
                        ILogger.Information("🧪 ExecuteScalarAsync completed: SELECT version()");

                        if (version is not null)
                        {
                            var versionString = version.ToString() ?? "Unknown";
                            var versionShort = versionString.Split(' ').Take(2).ToArray();
                            ILogger.Information($"✅ Connected to {string.Join(" ", versionShort)}");
                        }

                        await using var testQuery = new NpgsqlCommand(
                            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'",
                            testConnection)
                        {
                            CommandTimeout = 5
                        };

                        ILogger.Information("🧪 ExecuteScalarAsync starting: information_schema.tables count (CommandTimeout=5)");
                        var tableCount = await testQuery.ExecuteScalarAsync(linkedTestCts.Token).ConfigureAwait(false);
                        ILogger.Information("🧪 ExecuteScalarAsync completed: information_schema.tables count");
                        ILogger.Debug($"📊 Found {tableCount} tables in public schema");
                    }
                    finally
                    {
                        testConnection.Dispose();
                    }
                 }
             }
             ILogger.Information("🧪 Probe connection using-block exited (testConnection disposed)");

            // Initialize database schema (no factory needed)
            try
            {
                ILogger.Information("🗄️ Initializing database schema...");
                using var initCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var linkedInitCts = CancellationTokenSource.CreateLinkedTokenSource(initCts.Token, LinkedCancellationToken);
                // Warm-check whether our tables support an installation_id column so writes can include it.
                // No schema pre-checks. If any Postgres action fails (including missing columns), initialization will fail.

                await using var schemaConnection = await CreateOpenConnectionAsync(connectionString, linkedInitCts.Token, pooling: false)
                    .ConfigureAwait(false);

                await PostgresInitializer.DatabaseInitializeAsync(
                    ILogger,
                    schemaConnection,
                    linkedInitCts.Token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // Fail initialization if DB schema/actions fail. Log full exception and return false.
                ILogger.Error("❌ Database initialization failed.", exception);
                return false;
            }

            _isDatabaseAvailable = true;
            _failureCount = 0;
            _lastSuccessfulWrite = DateTime.UtcNow;

            ILogger.Information("✅ PostgreSQL connection established successfully");

            // Process any buffered messages
                if (_bufferingEnabled && _messageBuffer is not null && !_messageBuffer.IsEmpty)
                {
                    var bufferedCount = _messageBuffer.Count;
                    ILogger.Information($"📦 Found {bufferedCount} buffered messages, starting processing...");
                    StartBackground(async token => await ProcessBufferedMessagesAsync().ConfigureAwait(false));
                }

            ILogger.Information($"Returning from TryEstablishConnectionAsync - thread={Environment.CurrentManagedThreadId}");

            return true;
        }
        catch (OperationCanceledException)
        {
            _isDatabaseAvailable = false;
            ILogger.Warning("⚠️ Connection attempt cancelled by external shutdown");
            return false;
        }
        catch (Npgsql.NpgsqlException npgsqlException)
        {
            _isDatabaseAvailable = false;
            _lastConnectionAttempt = DateTime.UtcNow;
            _failureCount++;

            var errorDetails = $"ErrorCode: {npgsqlException.ErrorCode}";
            if (!string.IsNullOrWhiteSpace(npgsqlException.SqlState))
                errorDetails += $", SqlState: {npgsqlException.SqlState}";

            ILogger.Warning($"⚠️ Failed to establish PostgreSQL connection: {npgsqlException.Message} ({errorDetails})");
            return false;
        }
        catch (TimeoutException timeoutException)
        {
            _isDatabaseAvailable = false;
            _lastConnectionAttempt = DateTime.UtcNow;
            _failureCount++;
            ILogger.Warning($"⚠️ Connection timeout: {timeoutException.Message}");
            return false;
        }
        catch (Exception exception)
        {
            _isDatabaseAvailable = false;
            _lastConnectionAttempt = DateTime.UtcNow;
            _failureCount++;
            ILogger.Warning($"⚠️ Failed to establish PostgreSQL connection: {exception.Message}");
            ILogger.Debug($"   Exception type: {exception.GetType().Name}");
            ILogger.Debug($"   Stack trace: {exception.StackTrace}");
            return false;
        }
        finally
        {
            Interlocked.Decrement(ref _isInitializing);
        }
    }

    async Task StartDegradedAsync()
    {
        ILogger.Warning("🔶 PostgreSQL listener running in DEGRADED MODE");
        ILogger.Information("🔄 Auto-reconnection enabled - will retry every 30 seconds");

        // Register event handler even in degraded mode
        IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);

        // Start reconnection attempts
        StartReconnectionTimer();

        // Start health monitoring
        StartHealthMonitoring();

        await Task.CompletedTask;
    }

    async Task<bool> StartAsync()
    {
        ILogger.Information("✅ PostgreSQL Listener started in ACTIVE mode");
        IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);

        // Start health monitoring
        StartHealthMonitoring();

        // Start reconnection monitoring (will only reconnect if connection fails)
        StartReconnectionTimer();

        return await Task.FromResult(true);
    }

    void StartHealthMonitoring()
    {
        if (_healthCheckTimer is not null) return; // Already started

        // Check health every 60 seconds; callbacks should respect linked cancellation
        _healthCheckTimer = new Timer(
            HealthCheckCallback,
            null,
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60)
        );

        ILogger.Information("🏥 Health monitoring started for PostgreSQL listener");
    }

    void StartReconnectionTimer()
    {
        if (_reconnectionTimer is not null)
            return; // Already started

        // Attempt reconnection every 30 seconds if database is unavailable
        _reconnectionTimer = new Timer(
            ReconnectionCallback,
            null,
            TimeSpan.FromSeconds(ReconnectionIntervalSeconds),
            TimeSpan.FromSeconds(ReconnectionIntervalSeconds));

        ILogger.Information("🔄 Reconnection timer started for PostgreSQL listener");
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
                    ILogger.Warning(
                        $"⚠️ PostgreSQL [{status}] No writes in {timeSinceLastWrite.TotalMinutes:F1} minutes. Buffer: {bufferSize} messages");
                }
                else
                {
                    ILogger.Debug(
                        $"💚 PostgreSQL [{status}] Healthy - Last write: {timeSinceLastWrite.TotalSeconds:F0}s ago. Failures: {_failureCount}");
                }
            }
            else
            {
                var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
                ILogger.Warning(
                    $"🔶 PostgreSQL [{status}] Unavailable - Last attempt: {timeSinceLastAttempt.TotalSeconds:F0}s ago.  Buffer: {bufferSize} messages. Failures: {_failureCount}");
            }

            if (_failureCount >= MaxConsecutiveFailures && _isDatabaseAvailable)
            {
                ILogger.Error(
                    $"❌ PostgreSQL has {_failureCount} consecutive failures. Marking as UNAVAILABLE.");
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
        // Only attempt reconnection if database is unavailable or not shutting down
        if (_isDatabaseAvailable) return;
        if (LinkedCancellationToken.IsCancellationRequested) return;

        var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
        if (timeSinceLastAttempt < TimeSpan.FromSeconds(ReconnectionIntervalSeconds - 5))
            return;

        ILogger.Information("🔄 Attempting automatic PostgreSQL reconnection...");

        // Use ServiceBase helper to run cancellable background work
        StartBackground(async token =>
        {
            try
            {
                if (token.IsCancellationRequested) return;

                var connectionString = ISettingRepository
                    .GetValueOrDefault(@"/services/RawPacketRecordTypedInPostgresOut/connectionString");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    ILogger.Warning("⚠️ Cannot reconnect - connection string not available");
                    return;
                }

                var connected = await TryEstablishConnectionAsync(connectionString);

                if (connected)
                    ILogger.Information("✅ PostgreSQL reconnection SUCCESSFUL! Resuming normal operation.");
                else
                    ILogger.Warning("❌ PostgreSQL reconnection failed. Will retry in 30 seconds.");
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
        StartBackground(async token => await ProcessMessage(iRawPacketRecordTyped));
    }

    async Task ProcessMessage(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        // If database is unavailable and buffering is enabled, queue the message
        if (!_isDatabaseAvailable && _bufferingEnabled && _messageBuffer is not null)
        {
            if (_messageBuffer.Count < MaxBufferSize)
            {
                _messageBuffer.Enqueue(iRawPacketRecordTyped);
                ILogger.Debug(
                    $"📦 Message buffered ({_messageBuffer.Count}/{MaxBufferSize}): {iRawPacketRecordTyped.PacketEnum} id '{iRawPacketRecordTyped.Id}'");
                return;
            }
            else
            {
                ILogger.Warning(
                    $"⚠️ Buffer full ({MaxBufferSize}), dropping message: {iRawPacketRecordTyped.PacketEnum} id '{iRawPacketRecordTyped.Id}'");
                return;
            }
        }

        // If database is unavailable and buffering is disabled, drop the message
        if (!_isDatabaseAvailable)
        {
            ILogger.Warning(
                $"🔶 Database unavailable, dropping message: {iRawPacketRecordTyped.PacketEnum} id '{iRawPacketRecordTyped.Id}'");
            return;
        }

        // Database is available - attempt to write
        await WriteToDatabase(iRawPacketRecordTyped);
    }

    async Task WriteToDatabase(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        var tableString = PacketToTableDictionary[iRawPacketRecordTyped.PacketEnum];
            var sql = $@"INSERT INTO public.""{tableString}"" "
            + " (id, json_document_original, application_received_utc_timestampz)"
            + " VALUES (@id, @json_document_original, to_timestamp(@application_received_utc_timestampz))";

            // If the table supports an installation_id column, include it for multi-installation identification.
            // installation_id column exists and is UUID (verified during InitializeAsync)
            sql = $@"INSERT INTO public.""{tableString}"" "
                    + " (id, json_document_original, application_received_utc_timestampz, installation_id)"
                    + " VALUES (@id, @json_document_original, to_timestamp(@application_received_utc_timestampz), @installation_id)";

        try
        {
            // Create/open per operation; bounded via NpgsqlConnectionStringBuilder + cancellation.
            await using var npgsqlConnection = await CreateOpenConnectionAsync(_connectionString, LinkedCancellationToken);
            await using var npgsqlCommand = new NpgsqlCommand(sql, npgsqlConnection);

            npgsqlCommand.Parameters.AddWithValue("id", iRawPacketRecordTyped.Id);
            npgsqlCommand.Parameters.Add("json_document_original", NpgsqlTypes.NpgsqlDbType.Json).Value
                = iRawPacketRecordTyped.RawPacketJson;
            npgsqlCommand.Parameters.AddWithValue(
                "application_received_utc_timestampz", iRawPacketRecordTyped.ReceivedUtcUnixEpochSecondsAsLong);

            // Use cached installation id GUID for writes (installation id is stable per-installation)
            npgsqlCommand.Parameters.AddWithValue("installation_id", NpgsqlTypes.NpgsqlDbType.Uuid, _installationIdGuid);

            var rowsAffected = await npgsqlCommand.ExecuteNonQueryAsync(LinkedCancellationToken);

            // Track successful write
            _lastSuccessfulWrite = DateTime.UtcNow;
            _failureCount = 0; // Reset failure count on success

            // NEW: Link database record in provenance
            ProvenanceTracker?.LinkDatabaseRecord(iRawPacketRecordTyped.Id, iRawPacketRecordTyped.Id);

            ILogger.Information(
                $"✅ Wrote to PostgreSQL '{tableString}' - {iRawPacketRecordTyped.PacketEnum} id '{iRawPacketRecordTyped.Id}' ({rowsAffected} row)");
        }
        catch (OperationCanceledException) when (LinkedCancellationToken.IsCancellationRequested)
        {
            _failureCount++;
            _lastConnectionAttempt = DateTime.UtcNow;
            ILogger.Warning("⚠️ Database write cancelled by external shutdown");
            if (_bufferingEnabled && _messageBuffer is not null && _messageBuffer.Count < MaxBufferSize)
            {
                _messageBuffer.Enqueue(iRawPacketRecordTyped);
                ILogger.Warning($"📦 Message buffered due to shutdown (buffer size: {_messageBuffer.Count})");
            }
        }
        catch (Npgsql.NpgsqlException npgsqlException)
        {
            _failureCount++;
            _lastConnectionAttempt = DateTime.UtcNow;

            ILogger.Error(
                $"❌ PostgreSQL error writing to '{tableString}' (failure #{_failureCount}): " +
                $"{npgsqlException.Message} (ErrorCode: {npgsqlException.ErrorCode})");

            ProvenanceTracker?.RecordError(
                iRawPacketRecordTyped.Id,
                "ListenerSink",
                "Database Write",
                npgsqlException);

            if (npgsqlException.SqlState == "23505") // Unique violation
                ILogger.Warning($"   💡 Duplicate key violation - message may have been processed already");
            else if (npgsqlException.SqlState == "08006") // Connection failure
            {
                ILogger.Warning($"   💡 Connection lost - marking database as unavailable");
                _isDatabaseAvailable = false;
            }

            // If buffering enabled and failures mounting, buffer this message
            if (_bufferingEnabled && _messageBuffer is not null)
            {
                if (_messageBuffer.Count < MaxBufferSize)
                {
                    _messageBuffer.Enqueue(iRawPacketRecordTyped);
                    ILogger.Warning($"📦 Message moved to buffer after failure (buffer size: {_messageBuffer.Count})");
                }
                else
                {
                    ILogger.Warning($"⚠️ Buffer full - dropping message after failure: {iRawPacketRecordTyped.Id}");
                }
            }
        }
        catch (TimeoutException)
        {
            _failureCount++;
            _lastConnectionAttempt = DateTime.UtcNow;
            ILogger.Error($"⏱️ Database write timeout (failure #{_failureCount})");

            ProvenanceTracker?.RecordError(
                iRawPacketRecordTyped.Id,
                "ListenerSink",
                "Database Write",
                new TimeoutException($"Database write timeout (failure #{_failureCount})"));

            // On timeout treat as transient but consider buffering
            if (_bufferingEnabled && _messageBuffer is not null && _messageBuffer.Count < MaxBufferSize)
            {
                _messageBuffer.Enqueue(iRawPacketRecordTyped);
                ILogger.Warning($"📦 Message buffered due to timeout (buffer size: {_messageBuffer.Count})");
            }
        }
        catch (Exception exception)
        {
            _failureCount++;
            _lastConnectionAttempt = DateTime.UtcNow;

            ILogger.Error(
                $"❌ Failed to write to PostgreSQL '{tableString}' (failure #{_failureCount}): " +
                $"{exception.GetType().Name}: {exception.Message}");

            ProvenanceTracker?.RecordError(
                iRawPacketRecordTyped.Id,
                "ListenerSink",
                "Database Write",
                exception);

            // Buffer as a fallback if configured
            if (_bufferingEnabled && _messageBuffer is not null && _messageBuffer.Count < MaxBufferSize)
            {
                _messageBuffer.Enqueue(iRawPacketRecordTyped);
                ILogger.Warning($"📦 Message buffered after unknown error (buffer size: {_messageBuffer.Count})");
            }
            else
            {
                // On unknown fatal errors mark DB unavailable to trigger reconnection attempts
                _isDatabaseAvailable = false;
            }
        }
    }

    // NOTE: ClearConnectionFactoryAsync removed (factory inlined).

    async Task ProcessBufferedMessagesAsync()
    {
        if (_messageBuffer == null || _messageBuffer.IsEmpty)
            return;

        var bufferSize = _messageBuffer.Count;
        ILogger.Information($"📤 Processing {bufferSize} buffered messages...");

        int processed = 0;
        int failed = 0;

        while (_messageBuffer.TryDequeue(out var message) && _isDatabaseAvailable && !LinkedCancellationToken.IsCancellationRequested)
        {
            try
            {
                await WriteToDatabase(message);
                processed++;

                // Small delay to avoid overwhelming the database
                if (processed % 10 == 0)
                {
                    try { await Task.Delay(100, LinkedCancellationToken); } catch (OperationCanceledException) { break; }
                }
            }
            catch (OperationCanceledException) when (LinkedCancellationToken.IsCancellationRequested)
            {
                // Put message back and exit
                _messageBuffer.Enqueue(message);
                ILogger.Warning("⚠️ Buffer processing cancelled by external shutdown");
                break;
            }
            catch (Exception exception)
            {
                failed++;
                ILogger.Error($"❌ Failed to process buffered message: {exception.Message}");

                // If failures mount, stop processing and re-queue remaining
                if (failed >= 5)
                {
                    _messageBuffer.Enqueue(message); // Put it back
                    ILogger.Warning($"⚠️ Stopping buffer processing due to failures. {_messageBuffer.Count} messages remain.");
                    _isDatabaseAvailable = false;
                    break;
                }
            }
        }

        ILogger.Information(
            $"✅ Buffer processing complete. Processed: {processed}, Failed: {failed}, Remaining: {_messageBuffer.Count}");
    }

    protected override async Task OnDisposeAsync()
    {
        try
        {
            // Stop timers first
            try { _healthCheckTimer?.Dispose(); } catch { }
            _healthCheckTimer = null;
            try { _reconnectionTimer?.Dispose(); } catch { }
            _reconnectionTimer = null;

            // Unregister from relay (use backing field to be safe)
            try
            {
                IEventRelayBasic?.Unregister<IRawPacketRecordTyped>(this);
            }
            catch { /* swallow */ }

            // No factory to clear anymore.

            // Log disposal
            try { _iLogger?.Information("🧹 PostgreSQL listener disposed"); } catch { }
        }
        catch (Exception ex)
        {
            try { _iLogger?.Warning($"⚠️ Error during ListenerSink disposal: {ex.Message}"); } catch { }
        }

        await Task.CompletedTask;
    }
}