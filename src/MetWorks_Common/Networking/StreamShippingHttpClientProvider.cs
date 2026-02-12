namespace MetWorks.Common.Networking;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MetWorks.Constants;

public sealed class StreamShippingHttpClientProvider : ServiceBase
{
    const int DefaultTimeoutSeconds = 30;

    HttpClient? _client;

    public StreamShippingHttpClientProvider()
    {
    }

    public HttpClient Client => NullPropertyGuard.Get(_isInitialized, _client, nameof(Client));

    bool _isInitialized;

    public Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null)
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);

        InitializeBase(
            iLoggerResilient.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker);

        var timeoutSeconds = iSettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.StreamShippingHttpGroupSettingsDefinition.BuildSettingPath(SettingConstants.StreamShippingHttp_timeoutSeconds));

        if (timeoutSeconds <= 0)
            timeoutSeconds = DefaultTimeoutSeconds;

        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        _isInitialized = true;

        try { MarkReady(); } catch { }
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
