namespace MetWorks.Interfaces;
public interface ILogger
{
    void Information(string message);
    void Warning(string message);
    void Error(string message, Exception exception);
    void Error(string message);
    void Debug(string message);
    void Trace(string message);
    Exception LogExceptionAndReturn(Exception exception);
    Exception LogExceptionAndReturn(Exception exception, string message);
    /// <summary>
    /// Returns a logger that enriches all log entries with the provided context.
    /// Intended to mirror Serilog's ForContext behavior without exposing Serilog types.
    /// </summary>
    ILogger ForContext(string contextName, object? value);

    /// <summary>
    /// Returns a logger that enriches all log entries with the provided source type.
    /// </summary>
    ILogger ForContext(Type sourceType);
}