using System;
using System.IO;
using Xunit;
using MetWorks.Common.Settings;
using MetWorks.Interfaces;

public class InMemoryLogger : ILogger
{
    public void Information(string message) { }
    public void Warning(string message) { }
    public void Error(string message, Exception exception) { }
    public void Error(string message) { }
    public void Debug(string message) { }
    public void Trace(string message) { }
    public Exception LogExceptionAndReturn(Exception exception) => exception;
    public Exception LogExceptionAndReturn(Exception exception, string message) => exception;
}

public class InstanceIdentifierTests : IDisposable
{
    private readonly string _tempDir;
    public InstanceIdentifierTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MetWorks.Settings.Tests." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void GetOrCreateInstallationId_GeneratesAndPersists()
    {
        var provider = new SettingProvider(_tempDir);
        // initialize provider with a logger so it loads definitions
        var logger = new InMemoryLogger();
        provider.InitializeAsync(logger).GetAwaiter().GetResult();

        var idService = new InstanceIdentifier(provider, logger);
        var id1 = idService.GetOrCreateInstallationId();
        Assert.False(string.IsNullOrWhiteSpace(id1));

        // create a new provider pointing at same overrides dir to simulate restart
        var provider2 = new SettingProvider(_tempDir);
        provider2.InitializeAsync(logger).GetAwaiter().GetResult();
        var idService2 = new InstanceIdentifier(provider2, logger);
        var id2 = idService2.GetOrCreateInstallationId();
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void ResetInstallationId_ProducesNewId()
    {
        var provider = new SettingProvider(_tempDir);
        var logger = new InMemoryLogger();
        provider.InitializeAsync(logger).GetAwaiter().GetResult();
        var idService = new InstanceIdentifier(provider, logger);
        var id1 = idService.GetOrCreateInstallationId();

        var ok = idService.ResetInstallationId();
        Assert.True(ok);

        var id2 = idService.GetOrCreateInstallationId();
        Assert.False(string.IsNullOrWhiteSpace(id2));
        Assert.NotEqual(id1, id2);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
