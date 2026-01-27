namespace MetWorks.Common.Logging;

/// <summary>
/// Lightweight wrapper that adds a stable context prefix to all log messages.
/// Used when a concrete logger cannot attach structured properties directly.
/// </summary>
internal sealed class ContextualLogger : ILogger
{
    readonly ILogger _inner;
    readonly string _prefix;

    public ContextualLogger(ILogger inner, string prefix)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _prefix = prefix ?? string.Empty;
    }

    public void Information(string message) => _inner.Information(_prefix + (message ?? string.Empty));
    public void Warning(string message) => _inner.Warning(_prefix + (message ?? string.Empty));
    public void Error(string message, Exception exception) => _inner.Error(_prefix + (message ?? string.Empty), exception);
    public void Error(string message) => _inner.Error(_prefix + (message ?? string.Empty));
    public void Debug(string message) => _inner.Debug(_prefix + (message ?? string.Empty));
    public void Trace(string message) => _inner.Trace(_prefix + (message ?? string.Empty));

    public Exception LogExceptionAndReturn(Exception exception) => _inner.LogExceptionAndReturn(exception);

    public Exception LogExceptionAndReturn(Exception exception, string message) => _inner.LogExceptionAndReturn(exception, _prefix + (message ?? string.Empty));

    public ILogger ForContext(string contextName, object? value)
    {
        if (string.IsNullOrWhiteSpace(contextName)) return this;
        var val = value?.ToString() ?? string.Empty;
        return new ContextualLogger(_inner, _prefix + $"[{contextName}={val}] ");
    }

    public ILogger ForContext(Type sourceType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        return new ContextualLogger(_inner, _prefix + $"[{sourceType.Name}] ");
    }
}
