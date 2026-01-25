using System.IO;
using MetWorks.Common.Logging;
using MetWorks.Interfaces;
using Xunit;

public class FakePlatformPaths : IPlatformPaths
{
    public string AppDataDirectory { get; init; }
    public FakePlatformPaths(string dir) => AppDataDirectory = dir;
}

public class InMemorySettingRepository : ISettingRepository
{
    readonly Dictionary<string,string> _values = new();
    public InMemorySettingRepository(Dictionary<string,string> values) => _values = values;
    public string? GetValueOrDefault(string path) => _values.TryGetValue(path, out var v) ? v : null;
    public T GetValueOrDefault<T>(string path) => (T)Convert.ChangeType(GetValueOrDefault(path), typeof(T));
    public IEnumerable<ISettingDefinition> GetAllDefinitions() => Enumerable.Empty<ISettingDefinition>();
    public IEnumerable<ISettingValue> GetAllValues() => Enumerable.Empty<ISettingValue>();
    public void RegisterForSettingChangeMessages(string path, Action<ISettingValue> handler) { }
    public IEventRelayPath IEventRelayPath => throw new NotImplementedException();
}

public class LoggerFileTests
{
    [Fact]
    public async Task Initialize_Uses_Provided_PlatformPaths()
    {
        var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(temp);

        var settings = new Dictionary<string,string>
        {
            { "/services/logging/loggerFile/relativeLogPath", "logs/test.log" },
            { "/services/logging/loggerFile/fileSizeLimitBytes", "1048576" },
            { "/services/logging/loggerFile/minimumLevel", "Information" },
            { "/services/logging/loggerFile/outputTemplate", "{Timestamp:yyyy-MM-dd} {Level} {Message}" },
            { "/services/logging/loggerFile/retainedFileCountLimit", "2" },
            { "/services/logging/loggerFile/rollingInterval", "Day" },
            { "/services/logging/loggerFile/rollOnFileSizeLimit", "true" }
        };

        var repo = new InMemorySettingRepository(settings);
        var loggerStub = new MetWorks.Common.Logging.LoggerStub();
        var fileLogger = new MetWorks.Common.Logging.LoggerFile();

        var fake = new FakePlatformPaths(temp);
        var ok = await fileLogger.InitializeAsync(loggerStub, repo, fake);

        Assert.True(ok);
        Assert.StartsWith(temp, fileLogger.AbsoluteLogFilePath);

        // cleanup
        try { Directory.Delete(temp, true); } catch { }
    }
}
