namespace Logging;

using Serilog.Core;
public class LoggerFile : ILogger
{
    bool _isInitialized = false;

    Logger? _iLogger = null;
    Logger ILogger
    {
        get => NullPropertyGuard.Get(_isInitialized, _iLogger, nameof(ILogger));
        set => _iLogger = value;
    }

    string? _absoluteLogFilePath = null;
    public string AbsoluteLogFilePath
    {
        get => NullPropertyGuard.Get(
            _isInitialized,
            _absoluteLogFilePath,
            nameof(AbsoluteLogFilePath)
        );
        private set => _absoluteLogFilePath = value;
    }
    public LoggerFile()
    {
    }
    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository
    )
    {
        if (_isInitialized)
            throw new InvalidOperationException("Logger is already initialized.");

        try
        {
            var fileSizeLimitBytes = iSettingRepository.GetValueOrDefault<int>(
                    LoggerFileGroupSettingsDefinition.BuildSettingPath(LoggerFile_fileSizeLimitBytes)
                );
            var minimumLevel = iSettingRepository
                .GetValueOrDefault<string>(
                    LoggerFileGroupSettingsDefinition.BuildSettingPath(LoggerFile_minimumLevel)
                );
            var outputTemplate = iSettingRepository.GetValueOrDefault<string>(
                    LoggerFileGroupSettingsDefinition.BuildSettingPath(LoggerFile_outputTemplate)
                );
            var relativeLogPath = iSettingRepository.GetValueOrDefault<string>(
                    LoggerFileGroupSettingsDefinition.BuildSettingPath(LoggerFile_relativeLogPath)
                );
            var retainedFileCountLimit = iSettingRepository.GetValueOrDefault<int>(
                    LoggerFileGroupSettingsDefinition.BuildSettingPath(LoggerFile_retainedFileCountLimit)
                );
            var rollingInterval = iSettingRepository.GetValueOrDefault<string>(
                    LoggerFileGroupSettingsDefinition.BuildSettingPath(LoggerFile_rollingInterval)
                );
            var rollOnFileSizeLimit = iSettingRepository.GetValueOrDefault<bool>(
                    LoggerFileGroupSettingsDefinition.BuildSettingPath(LoggerFile_rollOnFileSizeLimit)
                );

            var absoluteLogFilePath = Path.Combine(FileSystem.AppDataDirectory, relativeLogPath);

            ILogger = new LoggerConfiguration()
                .MinimumLevel.Is(ParseLevel(minimumLevel))
                .WriteTo.File(
                    fileSizeLimitBytes: fileSizeLimitBytes,
                    outputTemplate: outputTemplate,
                    path: absoluteLogFilePath,
                    rollingInterval: Enum.Parse<RollingInterval>(rollingInterval),
                    rollOnFileSizeLimit: rollOnFileSizeLimit,
                    retainedFileCountLimit: retainedFileCountLimit
                )
                .CreateLogger();
            AbsoluteLogFilePath = absoluteLogFilePath;
            _isInitialized = true;
            ILogger.Information(@"FileLogger initialized");
        }

        catch (Exception exception)
        {
            throw new InvalidOperationException("Failed to read settings configuration from the provided path.", exception);
        }

        return await Task.FromResult(true);
    }

    public void Information(string message) => ILogger.Information(message);
    public void Warning(string message) => ILogger.Warning(message);
    public void Error(string message, Exception exception) => ILogger.Error(message, exception);
    public void Error(string message) => ILogger.Error(message);
    public void Debug(string message) => ILogger.Debug(message);
    public void Trace(string message) => ILogger.Verbose(message);
    private static LogEventLevel ParseLevel(string level) =>
        Enum.TryParse<LogEventLevel>(level, true, out var parsed)
            ? parsed
            : LogEventLevel.Information;
    public Exception LogExceptionAndReturn(Exception exception)
    {
        ILogger.Error("An error occurred", exception);
        return exception;
    }
    public Exception LogExceptionAndReturn(Exception exception, string message)
    {
        ILogger.Error(message, exception);
        return exception;
    }
}
