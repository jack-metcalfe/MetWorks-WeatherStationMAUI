using System.Collections.Concurrent;
using System.Threading;

namespace RawPacketRecordTypedInPostgresOut;

public class ListenerSink : IBackgroundService
{
    bool _isInitialized = false;

    ILogger? _iLogger = null;
    ILogger ILogger
    {
        get => NullPropertyGuard.Get(_isInitialized, _iLogger, nameof(ILogger));
        set => _iLogger = value;
    }

    IEventRelayBasic? _iEventRelayBasic = null;
    IEventRelayBasic IEventRelayBasic
    {
        get => NullPropertyGuard.Get(
            _isInitialized, _iEventRelayBasic, nameof(IEventRelayBasic));
        set => _iEventRelayBasic = value;
    }

    ISettingRepository? _iSettingsRepository;
    ISettingRepository ISettingRepository
    {
        get => NullPropertyGuard.Get(
            _isInitialized, _iSettingsRepository, nameof(ISettingRepository)
        );
        set => _iSettingsRepository = value;
    }
    ProvenanceTracker? ProvenanceTracker { get; set; }
    static Dictionary<PacketEnum, string> PacketToTableDictionary = new ()
    {
        { PacketEnum.Lightning, "lightning" },
        { PacketEnum.Observation, "observation" },
        { PacketEnum.Precipitation, "precipitation" },
        { PacketEnum.Wind, "wind" }
    };
    
    // Connection state tracking
    bool _isDatabaseAvailable = false;
    bool _isInitializing = false;
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
    PostgresConnectionFactory? PostgresConnectionFactory { get; set; }
    PostgresConnectionFactory PostgresConnectionFactorySafe => NullPropertyGuard.GetSafeClass(
        PostgresConnectionFactory, "Listener not initialized. Call InitializeAsync before using.");

    public ListenerSink()
    {
    }
    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        ProvenanceTracker? provenanceTracker = null
    )
    {
        try
        {
            _iLogger = iLogger;
            ISettingRepository = iSettingRepository;
            IEventRelayBasic = iEventRelayBasic;
            ProvenanceTracker = provenanceTracker;
            _isInitialized = true;
            if (ProvenanceTracker != null)
                ILogger.Information("🔍 Provenance tracking enabled for PostgreSQL listener");

            _bufferingEnabled = ISettingRepository.GetValueOrDefault<bool>(
                    XMLToPostgreSQLGroupSettingsDefinition.BuildSettingPath(XMLToPostgreSQL_enableBuffering)
                );

            if (_bufferingEnabled)
            {
                _messageBuffer = new ConcurrentQueue<IRawPacketRecordTyped>();
                ILogger.Information("📦 Message buffering enabled for PostgreSQL listener");
            }

            _connectionString = ISettingRepository.GetValueOrDefault<string>(
                    XMLToPostgreSQLGroupSettingsDefinition.BuildSettingPath(XMLToPostgreSQL_connectionString)
                );

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                ILogger.Warning("⚠️ PostgreSQL connection string not configured. Running in degraded mode.");
                await StartDegradedAsync();
                return true; // Return true to allow app to continue
            }
            
            // Attempt initial connection
            var connected = await TryEstablishConnectionAsync(_connectionString);
            
            if (connected)
            {
                ILogger.Information("✅ PostgreSQL listener initialized with active connection");
                await StartAsync();
            }
            else
            {
                ILogger.Warning("⚠️ PostgreSQL initially unavailable. Starting in degraded mode with auto-reconnect.");
                await StartDegradedAsync();
            }
            
            return true; // Always return true - we handle failures gracefully
        }
        catch (Exception exception)
        {
            ILogger.Error($"❌ Error during PostgreSQL listener initialization: {exception.Message}");
            ILogger.Warning("⚠️ Starting PostgreSQL listener in degraded mode");
            await StartDegradedAsync();
            return true; // Don't throw - allow app to continue
        }
    }
    
    private async Task<bool> TryEstablishConnectionAsync(string connectionString)
    {
        if (_isInitializing)
        {
            ILogger.Debug("⏳ Connection attempt already in progress, skipping");
            return false;
        }
        
        _isInitializing = true;
        _lastConnectionAttempt = DateTime.UtcNow;
        
        try
        {
            ILogger.Information("🔌 Attempting to establish PostgreSQL connection...");
            
            // Create the connection factory
            PostgresConnectionFactory = await PostgresConnectionFactory.CreateAsync(
                ILogger, connectionString);
            
            // Test the connection using the new async method
            await using (var testConnection = await PostgresConnectionFactorySafe.CreateConnectionAsync())
            {
                // Verify connection is working and get server info
                await using var versionCommand = new NpgsqlCommand("SELECT version()", testConnection);
                var version = await versionCommand.ExecuteScalarAsync();
                
                if (version != null)
                {
                    // Extract just the PostgreSQL version number for cleaner logging
                    var versionString = version.ToString() ?? "Unknown";
                    var versionShort = versionString.Split(' ').Take(2).ToArray();
                    ILogger.Information($"✅ Connected to {string.Join(" ", versionShort)}");
                }
                
                // Verify we can query system tables
                await using var testQuery = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'", 
                    testConnection);
                var tableCount = await testQuery.ExecuteScalarAsync();
                ILogger.Debug($"📊 Found {tableCount} tables in public schema");
            }
            
            // Initialize database schema (this creates its own connection)
            ILogger.Information("🗄️ Initializing database schema...");
            await PostgresInitializer.DatabaseInitializeAsync(
                ILogger, PostgresConnectionFactorySafe.CreateConnection());
            
            _isDatabaseAvailable = true;
            _failureCount = 0;
            _lastSuccessfulWrite = DateTime.UtcNow;
            
            ILogger.Information("✅ PostgreSQL connection established successfully");
            
            // Process any buffered messages
            if (_bufferingEnabled && _messageBuffer != null && !_messageBuffer.IsEmpty)
            {
                var bufferedCount = _messageBuffer.Count;
                ILogger.Information($"📦 Found {bufferedCount} buffered messages, starting processing...");
                _ = Task.Run(() => ProcessBufferedMessagesAsync());
            }
            
            return true;
        }
        catch (Npgsql.NpgsqlException npgsqlException)
        {
            _isDatabaseAvailable = false;
            
            // Provide detailed PostgreSQL-specific error information
            var errorDetails = $"ErrorCode: {npgsqlException.ErrorCode}";
            if (!string.IsNullOrWhiteSpace(npgsqlException.SqlState))
            {
                errorDetails += $", SqlState: {npgsqlException.SqlState}";
            }
            
            ILogger.Warning(
                $"⚠️ Failed to establish PostgreSQL connection: {npgsqlException.Message} ({errorDetails})");
            
            // Log specific error hints
            if (npgsqlException.Message.Contains("timeout"))
            {
                ILogger.Warning("   💡 Hint: Check network connectivity and firewall settings");
            }
            else if (npgsqlException.Message.Contains("authentication"))
            {
                ILogger.Warning("   💡 Hint: Verify username and password in connection string");
            }
            else if (npgsqlException.Message.Contains("does not exist"))
            {
                ILogger.Warning("   💡 Hint: Ensure the database exists and is accessible");
            }
            
            return false;
        }
        catch (TimeoutException timeoutException)
        {
            _isDatabaseAvailable = false;
            ILogger.Warning(
                $"⚠️ Connection timeout: {timeoutException.Message}");
            ILogger.Warning("   💡 Hint: Database server may be unreachable or overloaded");
            return false;
        }
        catch (Exception exception)
        {
            _isDatabaseAvailable = false;
            ILogger.Warning($"⚠️ Failed to establish PostgreSQL connection: {exception.Message}");
            ILogger.Debug($"   Exception type: {exception.GetType().Name}");
            ILogger.Debug($"   Stack trace: {exception.StackTrace}");
            return false;
        }
        finally
        {
            _isInitializing = false;
        }
    }
    
    private async Task StartDegradedAsync()
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
    
    public async Task<bool> StartAsync()
    {
        ILogger.Information("✅ PostgreSQL Listener started in ACTIVE mode");
        IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);
        
        // Start health monitoring
        StartHealthMonitoring();
        
        // Start reconnection monitoring (will only reconnect if connection fails)
        StartReconnectionTimer();
        
        return await Task.FromResult(true);
    }
    
    private void StartHealthMonitoring()
    {
        if (_healthCheckTimer != null)
            return; // Already started
        
        // Check health every 60 seconds
        _healthCheckTimer = new Timer(
            HealthCheckCallback, 
            null, 
            TimeSpan.FromSeconds(60), 
            TimeSpan.FromSeconds(60));
        
        ILogger.Information("🏥 Health monitoring started for PostgreSQL listener");
    }
    
    private void StartReconnectionTimer()
    {
        if (_reconnectionTimer != null)
            return; // Already started
        
        // Attempt reconnection every 30 seconds if database is unavailable
        _reconnectionTimer = new Timer(
            ReconnectionCallback,
            null,
            TimeSpan.FromSeconds(ReconnectionIntervalSeconds),
            TimeSpan.FromSeconds(ReconnectionIntervalSeconds));
        
        ILogger.Information("🔄 Reconnection timer started for PostgreSQL listener");
    }
    
    private void HealthCheckCallback(object? state)
    {
        try
        {
            var status = _isDatabaseAvailable ? "ACTIVE" : "DEGRADED";
            var timeSinceLastWrite = DateTime.UtcNow - _lastSuccessfulWrite;
            var bufferSize = _messageBuffer?.Count ?? 0;
            
            if (_isDatabaseAvailable)
            {
                // Database is up - check for stale writes
                if (timeSinceLastWrite > TimeSpan.FromMinutes(5) && _lastSuccessfulWrite != DateTime.MinValue)
                {
                    ILogger.Warning(
                        $"⚠️ PostgreSQL [{status}] No writes in {timeSinceLastWrite.TotalMinutes:F1} minutes. " +
                        $"Buffer: {bufferSize} messages");
                }
                else
                {
                    ILogger.Debug(
                        $"💚 PostgreSQL [{status}] Healthy - Last write: {timeSinceLastWrite.TotalSeconds:F0}s ago. " +
                        $"Failures: {_failureCount}");
                }
            }
            else
            {
                // Database is down
                var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
                ILogger.Warning(
                    $"🔶 PostgreSQL [{status}] Unavailable - Last attempt: {timeSinceLastAttempt.TotalSeconds:F0}s ago. " +
                    $"Buffer: {bufferSize} messages. " +
                    $"Failures: {_failureCount}");
            }
            
            // If too many consecutive failures, mark as unavailable
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
    
    private void ReconnectionCallback(object? state)
    {
        // Only attempt reconnection if database is unavailable
        if (_isDatabaseAvailable)
            return;
        
        // Don't retry too frequently
        var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
        if (timeSinceLastAttempt < TimeSpan.FromSeconds(ReconnectionIntervalSeconds - 5))
            return;
        
        ILogger.Information("🔄 Attempting automatic PostgreSQL reconnection...");
        
        _ = Task.Run(async () =>
        {
            var connectionString = ISettingRepository
                .GetValueOrDefault(@"/services/RawPacketRecordTypedInPostgresOut/connectionString");
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ILogger.Warning("⚠️ Cannot reconnect - connection string not available");
                return;
            }
            
            var connected = await TryEstablishConnectionAsync(connectionString);
            
            if (connected)
            {
                ILogger.Information("✅ PostgreSQL reconnection SUCCESSFUL! Resuming normal operation.");
            }
            else
            {
                ILogger.Warning("❌ PostgreSQL reconnection failed. Will retry in 30 seconds.");
            }
        });
    }
    
    void ReceiveHandler(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        _ = Task.Run(async () => await ProcessMessage(iRawPacketRecordTyped));
    }
    
    private async Task ProcessMessage(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        // If database is unavailable and buffering is enabled, queue the message
        if (!_isDatabaseAvailable && _bufferingEnabled && _messageBuffer != null)
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
    
    private async Task WriteToDatabase(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        var tableString = PacketToTableDictionary[iRawPacketRecordTyped.PacketEnum];
        var sql = $@"INSERT INTO public.""{tableString}"" "
            + " (id, json_document_original, application_received_utc_timestampz)"
            + " VALUES (@id, @json_document_original, to_timestamp(@application_received_utc_timestampz))";

        try
        {
            // Use async connection creation for better performance
            await using var npgsqlConnection = await PostgresConnectionFactorySafe.CreateConnectionAsync();
            await using var npgsqlCommand = new NpgsqlCommand(sql, npgsqlConnection);

            npgsqlCommand.Parameters.AddWithValue("id", iRawPacketRecordTyped.Id);
            npgsqlCommand.Parameters.Add("json_document_original", NpgsqlTypes.NpgsqlDbType.Json).Value 
                = iRawPacketRecordTyped.RawPacketJson;
            npgsqlCommand.Parameters.AddWithValue(
                "application_received_utc_timestampz", iRawPacketRecordTyped.ReceivedUtcUnixEpochSecondsAsLong);

            var rowsAffected = await npgsqlCommand.ExecuteNonQueryAsync();

            // Track successful write
            _lastSuccessfulWrite = DateTime.UtcNow;
            _failureCount = 0; // Reset failure count on success

            // NEW: Link database record in provenance
            ProvenanceTracker?.LinkDatabaseRecord(iRawPacketRecordTyped.Id, iRawPacketRecordTyped.Id);

            ILogger.Information(
                $"✅ Wrote to PostgreSQL '{tableString}' - {iRawPacketRecordTyped.PacketEnum} id '{iRawPacketRecordTyped.Id}' ({rowsAffected} row)");
        }
        catch (Npgsql.NpgsqlException npgsqlException)
        {
            _failureCount++;
            
            ILogger.Error(
                $"❌ PostgreSQL error writing to '{tableString}' (failure #{_failureCount}): " +
                $"{npgsqlException.Message} (ErrorCode: {npgsqlException.ErrorCode})");
            
            // NEW: Record error in provenance
            ProvenanceTracker?.RecordError(
                iRawPacketRecordTyped.Id,
                "ListenerSink",
                "Database Write",
                npgsqlException);
            
            // Check for specific PostgreSQL errors
            if (npgsqlException.SqlState == "23505") // Unique violation
            {
                ILogger.Warning($"   💡 Duplicate key violation - message may have been processed already");
            }
            else if (npgsqlException.SqlState == "08006") // Connection failure
            {
                ILogger.Warning($"   💡 Connection lost - marking database as unavailable");
                _isDatabaseAvailable = false;
            }
            
            // If buffering enabled and failures mounting, buffer this message
            if (_bufferingEnabled && _messageBuffer != null && _failureCount >= 3)
            {
                if (_messageBuffer.Count < MaxBufferSize)
                {
                    _messageBuffer.Enqueue(iRawPacketRecordTyped);
                    ILogger.Warning($"📦 Message moved to buffer after failure (buffer size: {_messageBuffer.Count})");
                }
            }
        }
        catch (TimeoutException)
        {
            _failureCount++;
            ILogger.Error($"⏱️ Database write timeout (failure #{_failureCount})");
            
            // NEW: Record timeout error in provenance
            ProvenanceTracker?.RecordError(
                iRawPacketRecordTyped.Id,
                "ListenerSink",
                "Database Write",
                new TimeoutException($"Database write timeout (failure #{_failureCount})"));
        }
        catch (Exception exception)
        {
            _failureCount++;
            
            ILogger.Error(
                $"❌ Failed to write to PostgreSQL '{tableString}' (failure #{_failureCount}): " +
                $"{exception.GetType().Name}: {exception.Message}");
            
            // NEW: Record generic error in provenance
            ProvenanceTracker?.RecordError(
                iRawPacketRecordTyped.Id,
                "ListenerSink",
                "Database Write",
                exception);
        }
    }
    
    private async Task ProcessBufferedMessagesAsync()
    {
        if (_messageBuffer == null || _messageBuffer.IsEmpty)
            return;
        
        var bufferSize = _messageBuffer.Count;
        ILogger.Information($"📤 Processing {bufferSize} buffered messages...");
        
        int processed = 0;
        int failed = 0;
        
        while (_messageBuffer.TryDequeue(out var message) && _isDatabaseAvailable)
        {
            try
            {
                await WriteToDatabase(message);
                processed++;
                
                // Small delay to avoid overwhelming the database
                if (processed % 10 == 0)
                {
                    await Task.Delay(100);
                }
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
    
    public async ValueTask DisposeAsync()
    {
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;
        
        _reconnectionTimer?.Dispose();
        _reconnectionTimer = null;

        IEventRelayBasic.Unregister<IRawPacketRecordTyped>(this);

        ILogger.Information("🧹 PostgreSQL listener disposed");
        await Task.CompletedTask;
    }
}