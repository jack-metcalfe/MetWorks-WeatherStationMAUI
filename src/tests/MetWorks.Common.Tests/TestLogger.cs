namespace MetWorks.Models.Observables.Provenance.Common.Tests;
class TestLogger : ILogger
{
    public readonly ConcurrentQueue<string> Messages = new();

    public void Information(string message) => Messages.Enqueue("I:" + message);
    public void Warning(string message) => Messages.Enqueue("W:" + message);
    public void Error(string message, System.Exception exception) => Messages.Enqueue("E:" + message + "|" + exception?.Message);
    public void Error(string message) => Messages.Enqueue("E:" + message);
    public void Debug(string message) => Messages.Enqueue("D:" + message);
    public void Trace(string message) => Messages.Enqueue("T:" + message);
    public System.Exception LogExceptionAndReturn(System.Exception exception) { Messages.Enqueue("EX:" + exception.Message); return exception; }
    public System.Exception LogExceptionAndReturn(System.Exception exception, string message) { Messages.Enqueue("EXM:" + message); return exception; }
}

public class LoggerResilientTests
{
    [Fact]
    public async Task Buffers_When_No_Loggers_And_Flushes_When_Added()
    {
        var resilient = new LoggerResilient(maxBufferSize: 100);
        resilient.Information("one");
        resilient.Warning("two");

        var backend = new TestLogger();
        resilient.AddLogger(backend);

        // give background flush a moment
        await Task.Delay(200);

        Assert.Contains("I:one", backend.Messages);
        Assert.Contains("W:two", backend.Messages);
    }
}