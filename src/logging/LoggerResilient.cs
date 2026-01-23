namespace MetWorks.Common.Logging;
/// <summary>
/// Resilient logger that fans out to multiple ILogger instances, and buffers messages
/// when no live logger is available. When a new logger is added the buffer is flushed.
/// Non-blocking and tolerant of logger failures.
/// </summary>
public sealed class LoggerResilient : ILogger
{
    readonly ConcurrentQueue<LogEntry> _buffer = new();
    readonly int _maxBufferSize;
    readonly ReaderWriterLockSlim _loggersLock = new();
    readonly List<ILogger> _loggers = new();

    // A simple fallback stub to ensure calls never NRE when nothing registered.
    readonly ILogger _fallbackLogger;

    record LogEntry(LogLevel Level, string Message, Exception? Exception);

    enum LogLevel { Information, Warning, Error, Debug, Trace }

    public LoggerResilient(ILogger? fallback = null, int maxBufferSize = 1000)
    {
        _fallbackLogger = fallback ?? new LoggerStub();
        _maxBufferSize = Math.Max(1, maxBufferSize);
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
        ThreadPool.QueueUserWorkItem(_ => FlushBuffer());
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
}