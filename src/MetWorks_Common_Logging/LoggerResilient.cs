namespace MetWorks.Common.Logging;
/// <summary>
/// Resilient logger that fans out to multiple ILogger instances, and buffers messages
/// when no live logger is available. When a new logger is added the buffer is flushed.
/// Non-blocking and tolerant of logger failures.
/// </summary>
public sealed class LoggerResilient : ServiceBase, ILoggerResilient
{
    readonly ConcurrentQueue<LogEntry> _buffer = new();
    int _maxBufferSize = 1000;
    int _initGuard = 0; // Interlocked guard for InitializeAsync
    readonly ReaderWriterLockSlim _loggersLock = new();
    readonly List<ILogger> _loggers = new();

    // Background worker signal to wake flushing loop
    readonly SemaphoreSlim _signal = new(0);

    // A simple fallback stub to ensure calls never NRE when nothing registered.
    ILogger _fallbackLogger = new LoggerStub();

    record LogEntry(LogLevel Level, string Message, Exception? Exception);

    enum LogLevel { Information, Warning, Error, Debug, Trace }

    // Parameterless constructor by design. Call InitializeAsync to configure runtime dependencies.
    public LoggerResilient()
    {
    }

    /// <summary>
    /// Async initialization separate from construction. Call once to wire logger, settings and start background worker.
    /// </summary>
    public Task<bool> InitializeAsync(
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier? iInstanceIdentifier = null,
        ILoggerStub? iLoggerStub = null,
        ILoggerFile? iLoggerFile = null,
        ILoggerPostgreSQL? iLoggerPostgreSQL = null,
        ILoggerSQLite? iLoggerSQLite = null,
        int? maxBufferSize = null,
        CancellationToken cancellationToken = default
    )
    {
        // Guard against concurrent initialize using Interlocked to prevent races
        if (Interlocked.CompareExchange(ref _initGuard, 1, 0) != 0)
            return Task.FromResult(true);

        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);

        try
        {
            if (maxBufferSize.HasValue) _maxBufferSize = Math.Max(1, maxBufferSize.Value);

            // Use ServiceBase helper to wire logger, settings and linked cancellation
            var selectedLogger = iLoggerStub ?? _fallbackLogger;

            if (iLoggerPostgreSQL is not null)
                selectedLogger = iLoggerPostgreSQL;
            else if (iLoggerFile is not null)
                selectedLogger = iLoggerFile;
            else if (iLoggerStub is not null)
                selectedLogger = iLoggerStub;

            AddLogger(selectedLogger);

            if (iLoggerSQLite is not null)
                AddLogger(iLoggerSQLite);

            InitializeBase(
                this,
                iSettingRepository,
                iEventRelayBasic,
                cancellationToken
            );

            // Start background worker using ServiceBase StartBackground which tracks tasks and honors linked cancellation
            StartBackground(async token => await WorkerLoopAsync(token).ConfigureAwait(false));

            // Mark service ready after worker loop has been started
            try { MarkReady(); } catch { }

            return Task.FromResult(true);
        }
        catch
        {
            // Reset guard so a retry can be attempted
            Interlocked.Exchange(ref _initGuard, 0);
            throw;
        }
    }

    /// <summary>
    /// Add a logger to the fan-out list. Immediately attempts to flush any buffered messages.
    /// </summary>
    public void AddLogger(ILogger logger)
    {
        if (logger == null) return;
        _loggersLock.EnterWriteLock();
        try
        {
            _loggers.Add(logger);
        }
        finally
        {
            _loggersLock.ExitWriteLock();
        }

        // Try flush in background to avoid blocking caller
        // Signal the background worker to attempt a flush.
        try { _signal.Release(); } catch { }
    }

    /// <summary>
    /// Remove a logger from the fan-out list.
    /// </summary>
    public void RemoveLogger(ILogger logger)
    {
        if (logger == null) return;
        _loggersLock.EnterWriteLock();
        try
        {
            _loggers.RemoveAll(l => ReferenceEquals(l, logger));
        }
        finally
        {
            _loggersLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Flush buffered entries to current loggers. Best-effort: entries that fail are re-enqueued up to buffer capacity.
    /// </summary>
    public void FlushBuffer()
    {
        // If no loggers available, nothing to flush
        if (!HasLiveLoggers()) return;

        var drained = new List<LogEntry>();
        while (_buffer.TryDequeue(out var entry))
        {
            drained.Add(entry);
        }

        foreach (var entry in drained)
        {
            var ok = TryDispatch(entry);
            if (!ok) EnqueueBuffered(entry);
        }
    }

    async Task WorkerLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Wait to be signalled, or wake periodically to attempt flush
                    await _signal.WaitAsync(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }

                // Attempt to flush buffered entries
                try
                {
                    FlushBuffer();
                }
                catch
                {
                    // swallow - best effort
                }
            }
        }
        finally
        {
            // On cancellation drain once more (best-effort)
            try
            {
                while (_buffer.TryDequeue(out var entry))
                {
                    TryDispatch(entry);
                }
            }
            catch { }
        }
    }

    bool HasLiveLoggers()
    {
        _loggersLock.EnterReadLock();
        try { return _loggers.Count > 0; }
        finally { _loggersLock.ExitReadLock(); }
    }

    bool TryDispatch(LogEntry entry)
    {
        bool anySucceeded = false;

        _loggersLock.EnterReadLock();
        try
        {
            if (_loggers.Count == 0) return false;

            foreach (var logger in _loggers.ToArray())
            {
                try
                {
                    DispatchTo(logger, entry);
                    anySucceeded = true;
                }
                catch
                {
                    // swallow
                }
            }
        }
        finally
        {
            _loggersLock.ExitReadLock();
        }

        return anySucceeded;
    }

    void DispatchTo(ILogger logger, LogEntry entry)
    {
        switch (entry.Level)
        {
            case LogLevel.Information: logger.Information(entry.Message); break;
            case LogLevel.Warning: logger.Warning(entry.Message); break;
            case LogLevel.Error:
                if (entry.Exception is not null) logger.Error(entry.Message, entry.Exception);
                else logger.Error(entry.Message);
                break;
            case LogLevel.Debug: logger.Debug(entry.Message); break;
            case LogLevel.Trace: logger.Trace(entry.Message); break;
        }
    }

    void EnqueueBuffered(LogEntry entry)
    {
        while (_buffer.Count >= _maxBufferSize) _buffer.TryDequeue(out _);
        _buffer.Enqueue(entry);
        try { _signal.Release(); } catch { }
    }

    void BufferOrDispatch(LogEntry entry)
    {
        try
        {
            if (!TryDispatch(entry)) EnqueueBuffered(entry);
        }
        catch
        {
            EnqueueBuffered(entry);
        }
    }

    // ILogger implementation: all methods are non-blocking and resilient.
    public void Information(string message) => BufferOrDispatch(new LogEntry(LogLevel.Information, message ?? string.Empty, null));
    public void Warning(string message) => BufferOrDispatch(new LogEntry(LogLevel.Warning, message ?? string.Empty, null));
    public void Error(string message, Exception exception) => BufferOrDispatch(new LogEntry(LogLevel.Error, message ?? string.Empty, exception));
    public void Error(string message) => BufferOrDispatch(new LogEntry(LogLevel.Error, message ?? string.Empty, null));
    public void Debug(string message) => BufferOrDispatch(new LogEntry(LogLevel.Debug, message ?? string.Empty, null));
    public void Trace(string message) => BufferOrDispatch(new LogEntry(LogLevel.Trace, message ?? string.Empty, null));

    /// <summary>
    /// Returns a logger that enriches all log entries with the provided context.
    /// For the resilient fan-out logger this is implemented as a lightweight prefix wrapper
    /// to avoid per-sink context fan-out complexity.
    /// </summary>
    public ILogger ForContext(string contextName, object? value)
    {
        if (string.IsNullOrWhiteSpace(contextName)) return this;
        return new ContextualLogger(this, $"[{contextName}={value}] ");
    }

    /// <summary>
    /// Returns a logger that enriches all log entries with the provided source type.
    /// </summary>
    public ILogger ForContext(Type sourceType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        return new ContextualLogger(this, $"[{sourceType.Name}] ");
    }

    public Exception LogExceptionAndReturn(Exception exception)
    {
        if (exception == null) return new ArgumentNullException(nameof(exception));
        var entry = new LogEntry(LogLevel.Error, exception.Message, exception);
        BufferOrDispatch(entry);
        return exception;
    }

    public Exception LogExceptionAndReturn(Exception exception, string message)
    {
        if (exception == null) return new ArgumentNullException(nameof(exception));
        var entry = new LogEntry(LogLevel.Error, message ?? exception.Message, exception);
        BufferOrDispatch(entry);
        return exception;
    }

    /// <summary>
    /// Request graceful shutdown: cancel background worker(s) and wait for them to drain.
    /// Returns true if background tasks completed within the timeout.
    /// </summary>
    public async Task<bool> ShutdownAsync(TimeSpan timeout)
    {
        try
        {
            if (!_isInitialized) return true;

            // Cancel local work and wait for background tasks tracked by ServiceBase
            try { LocalCancellationTokenSource.Cancel(); } catch { }

            await WaitForBackgroundTasksAsync(timeout).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            try { _signal.Dispose(); } catch { }
        }
    }
}