using System;
using System.Collections.Concurrent;
using MetWorks.Interfaces;

namespace MetWorks.Models.Observables.Provenance.Common.Tests;

class TestLogger : ILoggerStub
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

    public ILogger ForContext(string contextName, object? value) => this;

    public ILogger ForContext(Type sourceType) => this;
}

public class LoggerResilientTests
{
    [Fact]
    public async Task WhenBackendAddedThenSubsequentLogsReachBackend()
    {
        var resilient = new LoggerResilient();
        var eventRelay = new MetWorks.EventRelay.EventRelayBasic();
        var stub = new TestLogger();
        await resilient.InitializeAsync(
            iSettingRepository: new MetWorks.Common.Tests.InMemorySettingRepository(new System.Collections.Generic.Dictionary<string, string>()),
            iEventRelayBasic: eventRelay,
            iLoggerStub: stub,
            maxBufferSize: 100
        );

        var backend = new TestLogger();
        resilient.AddLogger(backend);

        resilient.Information("one");
        resilient.Warning("two");

        // give background flush a moment
        await Task.Delay(200);

        Assert.Contains("I:one", backend.Messages);
        Assert.Contains("W:two", backend.Messages);
    }

    [Fact]
    public void ForContext_Prefixes_Messages()
    {
        var resilient = new LoggerResilient();
        var backend = new TestLogger();
        resilient.AddLogger(backend);

        var ctx = resilient.ForContext(typeof(LoggerResilientTests));
        ctx.Information("hello");

        Assert.Contains(backend.Messages, m => m == $"I:[{nameof(LoggerResilientTests)}] hello");
    }
}