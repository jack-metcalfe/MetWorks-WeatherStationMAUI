using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

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

    // Protects creation/disposal/replacement of PostgresConnectionFactory
    readonly SemaphoreSlim _connectionLock = new(1,1);

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
        // Prevent overlapping connection attempts
        if (_isInitializing)
        {
            ILogger.Debug("⏳ Connection attempt already in progress, skipping");
            return false;
        }

        _isInitializing = true;
        _lastConnectionAttempt = DateTime.UtcNow;
        
        await _connectionLock.WaitAsync();
        try
        {
            ILogger.Information("🔌 Attempting to establish PostgreSQL connection...");
            
            // Create the connection factory
            var newFactory = await PostgresConnectionFactory.CreateAsync(ILogger, connectionString);

            // Test the connection using the new async method (with short timeout)
            using (var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            await using (var testConnection = await newFactory.CreateConnectionAsync(testCts.Token))
            {
                // Verify connection is working and get server info
                await using var versionCommand = new NpgsqlCommand("SELECT version()", testConnection);
                var version = await versionCommand.ExecuteScalarAsync(testCts.Token);

                if (version != null)
                {
                    var versionString = version.ToString() ?? "Unknown";
                    var versionShort = versionString.Split(' ').Take(2).ToArray();
                    ILogger.Information($"✅ Connected to {string.Join(" ", versionShort)}");
                }

                // Verify we can query system tables
                await using var testQuery = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'",
                    testConnection);
                var tableCount = await testQuery.ExecuteScalarAsync(testCts.Token);
                ILogger.Debug($"📊 Found {tableCount} tables in public schema");
            }

            // Replace the factory instance under lock (dispose old if supported)
            var oldFactory = PostgresConnectionFactory;
            PostgresConnectionFactory = newFactory;
            try
            {
                // Initialize database schema (this creates its own connection async)
                ILogger.Information("🗄️ Initializing database schema...");
                // Give schema init a slightly larger timeout
                using var initCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await PostgresInitializer.DatabaseInitializeAsync(
                    ILogger, await PostgresConnectionFactorySafe.CreateConnectionAsync(initCts.Token), initCts.Token);
            }
            catch (Exception exInit)
            {
                // If schema init fails, log and continue; connection is still usable in many cases
                ILogger.Warning($"⚠️ Database initialization failed: {exInit.Message}");
            }

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

            // Attempt to dispose old factory if it implements IDisposable
            try
            {
                (oldFactory as IDisposable)?.Dispose();
            }
            catch { /* swallow */ }
            
            return true;
        }
        catch (Npgsql.NpgsqlException npgsqlException)
        {
            _isDatabaseAvailable = false;
            _lastConnectionAttempt = DateTime.UtcNow;
            _failureCount++;

            var errorDetails = $"ErrorCode: {npgsqlException.ErrorCode}";
            if (!string.IsNullOrWhiteSpace(npgsqlException.SqlState))
            {
                errorDetails += $", SqlState: {npgsqlException.SqlState}";
            }
            
            ILogger.Warning(
                $"⚠️ Failed to establish PostgreSQL connection: {npgsqlException.Message} ({errorDetails})");
            
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
            _lastConnectionAttempt = DateTime.UtcNow;
            _failureCount++;
            ILogger.Warning(
                $"⚠️ Connection timeout: {timeoutException.Message}");
            ILogger.Warning("   💡 Hint: Database server may be unreachable or overloaded");
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
            _isInitializing = false;
            _connectionLock.Release();
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
                var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
                ILogger.Warning(
                    $"🔶 PostgreSQL [{status}] Unavailable - Last attempt: {timeSinceLastAttempt.TotalSeconds:F0}s ago. " +
                    $"Buffer: {bufferSize} messages. " +
                    $"Failures: {_failureCount}");
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
    
    private void ReconnectionCallback(object? state)
    {
        // Only attempt reconnection if database is unavailable
        if (_isDatabaseAvailable)
            return;
        
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
            {
                ILogger.Warning($"   💡 Duplicate key violation - message may have been processed already");
            }
            else if (npgsqlException.SqlState == "08006") // Connection failure
            {
                ILogger.Warning($"   💡 Connection lost - marking database as unavailable");
                _isDatabaseAvailable = false;
                // Dispose/clear factory so reconnection will create a fresh one
                await ClearConnectionFactoryAsync();
            }

            // If buffering enabled and failures mounting, buffer this message
            if (_bufferingEnabled && _messageBuffer != null)
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
            if (_bufferingEnabled && _messageBuffer != null && _messageBuffer.Count < MaxBufferSize)
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
            if (_bufferingEnabled && _messageBuffer != null && _messageBuffer.Count < MaxBufferSize)
            {
                _messageBuffer.Enqueue(iRawPacketRecordTyped);
                ILogger.Warning($"📦 Message buffered after unknown error (buffer size: {_messageBuffer.Count})");
            }
            else
            {
                // On unknown fatal errors mark DB unavailable to trigger reconnection attempts
                _isDatabaseAvailable = false;
                await ClearConnectionFactoryAsync();
            }
        }
    }

    // Safely clears/disposes the connection factory so reconnection creates a new one
    private async Task ClearConnectionFactoryAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            try
            {
                (PostgresConnectionFactory as IDisposable)?.Dispose();
            }
            catch { /* swallow */ }
            PostgresConnectionFactory = null;
        }
        finally
        {
            _connectionLock.Release();
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

        await ClearConnectionFactoryAsync();

        ILogger.Information("🧹 PostgreSQL listener disposed");
        await Task.CompletedTask;
    }
}