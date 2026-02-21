using MetWorks.Interfaces;

namespace MetWorks.Ingest.Transformer.Tests.Fakes;

sealed class TestLogger : ILogger
{
    readonly string _context;

    public TestLogger(string context = "test")
    {
        _context = context;
    }

    public void Information(string message) { }
    public void Warning(string message) { }
    public void Warning(string message, Exception exception) { }
    public void Error(string message, Exception exception) { }
    public void Error(string message) { }
    public void Debug(string message) { }
    public void Trace(string message) { }

    public Exception LogExceptionAndReturn(Exception exception)
        => exception;

    public Exception LogExceptionAndReturn(Exception exception, string message)
        => exception;

    public ILogger ForContext(string contextName, object? value)
        => new TestLogger($"{_context}:{contextName}");

    public ILogger ForContext(Type sourceType)
        => new TestLogger($"{_context}:{sourceType.Name}");
}
