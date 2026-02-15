using MetWorks.Persistence.Rollups;

namespace MetWorks.Ingest.SQLite.Rollups;

public sealed class ObservationRollupWorker : ServiceBase
{
    bool _isDatabaseAvailable = false;
    int _isInitializing = 0;
    DateTime _lastConnectionAttempt = DateTime.MinValue;
    int _failureCount = 0;

    const int ReconnectionIntervalSeconds = 30;

    readonly SemaphoreSlim _gate = new(1, 1);

    Timer? _timer;
    Timer? _reconnectionTimer;

    IRollupsDatabaseReadiness? _rollupsDatabaseReadiness;
    IObservationRollupRepository? _observationRollupRepository;

    public ObservationRollupWorker()
    {
    }

    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IRollupsDatabaseReadiness rollupsDatabaseReadiness,
        IObservationRollupRepository observationRollupRepository,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null)
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(rollupsDatabaseReadiness);
        ArgumentNullException.ThrowIfNull(observationRollupRepository);

        iLogger.Information($"ObservationRollupWorker(SQLite).InitializeAsync() starting - thread={Environment.CurrentManagedThreadId}");

        try
        {
            InitializeBase(
                iLogger.ForContext(GetType()),
                iSettingRepository,
                iEventRelayBasic,
                externalCancellation,
                provenanceTracker
            );

            _rollupsDatabaseReadiness = rollupsDatabaseReadiness;
            _observationRollupRepository = observationRollupRepository;

            var connected = await TryEnsureReadyAsync().ConfigureAwait(false);
            if (!connected)
            {
                ILogger.Warning("⚠️ ObservationRollupWorker initial DB readiness check failed. Starting in degraded mode.");
            }

            StartAsync();

            try { MarkReady(); } catch { }
            return true;
        }
        catch (Exception exception)
        {
            iLogger.Error($"❌ Error during ObservationRollupWorker initialization: {exception.Message}");
            StartAsync();
            return true;
        }
    }

    void StartAsync()
    {
        if (_timer is null)
        {
            _timer = new Timer(
                TimerCallback,
                null,
                dueTime: TimeSpan.FromSeconds(30),
                period: TimeSpan.FromSeconds(30));
        }

        StartReconnectionTimer();
    }

    void StartReconnectionTimer()
    {
        if (_reconnectionTimer is not null) return;

        _reconnectionTimer = new Timer(
            ReconnectionCallback,
            null,
            TimeSpan.FromSeconds(ReconnectionIntervalSeconds),
            TimeSpan.FromSeconds(ReconnectionIntervalSeconds));
    }

    void ReconnectionCallback(object? state)
    {
        if (_isDatabaseAvailable) return;
        if (LinkedCancellationToken.IsCancellationRequested) return;

        var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
        if (timeSinceLastAttempt < TimeSpan.FromSeconds(ReconnectionIntervalSeconds - 5))
            return;

        StartBackground(async token =>
        {
            try
            {
                if (token.IsCancellationRequested) return;

                var connected = await TryEnsureReadyAsync().ConfigureAwait(false);
                if (connected)
                    ILogger.Information("✅ ObservationRollupWorker SQLite reconnection SUCCESSFUL.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                ILogger.Warning($"⚠️ ObservationRollupWorker reconnection task failed: {ex.Message}");
            }
        });
    }

    async Task<bool> TryEnsureReadyAsync()
    {
        if (LinkedCancellationToken.IsCancellationRequested) return false;

        if (Interlocked.CompareExchange(ref _isInitializing, 1, 0) != 0) return false;

        _lastConnectionAttempt = DateTime.UtcNow;

        try
        {
            using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedTestCts = CancellationTokenSource.CreateLinkedTokenSource(testCts.Token, LinkedCancellationToken);

            if (_rollupsDatabaseReadiness is null)
                return false;
            await _rollupsDatabaseReadiness.EnsureReadyAsync(linkedTestCts.Token).ConfigureAwait(false);

            _isDatabaseAvailable = true;
            _failureCount = 0;
            return true;
        }
        catch (OperationCanceledException)
        {
            _isDatabaseAvailable = false;
            return false;
        }
        catch (Exception ex)
        {
            _isDatabaseAvailable = false;
            _failureCount++;
            ILogger.Warning($"⚠️ ObservationRollupWorker failed to establish SQLite connection: {ex.Message}");
            return false;
        }
        finally
        {
            Interlocked.Decrement(ref _isInitializing);
        }
    }

    void TimerCallback(object? state)
    {
        if (LinkedCancellationToken.IsCancellationRequested) return;

        if (!_isDatabaseAvailable) return;

        StartBackground(ct => RunOnceAsync(ct));
    }

    async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        if (!await _gate.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return;

        try
        {
            if (_rollupsDatabaseReadiness is null || _observationRollupRepository is null) return;

            await _rollupsDatabaseReadiness.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

            await _observationRollupRepository.RollupHourAsync(maxBucketsPerRun: 24, cancellationToken).ConfigureAwait(false);
            await _observationRollupRepository.RollupDayAsync(maxBucketsPerRun: 7, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Debug.Assert(false);
        }
        catch (Exception ex)
        {
            ILogger.Warning($"ObservationRollupWorker: rollup run failed: {ex.Message}");
        }
        finally
        {
            try { _gate.Release(); } catch { }
        }
    }

    protected override async Task OnDisposeAsync()
    {
        try
        {
            try { _timer?.Dispose(); } catch { }
            _timer = null;

            try { _reconnectionTimer?.Dispose(); } catch { }
            _reconnectionTimer = null;

            try { await _gate.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false); } catch { }
            try { _gate.Release(); } catch { }
        }
        finally
        {
            _gate.Dispose();
        }

        await Task.CompletedTask;
    }
}
