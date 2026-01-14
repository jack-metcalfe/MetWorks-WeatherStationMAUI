using System.Net.NetworkInformation;

namespace UdpInRawPacketRecordTypedOut;

/// <summary>
/// Layer 1 listener: reads UDP packets, wraps them in WeatherRawMessage, and broadcasts via IMessenger.
/// Handles bind failures gracefully and logs all diagnostics.
/// Enhanced with health monitoring and robust error recovery.
/// </summary>
public sealed partial class Transformer : IAsyncDisposable, IBackgroundService
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
            _isInitialized, _iEventRelayBasic, nameof(IEventRelayBasic)
        );
        set => _iEventRelayBasic = value;
    }

    ISettingRepository? _iSettingRepository;
    ISettingRepository ISettingRepository
    {
        get => NullPropertyGuard.Get(
            _isInitialized, _iSettingRepository, nameof(IEventRelayBasic)
        );
        set => _iSettingRepository = value;
    }

    private const int MaxRetryAttempts = 3;
    private const int RetryDelaySeconds = 5;
    private const int ReceiveTimeoutSeconds = 10;
    private const int NoDataWarningMinutes = 1;

    private Task? _receiveTask;
    private DateTime _lastPacketReceived = DateTime.MinValue;
    private long _totalPacketsReceived = 0;
    private long _totalPacketErrors = 0;
    private int _consecutiveErrors = 0;
    private const int MaxConsecutiveErrors = 10;
    
    UdpClient? UdpClient { get; set; }
    UdpClient UdpClientSafe => NullPropertyGuard.GetSafeClass(
        UdpClient, "Listener not initialized. Call InitializeAsync before using.", ILogger);
    
    CancellationTokenSource LocalCancellationTokenSource { get; set; } = new ();
    CancellationTokenSource LocalCancellationTokenSourceSafe => NullPropertyGuard.GetSafeClass(
        LocalCancellationTokenSource, "Listener not initialized. Call InitializeAsync before using.", ILogger);
    CancellationToken LocalCancellationTokenSafe => LocalCancellationTokenSourceSafe.Token;
    
    CancellationTokenSource? LinkedCancellationTokenSource { get; set; }
    CancellationTokenSource LinkedCancellationTokenSourceSafe => NullPropertyGuard.GetSafeClass(
        LinkedCancellationTokenSource, "Listener not initialized. Call InitializeAsync before using.", ILogger);
    
    ProvenanceTracker? ProvenanceTracker { get; set; }    
    public Transformer()
    {
    }    
    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        CancellationTokenSource externalCancellationTokenSource,
        ProvenanceTracker? provenanceTracker = null
    )
    {
        try
        {
            ILogger = iLogger;
            ISettingRepository = iSettingRepository;
            IEventRelayBasic = iEventRelayBasic;
            ProvenanceTracker = provenanceTracker;
            _isInitialized = true;

            if (ProvenanceTracker is null)
                ILogger.Warning("⚠️ Provenance tracking is not enabled for UDP listener");
            else
                ILogger.Information("🔍 Provenance tracking enabled for UDP listener");

            LinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                externalCancellationTokenSource.Token, LocalCancellationTokenSafe
            );
            // Log network interfaces for diagnostics
            LogNetworkInterfaces();

            if (!await SetupAsync().ConfigureAwait(false))
            {
                ILogger.Error("❌ UDP listener setup failed");
                return false;
            }
            
            if (!await StartAsync().ConfigureAwait(false))
            {
                ILogger.Error("❌ UDP listener failed to start");
                return false;
            }

            ILogger.Information("🛠️ UDP listener initialized successfully");

            return await Task.FromResult(_isInitialized);
        }
        catch (Exception exception)
        {
            throw ILogger.LogExceptionAndReturn(exception, "❌ UDP listener initialization failed");
        }
    }
    private void LogNetworkInterfaces()
    {
        try
        {
            ILogger.Information("📡 Available network interfaces:");
            
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up && 
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    ILogger.Information($"   • {networkInterface.Name} ({networkInterface.Description}) - {networkInterface.NetworkInterfaceType}");
                    
                    foreach (var addr in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ILogger.Information($"     IPv4: {addr.Address}");
                        }
                    }
                }
            }
        }
        catch (Exception exception)
        {
            ILogger.Warning($"⚠️ Could not enumerate network interfaces: {exception.Message}");
        }
    }

    async Task<bool> SetupAsync()
    {
        var preferredPort = ISettingRepository.GetValueOrDefault<int>(
                UdpListenerGroupSettingsDefinition.BuildSettingPath(UdpListener_preferredPort)
            );
        
        // Try preferred port first
        if (await TryBindToPortAsync(preferredPort))
            return true;
        
        // If preferred port fails, try alternate ports
        ILogger.Warning($"⚠️ Failed to bind to preferred port {preferredPort}, trying alternates...");
        
        for (int port = preferredPort + 1; port < preferredPort + 10; port++)
        {
            if (await TryBindToPortAsync(port))
            {
                ILogger.Information($"✅ Successfully bound to alternate port {port}");
                return true;
            }
        }
        
        ILogger.Error("❌ Failed to bind to any UDP port after trying alternates");
        return false;
    }
    
    private async Task<bool> TryBindToPortAsync(int port)
    {
        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                // Bind to all interfaces (0.0.0.0) to receive from any network
                UdpClient = new UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
                
                if (UdpClient == null)
                {
                    throw new InvalidOperationException("UdpClient instantiation returned null");
                }
                
                var endpoint = UdpClient.Client.LocalEndPoint?.ToString() ?? "(unbound)";
                ILogger.Information($"✅ Bound UDP listener to ALL INTERFACES on port {port} ({endpoint})");
                
                await Task.CompletedTask;
                return true;
            }
            catch (SocketException socketException) when (
                socketException.SocketErrorCode == SocketError.AddressAlreadyInUse ||
                socketException.SocketErrorCode == SocketError.AccessDenied)
            {
                if (attempt < MaxRetryAttempts)
                {
                    ILogger.Warning(
                        $"⚠️ Attempt {attempt}/{MaxRetryAttempts}: Port {port} unavailable " +
                        $"({socketException.SocketErrorCode}). Retrying in {RetryDelaySeconds}s...");
                    
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds));
                }
                else
                {
                    ILogger.Warning(
                        $"❌ Port {port} unavailable after {MaxRetryAttempts} attempts: " +
                        $"{socketException.SocketErrorCode}");
                }
            }
            catch (Exception exception)
            {
                ILogger.Warning($"❌ Unexpected error binding to port {port}: {exception.Message}");
                return false;
            }
        }
        
        return false;
    }
    
    public async Task<bool> StartAsync()
    {
        try
        {
            _receiveTask = Task.Run(async () => await ReceiveLoopAsync());
            ILogger.Information("🚀 UDP receive loop started");
            return await Task.FromResult(true);
        }
        catch (Exception exception)
        {
            ILogger.LogExceptionAndReturn(exception, "Failed to start receive loop");
            return await Task.FromResult(false);
        }
    }
    
    async Task ReceiveLoopAsync()
    {
        var lastWarningTime = DateTime.MinValue;
        var noDataWarningInterval = TimeSpan.FromMinutes(NoDataWarningMinutes);
        
        ILogger.Information("📡 UDP receive loop active, waiting for packets...");
        ILogger.Information($"⏱️ Timeout: {ReceiveTimeoutSeconds}s | Warning interval: {NoDataWarningMinutes}m");
        
        while (!LocalCancellationTokenSourceSafe.IsCancellationRequested)
        {
            try
            {
                // Use a timeout to detect if no data is being received
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(ReceiveTimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    LocalCancellationTokenSafe, 
                    timeoutCts.Token);
                
                try
                {
                    // CRITICAL: This is the blocking call that waits for UDP packets
                    UdpReceiveResult result = await UdpClientSafe.ReceiveAsync(linkedCts.Token);
                    
                    // Packet received successfully
                    _lastPacketReceived = DateTime.UtcNow;
                    _totalPacketsReceived++;
                    _consecutiveErrors = 0; // Reset error counter on success
                    lastWarningTime = DateTime.MinValue; // Reset warning timer
                    
                    // Process the packet
                    await ProcessPacketAsync(result);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && 
                                                          !LocalCancellationTokenSafe.IsCancellationRequested)
                {
                    // Timeout occurred (no packet received in 10 seconds) - this is normal
                    // Check if we should warn about no data
                    if (DateTime.UtcNow - lastWarningTime >= noDataWarningInterval)
                    {
                        var timeSinceLastPacket = _lastPacketReceived == DateTime.MinValue 
                            ? "never" 
                            : $"{(DateTime.UtcNow - _lastPacketReceived).TotalMinutes:F1} minutes ago";
                        
                        ILogger.Warning(
                            $"⏰ No UDP packets received in {NoDataWarningMinutes} minute(s). " +
                            $"Last packet: {timeSinceLastPacket}. " +
                            $"Total received: {_totalPacketsReceived}. " +
                            $"Is the weather station transmitting?");
                        
                        lastWarningTime = DateTime.UtcNow;
                    }
                    
                    // Continue waiting - this is not an error
                    continue;
                }
            }
            catch (OperationCanceledException) when (LocalCancellationTokenSafe.IsCancellationRequested)
            {
                // Graceful shutdown requested
                ILogger.Information("🛑 UDP listener canceled gracefully");
                break;
            }
            catch (ObjectDisposedException)
            {
                // UDP client was disposed (likely during shutdown)
                ILogger.Information("🛑 UDP client was disposed");
                break;
            }
            catch (SocketException socketException)
            {
                _consecutiveErrors++;
                _totalPacketErrors++;
                
                ILogger.Error(
                    $"⚠️ Socket error in UDP receive loop (#{_consecutiveErrors}): " +
                    $"{socketException.Message} (ErrorCode: {socketException.SocketErrorCode})");
                
                // If too many consecutive errors, something is seriously wrong
                if (_consecutiveErrors >= MaxConsecutiveErrors)
                {
                    ILogger.Error(
                        $"❌ Too many consecutive socket errors ({_consecutiveErrors}). " +
                        "UDP listener may be in a bad state. Consider restarting the application.");
                    
                    // Don't break - keep trying, but throttle retries
                    await Task.Delay(TimeSpan.FromSeconds(30), LocalCancellationTokenSafe);
                }
                else
                {
                    // Brief delay before retrying
                    await Task.Delay(TimeSpan.FromSeconds(1), LocalCancellationTokenSafe);
                }
            }
            catch (Exception exception)
            {
                _consecutiveErrors++;
                _totalPacketErrors++;
                
                // Log the error but DON'T throw - keep the receive loop running
                ILogger.Error(
                    $"⚠️ Unexpected error in UDP receive loop (#{_consecutiveErrors}): " +
                    $"{exception.GetType().Name}: {exception.Message}");
                
                ILogger.Debug($"   Stack trace: {exception.StackTrace}");
                
                // Throttle on repeated errors
                if (_consecutiveErrors >= MaxConsecutiveErrors)
                {
                    ILogger.Error(
                        $"❌ Too many consecutive errors ({_consecutiveErrors}). Throttling receive loop.");
                    await Task.Delay(TimeSpan.FromSeconds(30), LocalCancellationTokenSafe);
                }
                else
                {
                    // Brief delay to avoid tight error loop
                    await Task.Delay(TimeSpan.FromMilliseconds(100), LocalCancellationTokenSafe);
                }
            }
        }
        
        // Log final statistics
        ILogger.Information(
            $"🏁 UDP receive loop ended. " +
            $"Total packets: {_totalPacketsReceived}, " +
            $"Total errors: {_totalPacketErrors}");
    }
    
    private async Task ProcessPacketAsync(UdpReceiveResult result)
    {
        try
        {
            var packetAsReadOnlyMemoryOfChar = ReadOnlyMemoryOfCharFactory.From(result.Buffer);
            var iRawPacketRecordTyped = IRawPacketRecordTypedFactory.Create(packetAsReadOnlyMemoryOfChar);
            
            // NEW: Track packet reception in provenance system
            ProvenanceTracker?.TrackNewPacket(iRawPacketRecordTyped);
            
            if (iRawPacketRecordTyped.PacketEnum != PacketEnum.NotImplemented)
            {
                ProvenanceTracker?.AddStep(
                    iRawPacketRecordTyped.Id,
                    "JSON Parse",
                    "UdpTransformer",
                    $"Packet type: {iRawPacketRecordTyped.PacketEnum}");
                
                IEventRelayBasic.Send(iRawPacketRecordTyped);
                
                ILogger.Information(
                    $"📦 Received {iRawPacketRecordTyped.PacketEnum} packet from {result.RemoteEndPoint} " +
                    $"({result.Buffer.Length} bytes) [Total: {_totalPacketsReceived}]");
            }
            else
            {
                // NEW: Mark as failed
                ProvenanceTracker?.UpdateStatus(iRawPacketRecordTyped.Id, DataStatus.Failed);
                
                ILogger.Warning(
                    $"⚠️ Received unimplemented packet type from {result.RemoteEndPoint} " +
                        $"({result.Buffer.Length} bytes)");
                ILogger.Warning($"packet contents [{iRawPacketRecordTyped.RawPacketJson}]");

            }

            await Task.CompletedTask;
        }
        catch (Exception exception)
        {
            // Log packet processing errors but don't let them kill the receive loop
            ILogger.Error(
                $"❌ Error processing packet from {result.RemoteEndPoint}: " +
                $"{exception.Message}");
            
            // Optionally log the raw packet data for debugging
            ILogger.Debug($"   Raw packet ({result.Buffer.Length} bytes): " +
                $"{System.Text.Encoding.UTF8.GetString(result.Buffer).Substring(0, Math.Min(100, result.Buffer.Length))}...");
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        try
        {
            ILogger.Information("🧹 Disposing UDP listener...");
            
            // Cancel the receive loop
            LocalCancellationTokenSource?.Cancel();
            LinkedCancellationTokenSource?.Cancel();
            
            // Wait for receive task to complete (with timeout)
            if (_receiveTask != null && !_receiveTask.IsCompleted)
            {
                try
                {
                    await _receiveTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (TimeoutException)
                {
                    ILogger.Warning("⚠️ Receive task did not complete within timeout during disposal");
                }
            }
            
            // Dispose resources
            UdpClient?.Dispose();
            LinkedCancellationTokenSource?.Dispose();
            LocalCancellationTokenSource?.Dispose();
            
            ILogger.Information(
                $"✅ UDP listener disposed. Final stats - " +
                $"Packets: {_totalPacketsReceived}, Errors: {_totalPacketErrors}");
        }
        catch (Exception exception)
        {
            ILogger.Warning($"⚠️ Error during UDP listener disposal: {exception.Message}");
        }
        
        await Task.CompletedTask;
    }
}
