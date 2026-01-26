namespace MetWorks.Networking.Udp.Transformer;
public class TempestPacketTransformer : ServiceBase
{
    private const int MaxRetryAttempts = 3;
    private const int RetryDelaySeconds = 5;
    private const int ReceiveTimeoutSeconds = 10;
    private const int NoDataWarningMinutes = 1;
    private DateTime _lastPacketReceived = DateTime.MinValue;
    private long _totalPacketsReceived = 0;
    private long _totalPacketErrors = 0;
    private int _consecutiveErrors = 0;
    private const int MaxConsecutiveErrors = 10;
    UdpClient? UdpClient { get; set; }
    // Lock to protect replacement of the UdpClient instance
    private readonly SemaphoreSlim _udpClientLock = new(1, 1);
    public TempestPacketTransformer()
    {
    }
    // Accept CancellationToken (not CTS) per .NET convention
    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null
    )
    {
        try
        {
            InitializeBase(
                iLogger,
                iSettingRepository,
                iEventRelayBasic,
                externalCancellation,
                provenanceTracker
            );

            iLogger.Information($"🔍 Provenance tracking {(HaveProvenanceTracker ? string.Empty : "NOT")} enabled for Tempest Packet Transformer");

            // Log network interfaces for diagnostics
            LogNetworkInterfaces();

            // Subscribe to network events so we can rebind when connectivity changes
            SubscribeToNetworkEvents();

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

            return true;
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
                LookupDictionaries.UdpListenerGroupSettingsDefinition.BuildSettingPath(SettingConstants.UdpListener_preferredPort)
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
        var token = LinkedCancellationToken;
        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                // Respect external cancellation
                if (token.IsCancellationRequested)
                {
                    ILogger.Warning("⚠️ Bind cancelled by external shutdown");
                    return false;
                }

                // Create UdpClient without binding so we can set socket options first
                var client = new UdpClient(AddressFamily.InterNetwork);

                // Allow reuse of address on platforms that support it so rebind is more resilient
                try
                {
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                }
                catch
                {
                    // Not critical if platform doesn't allow this; log for diagnostics
                    ILogger.Debug("Could not set SO_REUSEADDR on UdpClient (platform may not support it)");
                }

                // Bind explicitly (so exceptions are thrown here)
                client.Client.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));

                if (client == null)
                {
                    throw new InvalidOperationException("UdpClient instantiation returned null");
                }

                // Swap in new client under lock and dispose old
                        await _udpClientLock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    var old = UdpClient;
                    UdpClient = client;
                    old?.Dispose();
                }
                finally
                {
                    _udpClientLock.Release();
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

                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), token).ConfigureAwait(false);
                }
                else
                {
                    ILogger.Warning(
                        $"❌ Port {port} unavailable after {MaxRetryAttempts} attempts: " +
                        $"{socketException.SocketErrorCode}");
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                ILogger.Warning("⚠️ Port bind cancelled by external shutdown");
                return false;
            }
            catch (Exception exception)
            {
                ILogger.Warning($"❌ Unexpected error binding to port {port}: {exception.Message}");
                return false;
            }
        }

        return false;
    }
    // Called to attempt a rebind when we detect network issues or events
    private async Task<bool> TryRebindIfNeededAsync(CancellationToken? optionalToken = null)
    {
        var token = optionalToken ?? LinkedCancellationToken;
        if (token.IsCancellationRequested)
            return false;

        var preferredPort = ISettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.UdpListenerGroupSettingsDefinition.BuildSettingPath(SettingConstants.UdpListener_preferredPort)
        );

        ILogger.Information("🔁 Attempting to rebind UDP listener after connectivity change...");
        // Attempt preferred then alternates (same as SetupAsync)
        if (await TryBindToPortAsync(preferredPort))
            return true;

        for (int port = preferredPort + 1; port < preferredPort + 10; port++)
        {
            if (await TryBindToPortAsync(port))
            {
                ILogger.Information($"✅ Successfully rebound to alternate port {port}");
                return true;
            }
        }

        ILogger.Warning("⚠️ Rebind attempts failed");
        return false;
    }
    public async Task<bool> StartAsync()
    {
        try
        {
            // Use ServiceBase helper to start a tracked background task
            StartBackground(ReceiveLoopAsync);

            ILogger.Information("🚀 UDP receive loop started");
            return true;
        }
        catch (Exception exception)
        {
            ILogger.LogExceptionAndReturn(exception, "Failed to start receive loop");
            return false;
        }
    }
    async Task ReceiveLoopAsync(CancellationToken token)
    {
        var lastWarningTime = DateTime.MinValue;
        var noDataWarningInterval = TimeSpan.FromMinutes(NoDataWarningMinutes);

        ILogger.Information("📡 UDP receive loop active, waiting for packets...");
        ILogger.Information($"⏱️ Timeout: {ReceiveTimeoutSeconds}s | Warning interval: {NoDataWarningMinutes}m");

        while (!token.IsCancellationRequested)
        {
            try
            {
                // Use a timeout to detect if no data is being received
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(ReceiveTimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    token,
                    timeoutCts.Token);

                try
                {
                    // CRITICAL: This is the blocking call that waits for UDP packets
                    UdpReceiveResult result;
                    // Acquire a snapshot of the client under lock to avoid race with rebind
                    await _udpClientLock.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        if (UdpClient == null)
                            throw new InvalidOperationException("UdpClient is not initialized");
                        result = await UdpClient.ReceiveAsync(linkedCts.Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        _udpClientLock.Release();
                    }

                    // Packet received successfully
                    _lastPacketReceived = DateTime.UtcNow;
                    _totalPacketsReceived++;
                    _consecutiveErrors = 0; // Reset error counter on success
                    lastWarningTime = DateTime.MinValue; // Reset warning timer

                    // Process the packet
                    await ProcessPacketAsync(result).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested &&
                                                          !token.IsCancellationRequested)
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
            catch (OperationCanceledException) when (token.IsCancellationRequested)
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

                // If error is likely due to network/interface going away, attempt to rebind
                if (socketException.SocketErrorCode == SocketError.NetworkDown ||
                    socketException.SocketErrorCode == SocketError.NetworkReset ||
                    socketException.SocketErrorCode == SocketError.ConnectionReset ||
                    socketException.SocketErrorCode == SocketError.HostDown ||
                    socketException.SocketErrorCode == SocketError.NetworkUnreachable)
                {
                    ILogger.Warning("🔁 Detected network-related socket error; attempting to rebind UdpClient...");
                    try
                    {
                        var rebound = await TryRebindIfNeededAsync(token);
                        if (rebound)
                        {
                            _consecutiveErrors = 0;
                            continue; // Try receiving again with new client
                        }
                    }
                    catch (Exception ex)
                    {
                        ILogger.Warning($"⚠️ Rebind attempt threw: {ex.Message}");
                    }
                }

                // If too many consecutive errors, something is seriously wrong
                if (_consecutiveErrors >= MaxConsecutiveErrors)
                {
                    ILogger.Error(
                        $"❌ Too many consecutive socket errors ({_consecutiveErrors}). " +
                        "UDP listener may be in a bad state. Consider restarting the application.");

                    // Don't break - keep trying, but throttle retries
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { /* shutting down */ }
                }
                else
                {
                    // Brief delay before retrying
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { /* shutting down */ }
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
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { /* shutting down */ }
                }
                else
                {
                    // Brief delay to avoid tight error loop
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { /* shutting down */ }
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
                ProvenanceTracker?.UpdateStatus(iRawPacketRecordTyped.Id, DataStatusEnum.Failed);

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

    // Subscribe to OS network events to trigger rebind attempts when connectivity changes
    private void SubscribeToNetworkEvents()
    {
        try
        {
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }
        catch (Exception ex)
        {
            ILogger.Warning($"⚠️ Failed to subscribe to network change events: {ex.Message}");
        }
    }

    private void UnsubscribeFromNetworkEvents()
    {
        try
        {
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        }
        catch (Exception ex)
        {
            ILogger.Warning($"⚠️ Failed to unsubscribe from network change events: {ex.Message}");
        }
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        // Start a tracked background task to attempt rebind (non-blocking)
        StartBackground(async token =>
        {
            try
            {
                ILogger.Information("🔔 Network address change detected, attempting rebind...");
                await TryRebindIfNeededAsync(token);
            }
            catch (Exception ex)
            {
                try { ILogger.Warning($"⚠️ Rebind on address change failed: {ex.Message}"); } catch { }
            }
        });
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        StartBackground(async token =>
        {
            try
            {
                ILogger.Information($"🔔 Network availability changed: Available={e.IsAvailable}. Attempting rebind if available...");
                if (e.IsAvailable)
                {
                    await TryRebindIfNeededAsync(token);
                }
            }
            catch (Exception ex)
            {
                try { ILogger.Warning($"⚠️ Rebind on availability change failed: {ex.Message}"); } catch { }
            }
        });
    }

    protected override Task OnDisposeAsync()
    {
        try
        {
            // Use backing fields to avoid NullPropertyGuard exceptions if Dispose called before Initialize
            try
            {
                _iLogger?.Information("🧹 Disposing UDP listener...");
            }
            catch { /* swallow */ }

            // Unsubscribe network events
            try
            {
                UnsubscribeFromNetworkEvents();
            }
            catch { /* swallow */ }

            // Dispose resources under lock to avoid race with receive loop
            _udpClientLock.Wait();
            try
            {
                try { UdpClient?.Dispose(); } catch { /* swallow */ }
                UdpClient = null;
            }
            finally
            {
                _udpClientLock.Release();
            }
        }
        catch (Exception ex)
        {
            try { _iLogger?.Warning($"⚠️ Error during UDP listener disposal: {ex.Message}"); } catch { }
        }

        return Task.CompletedTask;
    }
}
