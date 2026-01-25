namespace MetWorks.Common;
/// <summary>
/// Lightweight base for long-running services:
/// - standard linked CancellationTokenSource pattern
/// - background task tracking helper
/// - safe disposal (cancel -> wait -> dispose)
/// </summary>
public abstract class ServiceBase : IAsyncDisposable
{
    protected bool _isInitialized = false;

    protected ILogger? _iLogger = null;
    protected ILogger ILogger => NullPropertyGuard.Get(_isInitialized, _iLogger, nameof(ILogger));
    ISettingRepository? _iSettingRepository = null;
    protected ISettingRepository ISettingRepository => NullPropertyGuard.Get(_isInitialized, _iSettingRepository, nameof(ISettingRepository));
    IEventRelayBasic? _iEventRelayBasic = null;
    protected IEventRelayBasic IEventRelayBasic => NullPropertyGuard.Get(_isInitialized, _iEventRelayBasic, nameof(IEventRelayBasic));
    IEventRelayPath? _iEventRelayPath = null;
    protected IEventRelayPath IEventRelayPath => NullPropertyGuard.Get(_isInitialized, _iEventRelayPath, nameof(IEventRelayPath));
    ProvenanceTracker? _provenanceTracker = null;
    protected bool HaveProvenanceTracker => _isInitialized && _provenanceTracker != null;
    protected ProvenanceTracker? ProvenanceTracker => NullPropertyGuard.Get(_isInitialized, _provenanceTracker, nameof(ProvenanceTracker));
    // Owned local CTS + linked CTS that honors external cancellation
    protected CancellationTokenSource LocalCancellationTokenSource { get; private set; } = new();
    protected CancellationToken LocalCancellationToken => LocalCancellationTokenSource.Token;

    protected CancellationTokenSource? LinkedCancellationTokenSource { get; private set; }
    protected CancellationToken LinkedCancellationToken => LinkedCancellationTokenSource?.Token ?? CancellationToken.None;

    // Track background tasks started through StartBackground
    readonly List<Task> _backgroundTasks = new();

    /// <summary>
    /// Call early in derived Initialize to wire logger and cancellation.
    /// Sets backing logger and marks initialized so NullPropertyGuard access is safe.
    /// </summary>
    protected void InitializeBase(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null
    )
    {
        _iLogger = iLogger;
        _iSettingRepository = iSettingRepository;
        _iEventRelayBasic = iEventRelayBasic;
        _iEventRelayPath = iSettingRepository.IEventRelayPath;
        _provenanceTracker = provenanceTracker;

        _isInitialized = true;

        LocalCancellationTokenSource = new CancellationTokenSource();
        LinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            externalCancellation, LocalCancellationToken);
    }

    /// <summary>
    /// Start and track a background task that observes the linked cancellation token.
    /// </summary>
    protected void StartBackground(Func<CancellationToken, Task> backgroundWork)
    {
        if (backgroundWork == null) throw new ArgumentNullException(nameof(backgroundWork));
        var token = LinkedCancellationToken;
        var t = Task.Run(async () =>
        {
            try
            {
                await backgroundWork(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                try { ILogger.Debug("Background task cancelled"); } catch { }
            }
            catch (Exception ex)
            {
                try { ILogger.Error($"Unhandled background task exception: {ex.Message}"); } catch { }
                throw;
            }
        }, token);

        lock (_backgroundTasks)
        {
            _backgroundTasks.Add(t);
        }
    }

    protected async Task WaitForBackgroundTasksAsync(TimeSpan timeout)
    {
        Task[] copy;
        lock (_backgroundTasks) { copy = _backgroundTasks.ToArray(); }
        if (copy.Length == 0) return;

        try
        {
            var all = Task.WhenAll(copy);
            var completed = await Task.WhenAny(all, Task.Delay(timeout)).ConfigureAwait(false);
            if (completed != all)
            {
                try { ILogger.Warning("Background tasks did not complete in time"); } catch { }
            }
        }
        catch (Exception ex)
        {
            try { ILogger.Debug($"WaitForBackgroundTasks exception: {ex.Message}"); } catch { }
        }
    }

    protected virtual Task OnDisposeAsync() => Task.CompletedTask;

    public async virtual ValueTask DisposeAsync()
    {
        try
        {
            try { _iLogger?.Information("Disposing service..."); } catch { }

            await OnDisposeAsync().ConfigureAwait(false);

            try { LocalCancellationTokenSource?.Cancel(); } catch { }
            try { LinkedCancellationTokenSource?.Cancel(); } catch { }

            await WaitForBackgroundTasksAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try { _iLogger?.Warning($"Error during disposal: {ex.Message}"); } catch { }
        }
        finally
        {
            try { LinkedCancellationTokenSource?.Dispose(); } catch { }
            try { LocalCancellationTokenSource?.Dispose(); } catch { }
            _isInitialized = false;
        }
    }
}