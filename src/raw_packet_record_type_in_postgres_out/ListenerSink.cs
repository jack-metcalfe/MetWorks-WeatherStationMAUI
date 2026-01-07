using System.Collections.Concurrent;
using System.Threading;

namespace RawPacketRecordTypedInPostgresOut;

public class ListenerSink : IBackgroundService
{
    static Dictionary<PacketEnum, string> PacketToTableDictionary = new ()
    {
        { PacketEnum.Lightning, "lightning" },
        { PacketEnum.Observation, "observation" },
        { PacketEnum.Precipitation, "precipitation" },
        { PacketEnum.Wind, "wind" }
    };
    
    // Connection state tracking
    private bool _isDatabaseAvailable = false;
    private bool _isInitializing = false;
    private DateTime _lastConnectionAttempt = DateTime.MinValue;
    private DateTime _lastSuccessfulWrite = DateTime.MinValue;
    private int _failureCount = 0;
    private const int MaxConsecutiveFailures = 5;
    private const int ReconnectionIntervalSeconds = 30;
    
    // Message buffering (optional)
    private ConcurrentQueue<IRawPacketRecordTyped>? _messageBuffer;
    private const int MaxBufferSize = 1000;
    private bool _bufferingEnabled = false;
    
    // Health monitoring
    private Timer? _healthCheckTimer;
    private Timer? _reconnectionTimer;
    
    PostgresConnectionFactory? PostgresConnectionFactory { get; set; }
    PostgresConnectionFactory PostgresConnectionFactorySafe => NullPropertyGuard.GetSafeClass(
        PostgresConnectionFactory, "Listener not initialized. Call InitializeAsync before using.");
    
    IFileLogger? IFileLogger { get; set; }
    IFileLogger IFileLoggerSafe => NullPropertyGuard.GetSafeClass(
        IFileLogger, "Listener not initialized. Call InitializeAsync before using.");
    
    ISettingsRepository? ISettingsRepository { get; set; }
    ISettingsRepository ISettingsRepositorySafe => NullPropertyGuard.GetSafeClass(
        ISettingsRepository, "Listener not initialized. Call InitializeAsync before using.");
    
    ProvenanceTracker? ProvenanceTracker { get; set; }
    
    public ListenerSink()
    {
    }
    
    public async Task<bool> InitializeAsync(
        IFileLogger iFileLogger,
        ISettingsRepository iSettingsRepository,
        ProvenanceTracker? provenanceTracker = null  // NEW: Optional
    )
    {
        try
        {
            IFileLogger = iFileLogger;
            ISettingsRepository = iSettingsRepository;
            ProvenanceTracker = provenanceTracker;

            if (ProvenanceTracker != null)
            {
                IFileLoggerSafe.Information("🔍 Provenance tracking enabled for PostgreSQL listener");
            }
            
            // Check if buffering is enabled
            var bufferingSetting = iSettingsRepository.GetValueOrDefault(
                "/services/RawPacketRecordTypedInPostgresOut/enableBuffering");
            _bufferingEnabled = bool.TryParse(bufferingSetting, out var bufferEnabled) && bufferEnabled;
            
            if (_bufferingEnabled)
            {
                _messageBuffer = new ConcurrentQueue<IRawPacketRecordTyped>();
                IFileLoggerSafe.Information("📦 Message buffering enabled for PostgreSQL listener");
            }
            
            var connectionString = iSettingsRepository
                .GetValueOrDefault(@"/services/RawPacketRecordTypedInPostgresOut/connectionString");
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                IFileLoggerSafe.Warning("⚠️ PostgreSQL connection string not configured. Running in degraded mode.");
                await StartDegradedAsync();
                return true; // Return true to allow app to continue
            }
            
            // Attempt initial connection
            var connected = await TryEstablishConnectionAsync(connectionString);
            
            if (connected)
            {
                IFileLoggerSafe.Information("✅ PostgreSQL listener initialized with active connection");
                await StartAsync();
            }
            else
            {
                IFileLoggerSafe.Warning("⚠️ PostgreSQL initially unavailable. Starting in degraded mode with auto-reconnect.");
                await StartDegradedAsync();
            }
            
            return true; // Always return true - we handle failures gracefully
        }
        catch (Exception exception)
        {
            IFileLoggerSafe.Error($"❌ Error during PostgreSQL listener initialization: {exception.Message}");
            IFileLoggerSafe.Warning("⚠️ Starting PostgreSQL listener in degraded mode");
            await StartDegradedAsync();
            return true; // Don't throw - allow app to continue
        }
    }
    
    private async Task<bool> TryEstablishConnectionAsync(string connectionString)
    {
        if (_isInitializing)
        {
            IFileLoggerSafe.Debug("⏳ Connection attempt already in progress, skipping");
            return false;
        }
        
        _isInitializing = true;
        _lastConnectionAttempt = DateTime.UtcNow;
        
        try
        {
            IFileLoggerSafe.Information("🔌 Attempting to establish PostgreSQL connection...");
            
            // Create the connection factory
            PostgresConnectionFactory = await PostgresConnectionFactory.CreateAsync(
                IFileLoggerSafe, connectionString);
            
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
                    IFileLoggerSafe.Information($"✅ Connected to {string.Join(" ", versionShort)}");
                }
                
                // Verify we can query system tables
                await using var testQuery = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'", 
                    testConnection);
                var tableCount = await testQuery.ExecuteScalarAsync();
                IFileLoggerSafe.Debug($"📊 Found {tableCount} tables in public schema");
            }
            
            // Initialize database schema (this creates its own connection)
            IFileLoggerSafe.Information("🗄️ Initializing database schema...");
            await PostgresInitializer.DatabaseInitializeAsync(
                IFileLoggerSafe, PostgresConnectionFactorySafe.CreateConnection());
            
            _isDatabaseAvailable = true;
            _failureCount = 0;
            _lastSuccessfulWrite = DateTime.UtcNow;
            
            IFileLoggerSafe.Information("✅ PostgreSQL connection established successfully");
            
            // Process any buffered messages
            if (_bufferingEnabled && _messageBuffer != null && !_messageBuffer.IsEmpty)
            {
                var bufferedCount = _messageBuffer.Count;
                IFileLoggerSafe.Information($"📦 Found {bufferedCount} buffered messages, starting processing...");
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
            
            IFileLoggerSafe.Warning(
                $"⚠️ Failed to establish PostgreSQL connection: {npgsqlException.Message} ({errorDetails})");
            
            // Log specific error hints
            if (npgsqlException.Message.Contains("timeout"))
            {
                IFileLoggerSafe.Warning("   💡 Hint: Check network connectivity and firewall settings");
            }
            else if (npgsqlException.Message.Contains("authentication"))
            {
                IFileLoggerSafe.Warning("   💡 Hint: Verify username and password in connection string");
            }
            else if (npgsqlException.Message.Contains("does not exist"))
            {
                IFileLoggerSafe.Warning("   💡 Hint: Ensure the database exists and is accessible");
            }
            
            return false;
        }
        catch (TimeoutException timeoutException)
        {
            _isDatabaseAvailable = false;
            IFileLoggerSafe.Warning(
                $"⚠️ Connection timeout: {timeoutException.Message}");
            IFileLoggerSafe.Warning("   💡 Hint: Database server may be unreachable or overloaded");
            return false;
        }
        catch (Exception exception)
        {
            _isDatabaseAvailable = false;
            IFileLoggerSafe.Warning($"⚠️ Failed to establish PostgreSQL connection: {exception.Message}");
            IFileLoggerSafe.Debug($"   Exception type: {exception.GetType().Name}");
            IFileLoggerSafe.Debug($"   Stack trace: {exception.StackTrace}");
            return false;
        }
        finally
        {
            _isInitializing = false;
        }
    }
    
    private async Task StartDegradedAsync()
    {
        IFileLoggerSafe.Warning("🔶 PostgreSQL listener running in DEGRADED MODE");
        IFileLoggerSafe.Information("🔄 Auto-reconnection enabled - will retry every 30 seconds");
        
        // Register event handler even in degraded mode
        ISingletonEventRelay.Register<IRawPacketRecordTyped>(this, ReceiveHandler);
        
        // Start reconnection attempts
        StartReconnectionTimer();
        
        // Start health monitoring
        StartHealthMonitoring();
        
        await Task.CompletedTask;
    }
    
    public async Task<bool> StartAsync()
    {
        IFileLoggerSafe.Information("✅ PostgreSQL Listener started in ACTIVE mode");
        ISingletonEventRelay.Register<IRawPacketRecordTyped>(this, ReceiveHandler);
        
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
        
        IFileLoggerSafe.Information("🏥 Health monitoring started for PostgreSQL listener");
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
        
        IFileLoggerSafe.Information("🔄 Reconnection timer started for PostgreSQL listener");
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
                    IFileLoggerSafe.Warning(
                        $"⚠️ PostgreSQL [{status}] No writes in {timeSinceLastWrite.TotalMinutes:F1} minutes. " +
                        $"Buffer: {bufferSize} messages");
                }
                else
                {
                    IFileLoggerSafe.Debug(
                        $"💚 PostgreSQL [{status}] Healthy - Last write: {timeSinceLastWrite.TotalSeconds:F0}s ago. " +
                        $"Failures: {_failureCount}");
                }
            }
            else
            {
                // Database is down
                var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
                IFileLoggerSafe.Warning(
                    $"🔶 PostgreSQL [{status}] Unavailable - Last attempt: {timeSinceLastAttempt.TotalSeconds:F0}s ago. " +
                    $"Buffer: {bufferSize} messages. " +
                    $"Failures: {_failureCount}");
            }
            
            // If too many consecutive failures, mark as unavailable
            if (_failureCount >= MaxConsecutiveFailures && _isDatabaseAvailable)
            {
                IFileLoggerSafe.Error(
                    $"❌ PostgreSQL has {_failureCount} consecutive failures. Marking as UNAVAILABLE.");
                _isDatabaseAvailable = false;
            }
        }
        catch (Exception exception)
        {
            IFileLoggerSafe.Error($"❌ Error in health check: {exception.Message}");
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
        
        IFileLoggerSafe.Information("🔄 Attempting automatic PostgreSQL reconnection...");
        
        _ = Task.Run(async () =>
        {
            var connectionString = ISettingsRepositorySafe
                .GetValueOrDefault(@"/services/RawPacketRecordTypedInPostgresOut/connectionString");
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                IFileLoggerSafe.Warning("⚠️ Cannot reconnect - connection string not available");
                return;
            }
            
            var connected = await TryEstablishConnectionAsync(connectionString);
            
            if (connected)
            {
                IFileLoggerSafe.Information("✅ PostgreSQL reconnection SUCCESSFUL! Resuming normal operation.");
            }
            else
            {
                IFileLoggerSafe.Warning("❌ PostgreSQL reconnection failed. Will retry in 30 seconds.");
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
                IFileLoggerSafe.Debug(
                    $"📦 Message buffered ({_messageBuffer.Count}/{MaxBufferSize}): {iRawPacketRecordTyped.PacketEnum} id '{iRawPacketRecordTyped.Id}'");
                return;
            }
            else
            {
                IFileLoggerSafe.Warning(
                    $"⚠️ Buffer full ({MaxBufferSize}), dropping message: {iRawPacketRecordTyped.PacketEnum} id '{iRawPacketRecordTyped.Id}'");
                return;
            }
        }
        
        // If database is unavailable and buffering is disabled, drop the message
        if (!_isDatabaseAvailable)
        {
            IFileLoggerSafe.Warning(
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

            IFileLoggerSafe.Information(
                $"✅ Wrote to PostgreSQL '{tableString}' - {iRawPacketRecordTyped.PacketEnum} id '{iRawPacketRecordTyped.Id}' ({rowsAffected} row)");
        }
        catch (Npgsql.NpgsqlException npgsqlException)
        {
            _failureCount++;
            
            IFileLoggerSafe.Error(
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
                IFileLoggerSafe.Warning($"   💡 Duplicate key violation - message may have been processed already");
            }
            else if (npgsqlException.SqlState == "08006") // Connection failure
            {
                IFileLoggerSafe.Warning($"   💡 Connection lost - marking database as unavailable");
                _isDatabaseAvailable = false;
            }
            
            // If buffering enabled and failures mounting, buffer this message
            if (_bufferingEnabled && _messageBuffer != null && _failureCount >= 3)
            {
                if (_messageBuffer.Count < MaxBufferSize)
                {
                    _messageBuffer.Enqueue(iRawPacketRecordTyped);
                    IFileLoggerSafe.Warning($"📦 Message moved to buffer after failure (buffer size: {_messageBuffer.Count})");
                }
            }
        }
        catch (TimeoutException)
        {
            _failureCount++;
            IFileLoggerSafe.Error($"⏱️ Database write timeout (failure #{_failureCount})");
            
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
            
            IFileLoggerSafe.Error(
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
        IFileLoggerSafe.Information($"📤 Processing {bufferSize} buffered messages...");
        
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
                IFileLoggerSafe.Error($"❌ Failed to process buffered message: {exception.Message}");
                
                // If failures mount, stop processing and re-queue remaining
                if (failed >= 5)
                {
                    _messageBuffer.Enqueue(message); // Put it back
                    IFileLoggerSafe.Warning($"⚠️ Stopping buffer processing due to failures. {_messageBuffer.Count} messages remain.");
                    _isDatabaseAvailable = false;
                    break;
                }
            }
        }
        
        IFileLoggerSafe.Information(
            $"✅ Buffer processing complete. Processed: {processed}, Failed: {failed}, Remaining: {_messageBuffer.Count}");
    }
    
    public async ValueTask DisposeAsync()
    {
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;
        
        _reconnectionTimer?.Dispose();
        _reconnectionTimer = null;
        
        ISingletonEventRelay.Unregister<IRawPacketRecordTyped>(this);
        
        IFileLoggerSafe.Information("🧹 PostgreSQL listener disposed");
        await Task.CompletedTask;
    }
}