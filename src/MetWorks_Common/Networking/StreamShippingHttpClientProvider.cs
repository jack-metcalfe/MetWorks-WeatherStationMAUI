namespace MetWorks.Common.Networking;

using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MetWorks.Constants;

public sealed class StreamShippingHttpClientProvider : ServiceBase
{
    const int DefaultTimeoutSeconds = 120;
    const int MinTimeoutSeconds = 5;
    const int MaxTimeoutSeconds = 10 * 60;

    HttpClient? _client;

    public StreamShippingHttpClientProvider()
    {
    }

    public HttpClient Client => NullPropertyGuard.Get(_isInitialized, _client, nameof(Client));

    bool _isInitialized;

    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);

        InitializeBase(
            iLogger.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker
        );

        var timeoutSeconds = iSettingRepository.GetValueOrDefault<int>(
LookupDictionaries.StreamShippingHttpGroupSettingsDefinition.BuildPath(SettingConstants.StreamShippingHttp_timeoutSeconds));

        if (timeoutSeconds <= 0)
            timeoutSeconds = DefaultTimeoutSeconds;

        if (timeoutSeconds < MinTimeoutSeconds)
            timeoutSeconds = MinTimeoutSeconds;
        else if (timeoutSeconds > MaxTimeoutSeconds)
            timeoutSeconds = MaxTimeoutSeconds;

        var allowInvalidTlsForEndpointHost = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.StreamShippingHttpGroupSettingsDefinition.BuildPath(SettingConstants.StreamShippingHttp_allowInvalidTlsForEndpointHost));

        HttpMessageHandler handler;
        if (!string.IsNullOrWhiteSpace(allowInvalidTlsForEndpointHost))
        {
            var endpointHost = allowInvalidTlsForEndpointHost.Trim();
            var innerHandler = new HttpClientHandler();

            innerHandler.ServerCertificateCustomValidationCallback = (_, certificate, chain, sslPolicyErrors) =>
            {
#if DEBUG
                var cert2 = certificate as X509Certificate2;
                var subject = cert2?.Subject ?? "(unknown)";

                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;

                if (cert2 is null)
                    return false;

                if (cert2.Subject.Contains($"OU={endpointHost}", StringComparison.OrdinalIgnoreCase)
                    || cert2.Subject.Contains($"CN = {endpointHost}", StringComparison.OrdinalIgnoreCase))
                {
                    ILogger.Warning($"StreamShippingHttpClientProvider: accepting invalid TLS certificate for host '{endpointHost}' (errors={sslPolicyErrors}, subject='{subject}').");
                    return true;
                }

                return false;
#else
                return sslPolicyErrors == SslPolicyErrors.None;
#endif
            };

            handler = innerHandler;
        }
        else
        {
            handler = new HttpClientHandler();
        }

        _client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        _isInitialized = true;

        try { MarkReady(); } catch { }
        if (!string.IsNullOrWhiteSpace(allowInvalidTlsForEndpointHost))
            ILogger.Warning($"StreamShippingHttpClientProvider initialized (timeout={timeoutSeconds}s, allowInvalidTlsForEndpointHost='{allowInvalidTlsForEndpointHost}')");
        else
            ILogger.Information($"StreamShippingHttpClientProvider initialized (timeout={timeoutSeconds}s)");
        return Task.FromResult(true);
    }

    protected override async Task OnDisposeAsync()
    {
        try
        {
            _client?.Dispose();
        }
        catch
        {
        }

        await Task.CompletedTask;
    }
}
