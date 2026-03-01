namespace MetWorks.Common;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

using MetWorks.Constants;
using MetWorks.Interfaces;

public sealed class TempestWebSocketObservationsProvider : ServiceBase, ITempestWebSocketObservationsProvider
{
    const string WsBaseUrl = "wss://ws.weatherflow.com/swd/data?token=";

    static readonly TimeSpan ConnectRetryMinDelay = TimeSpan.FromSeconds(2);
    static readonly TimeSpan ConnectRetryMaxDelay = TimeSpan.FromSeconds(30);
    static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(5);
    static readonly TimeSpan InteractiveAuthRequestMinInterval = TimeSpan.FromMinutes(1);

    readonly object _gate = new();

    TempestWebSocketObservationsSnapshot? _latest;

    long _bytesInTotal;
    long _bytesOutTotal;

    DateTimeOffset? _lastInteractiveAuthRequestUtc;

    IStationMetadataProvider? _stationMetadataProvider;
    ITempestOAuthTokenProvider? _tokenProvider;

    public TempestWebSocketObservationsProvider() { }

    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        ITempestOAuthTokenProvider iTempestOAuthTokenProvider,
        IStationMetadataProvider iStationMetadataProvider,
        CancellationToken externalCancellation = default
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iTempestOAuthTokenProvider);
        ArgumentNullException.ThrowIfNull(iStationMetadataProvider);

        InitializeBase(
            iLogger.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation
        );

        _tokenProvider = iTempestOAuthTokenProvider;
        _stationMetadataProvider = iStationMetadataProvider;

        StartBackground(RunAsync);

        MarkReady();
        ILogger.Information("TempestWebSocketObservationsProvider initialized");

        return Task.FromResult(true);
    }

    public ValueTask<TempestWebSocketObservationsSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) { return ValueTask.FromResult(_latest); }
    }

    async Task RunAsync(CancellationToken token)
    {
        // Single forever-loop: wait for enabled + token + deviceId, then connect.
        var delay = ConnectRetryMinDelay;

        while (!token.IsCancellationRequested)
        {
            var enabled = LoadEnabled();
            if (!enabled)
            {
                delay = ConnectRetryMinDelay;
                await Task.Delay(IdleDelay, token).ConfigureAwait(false);
                continue;
            }

            var tokenProvider = _tokenProvider;
            var stationMetadataProvider = _stationMetadataProvider;
            if (tokenProvider is null || stationMetadataProvider is null)
                return;

            string? accessToken = null;
            try
            {
                accessToken = await tokenProvider.GetAccessTokenAsync(allowInteractive: false, token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (InvalidOperationException ex)
            {
                ILogger.Warning($"TempestWebSocket: token provider error. {ex.Message}");
            }
            catch (NotSupportedException ex)
            {
                ILogger.Warning($"TempestWebSocket: token provider error. {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                MaybeRequestInteractiveOAuth(
                    requestedUtc: DateTimeOffset.UtcNow,
                    reason: "WebSocket ingest is enabled, but no cached OAuth access token is available."
                );
                delay = ConnectRetryMinDelay;
                await Task.Delay(TimeSpan.FromSeconds(10), token).ConfigureAwait(false);
                continue;
            }

            long deviceId = 0;
            try
            {
                deviceId = await ResolveDeviceIdAsync(stationMetadataProvider, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (InvalidOperationException ex)
            {
                ILogger.Warning($"TempestWebSocket: device id resolution failed. {ex.Message}");
            }

            if (deviceId <= 0)
            {
                delay = ConnectRetryMinDelay;
                await Task.Delay(TimeSpan.FromSeconds(10), token).ConfigureAwait(false);
                continue;
            }

            var wsUrl = WsBaseUrl + Uri.EscapeDataString(accessToken);

            try
            {
                await ConnectAndReceiveLoopAsync(wsUrl, deviceId, token).ConfigureAwait(false);
                delay = ConnectRetryMinDelay;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (WebSocketException ex)
            {
                ILogger.Warning($"TempestWebSocket: socket error. {ex.Message}");
            }
            catch (JsonException ex)
            {
                ILogger.Warning($"TempestWebSocket: invalid JSON message. {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                ILogger.Warning($"TempestWebSocket: invalid operation. {ex.Message}");
            }

            await Task.Delay(delay, token).ConfigureAwait(false);
            delay = NextDelay(delay);
        }
    }

    bool LoadEnabled()
    {
        var enabledPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_websocket_enabled);
        return ISettingRepository.GetValueOrDefault<bool>(enabledPath);
    }

    async Task<long> ResolveDeviceIdAsync(IStationMetadataProvider stationMetadataProvider, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var deviceIdPath = LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_websocket_deviceId);
        var configured = ISettingRepository.GetValueOrDefault<long>(deviceIdPath);
        if (configured > 0)
            return configured;

        var md = await stationMetadataProvider.GetStationMetadataAsync(token).ConfigureAwait(false);
        return md?.TempestDeviceId ?? 0;
    }

    static TimeSpan NextDelay(TimeSpan current)
    {
        var next = current + current;
        if (next < ConnectRetryMinDelay) return ConnectRetryMinDelay;
        if (next > ConnectRetryMaxDelay) return ConnectRetryMaxDelay;
        return next;
    }

    async Task ConnectAndReceiveLoopAsync(string wsUrl, long deviceId, CancellationToken token)
    {
        using var ws = new ClientWebSocket();
        ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

        await ws.ConnectAsync(new Uri(wsUrl, UriKind.Absolute), token).ConfigureAwait(false);

        // Subscribe to observation stream and rapid wind.
        await SendJsonAsync(ws, new
        {
            type = "listen_start",
            device_id = deviceId,
            id = "metworks-listen"
        }, token).ConfigureAwait(false);

        await SendJsonAsync(ws, new
        {
            type = "listen_rapid_start",
            device_id = deviceId,
            id = "metworks-rapid"
        }, token).ConfigureAwait(false);

        var buffer = new byte[8192];

        while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            var (text, receivedBytes) = await ReceiveTextMessageAsync(ws, buffer, token).ConfigureAwait(false);
            if (text is null)
                continue;

            Interlocked.Add(ref _bytesInTotal, receivedBytes);

            var receivedUtc = DateTimeOffset.UtcNow;

            string messageType = "unknown";
            long messageDeviceId = deviceId;

            using (var doc = JsonDocument.Parse(text))
            {
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String)
                        messageType = t.GetString() ?? "unknown";

                    if (root.TryGetProperty("device_id", out var did) && did.ValueKind == JsonValueKind.Number && did.TryGetInt64(out var didVal))
                        messageDeviceId = didVal;
                }
            }

            var snapshot = new TempestWebSocketObservationsSnapshot(
                DeviceId: messageDeviceId,
                ReceivedUtc: receivedUtc,
                MessageType: messageType,
                RawJson: text
            );

            lock (_gate) { _latest = snapshot; }

            // Publish raw message snapshot; mapping happens in the mux.
            IEventRelayBasic.Send(snapshot);

        }
    }

    async Task SendJsonAsync(ClientWebSocket ws, object payload, CancellationToken token)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        Interlocked.Add(ref _bytesOutTotal, bytes.Length);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: token).ConfigureAwait(false);
    }

    static async Task<(string? Text, long Bytes)> ReceiveTextMessageAsync(ClientWebSocket ws, byte[] buffer, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(ws);
        ArgumentNullException.ThrowIfNull(buffer);

        long totalBytes = 0;

        using var ms = new MemoryStream();
        while (true)
        {
            var res = await ws.ReceiveAsync(buffer, token).ConfigureAwait(false);
            if (res.MessageType == WebSocketMessageType.Close)
            {
                return (null, totalBytes);
            }

            if (res.Count > 0)
            {
                totalBytes += res.Count;
                ms.Write(buffer, 0, res.Count);
            }

            if (res.EndOfMessage)
                break;
        }

        var text = Encoding.UTF8.GetString(ms.ToArray());
        return (text, totalBytes);
    }

    void MaybeRequestInteractiveOAuth(DateTimeOffset requestedUtc, string reason)
    {
        bool shouldSend;
        lock (_gate)
        {
            shouldSend = _lastInteractiveAuthRequestUtc is null
                || (requestedUtc - _lastInteractiveAuthRequestUtc.Value) >= InteractiveAuthRequestMinInterval;
            if (shouldSend)
                _lastInteractiveAuthRequestUtc = requestedUtc;
        }

        if (!shouldSend)
            return;

        ILogger.Warning("TempestWebSocket: OAuth access token is missing; UI authorization is required.");

        try
        {
            IEventRelayBasic.Send(new TempestOAuthInteractiveAuthRequest(
                RequestedUtc: requestedUtc,
                Reason: reason
            ));
        }
        catch (InvalidOperationException ex)
        {
            ILogger.Warning($"TempestWebSocket: failed to publish OAuth interactive auth request. {ex.Message}");
        }
    }
}
