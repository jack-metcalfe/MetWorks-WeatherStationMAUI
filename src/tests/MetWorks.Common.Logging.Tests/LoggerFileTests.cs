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
    public T GetValueOrDefault<T>(string path)
    {
        var raw = GetValueOrDefault(path);
        if (raw is null)
            return default!;

        if (typeof(T) == typeof(bool) && bool.TryParse(raw, out var b))
            return (T)(object)b;

        if (typeof(T).IsEnum && Enum.TryParse(typeof(T), raw, ignoreCase: true, out var parsed))
            return (T)parsed;

        return (T)Convert.ChangeType(raw, typeof(T));
    }
    public IEnumerable<ISettingDefinition> GetAllDefinitions() => Enumerable.Empty<ISettingDefinition>();
    public IEnumerable<ISettingValue> GetAllValues() => Enumerable.Empty<ISettingValue>();
    public void RegisterForSettingChangeMessages(string path, Action<ISettingValue> handler) { }
    public IEventRelayPath IEventRelayPath => throw new NotImplementedException();
}

sealed class TestInstanceIdentifier : IInstanceIdentifier
{
    public string GetOrCreateInstallationId() => Guid.Empty.ToString();
    public bool SetInstallationId(string installationId) => true;
    public bool ResetInstallationId() => true;
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
            { "/services/loggerFile/relativeLogPath", "logs/test.log" },
            { "/services/loggerFile/fileSizeLimitBytes", "1048576" },
            { "/services/loggerFile/minimumLevel", "Information" },
            { "/services/loggerFile/outputTemplate", "{Timestamp:yyyy-MM-dd} {Level} {Message}" },
            { "/services/loggerFile/retainedFileCountLimit", "2" },
            { "/services/loggerFile/rollingInterval", "Day" },
            { "/services/loggerFile/rollOnFileSizeLimit", "true" }
        };

        var repo = new InMemorySettingRepository(settings);
        var loggerStub = new MetWorks.Common.Logging.LoggerStub();
        var fileLogger = new MetWorks.Common.Logging.LoggerFile();
        var instanceId = new TestInstanceIdentifier();

        var fake = new FakePlatformPaths(temp);
        var ok = await fileLogger.InitializeAsync(loggerStub, repo, instanceId, platformPaths: fake);

        Assert.True(ok);
        Assert.StartsWith(temp, fileLogger.AbsoluteLogFilePath);

        // cleanup
        try { Directory.Delete(temp, true); } catch { }
    }
}
