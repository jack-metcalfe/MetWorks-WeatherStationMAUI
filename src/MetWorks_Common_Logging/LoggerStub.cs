namespace MetWorks.Common.Logging;

/// <summary>
/// Bootstrap logger that writes to <see cref="System.Diagnostics.Debug"/> and buffers
/// entries so they can be replayed into a persistent logger via <see cref="DrainTo"/>.
/// Used during the early startup phase before file/SQLite sinks are initialized.
/// </summary>
public sealed class LoggerStub : ILoggerStub
{
    readonly ConcurrentQueue<BufferedEntry> _buffer = new();
    int _drained; // 0 = active, 1 = drained (entries go to Debug only after drain)

    record BufferedEntry(Level Lvl, string Message, Exception? Exception);
    enum Level { Information, Warning, Error, Debug, Trace }

    public LoggerStub() { }

    /// <inheritdoc />
    public void DrainTo(ILogger target)
    {
        ArgumentNullException.ThrowIfNull(target);

        // Mark drained so new entries after this point skip buffering
        Interlocked.Exchange(ref _drained, 1);

        while (_buffer.TryDequeue(out var entry))
        {
            try { Dispatch(target, entry); }
            catch { /* best-effort replay */ }
        }
    }

    public void Information(string message) { Emit(Level.Information, message, null); }
    public void Warning(string message) { Emit(Level.Warning, message, null); }
    public void Warning(string message, Exception exception) { Emit(Level.Warning, message, exception); }
    public void Error(string message, Exception exception) { Emit(Level.Error, message, exception); }
    public void Error(string message) { Emit(Level.Error, message, null); }
    public void Debug(string message) { Emit(Level.Debug, message, null); }
    public void Trace(string message) { Emit(Level.Trace, message, null); }

    public Exception LogExceptionAndReturn(Exception exception)
    {
        if (exception is null) return new ArgumentNullException(nameof(exception));
        Emit(Level.Error, exception.Message, exception);
        return exception;
    }

    public Exception LogExceptionAndReturn(Exception exception, string message)
    {
        if (exception is null) return new ArgumentNullException(nameof(exception));
        Emit(Level.Error, message ?? exception.Message, exception);
        return exception;
    }

    public ILogger ForContext(string contextName, object? value) => this;
    public ILogger ForContext(Type sourceType) => this;

    void Emit(Level level, string message, Exception? exception)
    {
        var text = message ?? string.Empty;

        // Always write to the debug output window for live visibility
        if (exception is not null)
            System.Diagnostics.Debug.WriteLine($"[LoggerStub:{level}] {text} — {exception}");
        else
            System.Diagnostics.Debug.WriteLine($"[LoggerStub:{level}] {text}");

        // Buffer for replay only while we haven't been drained yet
        if (Volatile.Read(ref _drained) == 0)
            _buffer.Enqueue(new BufferedEntry(level, text, exception));
    }

    static void Dispatch(ILogger target, BufferedEntry entry)
    {
        switch (entry.Lvl)
        {
            case Level.Information: target.Information(entry.Message); break;
            case Level.Warning:
                if (entry.Exception is not null) target.Warning(entry.Message, entry.Exception);
                else target.Warning(entry.Message);
                break;
            case Level.Error:
                if (entry.Exception is not null) target.Error(entry.Message, entry.Exception);
                else target.Error(entry.Message);
                break;
            case Level.Debug: target.Debug(entry.Message); break;
            case Level.Trace: target.Trace(entry.Message); break;
        }
    }
}