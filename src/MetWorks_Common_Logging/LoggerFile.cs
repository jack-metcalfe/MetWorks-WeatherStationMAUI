namespace MetWorks.Common.Logging;
public class LoggerFile : ILogger
{
    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IInstanceIdentifier iInstanceIdentifier,
        IPlatformPaths? platformPaths = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);

        if (_isInitialized)
            throw new InvalidOperationException($"{nameof(LoggerFile)} is already initialized.");

        if (cancellationToken.IsCancellationRequested) return Task.FromResult(false);

        try
        {
            var fileSizeLimitBytes = iSettingRepository.GetValueOrDefault<int>(
                LookupDictionaries.LoggerFileGroupSettingsDefinition.BuildSettingPath(SettingConstants.LoggerFile_fileSizeLimitBytes)
            );

            var minimumLevel = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.LoggerFileGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerFile_minimumLevel
                )
            );

            var outputTemplate = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.LoggerFileGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerFile_outputTemplate
                )
            );

            var relativeLogPath = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.LoggerFileGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerFile_relativeLogPath
                )
            );

            var retainedFileCountLimit = iSettingRepository.GetValueOrDefault<int>(
                LookupDictionaries.LoggerFileGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerFile_retainedFileCountLimit
                )
            );

            var rollingInterval = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.LoggerFileGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerFile_rollingInterval
                )
            );

            var rollOnFileSizeLimit = iSettingRepository.GetValueOrDefault<bool>(
                LookupDictionaries.LoggerFileGroupSettingsDefinition.BuildSettingPath(
                    SettingConstants.LoggerFile_rollOnFileSizeLimit
                )
            );

            var paths = platformPaths ?? new DefaultPlatformPaths();

            if (string.IsNullOrWhiteSpace(relativeLogPath))
                throw new ArgumentException("relativeLogPath setting is required", nameof(relativeLogPath));

            // Combine and resolve to a full path, then ensure it stays inside the AppDataDirectory.
            var candidate = Path.Combine(paths.AppDataDirectory, relativeLogPath);
            var absoluteLogFilePath = Path.GetFullPath(candidate);
            var appDataFull = Path.GetFullPath(paths.AppDataDirectory);

            // Prevent path traversal escaping the app data directory
            if (!absoluteLogFilePath.StartsWith(appDataFull, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Resolved log path escapes the application data directory.");

            // Ensure directory exists
            var dir = Path.GetDirectoryName(absoluteLogFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var loggerCfg = new LoggerConfiguration()
                .MinimumLevel.Is(ParseLevel(minimumLevel));

            // Enrich logs with installation id when available
            try
            {
                var installationId = iInstanceIdentifier?.GetOrCreateInstallationId();
                if (!string.IsNullOrWhiteSpace(installationId))
                {
                    loggerCfg = loggerCfg.Enrich.WithProperty("InstallationId", installationId);
                }
            }
            catch
            {
                // Ignore enrichment failures - logging should not block initialization
            }

            loggerCfg = loggerCfg.WriteTo.File(
                    fileSizeLimitBytes: fileSizeLimitBytes,
                    outputTemplate: outputTemplate,
                    path: absoluteLogFilePath,
                    rollingInterval: Enum.TryParse<RollingInterval>(rollingInterval, true, out var parsedRolling) ? parsedRolling : RollingInterval.Day,
                    rollOnFileSizeLimit: rollOnFileSizeLimit,
                    retainedFileCountLimit: retainedFileCountLimit
                );

            ILogger = loggerCfg.CreateLogger();
            AbsoluteLogFilePath = absoluteLogFilePath;
            _isInitialized = true;
            ILogger.Information(@"FileLogger initialized");
        }

        catch (OperationCanceledException)
        {
            return Task.FromResult(false);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Failed to read settings configuration from the provided path.", exception);
        }

        return Task.FromResult(true);
    }

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
