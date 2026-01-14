namespace Logging;

/// <summary>
/// Stub implementation of IFileLogger that discards all log messages.
/// Used during bootstrap phase before the real logger is initialized.
/// This is a no-op stub that satisfies the IFileLogger interface contract.
/// </summary>
public sealed class LoggerStub : Interfaces.ILogger
{
    public Task<bool> InitializeAsync(
        int fileSizeLimitBytes,
        string minimumLevel,
        string outputTemplate,
        string relativeLogPath,
        int retainedFileCountLimit,
        string rollingInterval,
        bool rollOnFileSizeLimit)
    {
        return Task.FromResult(true);
    }

    public void Information(string message) { }
    public void Warning(string message) { }
    public void Error(string message, Exception exception) { }
    public void Error(string message) { }
    public void Debug(string message) { }
    public void Trace(string message) { }
    
    public Exception LogExceptionAndReturn(Exception exception) => exception;
    public Exception LogExceptionAndReturn(Exception exception, string message) => exception;
}