using System;
using System.IO;
using Xunit;
using MetWorks.Common.Settings;
using MetWorks.Interfaces;

public class SettingProviderSaveTests : IDisposable
{
    private readonly string _tempDir;
    public SettingProviderSaveTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MetWorks.Settings.Save.Tests." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void SaveValueOverride_CreatesOverrideFileAndPersistsValue()
    {
        var provider = new SettingProvider(_tempDir);
        var logger = new TestLogger();
        provider.InitializeAsync(logger).GetAwaiter().GetResult();

        var path = "/services/instance/installationId";
        var val = Guid.NewGuid().ToString();
        var ok = provider.SaveValueOverride(path, val);
        Assert.True(ok);

        // create a new provider to read the override
        var provider2 = new SettingProvider(_tempDir);
        provider2.InitializeAsync(logger).GetAwaiter().GetResult();
        var read = provider2.ISettingValueDictionary[path].Value;
        Assert.Equal(val, read);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}

// Minimal test logger used for initialization
class TestLogger : ILogger
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
