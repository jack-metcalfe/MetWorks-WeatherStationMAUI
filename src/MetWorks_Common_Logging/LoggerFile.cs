namespace MetWorks.Common.Logging;
public class LoggerFile : ILoggerFile
{
    public Task<bool> InitializeAsync(
        ILoggerStub iLoggerStub,
        ISettingRepository iSettingRepository,
        IInstanceIdentifier iInstanceIdentifier,
        IPlatformPaths? platformPaths = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(iLoggerStub);
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

    public ILogger ForContext(string contextName, object? value)
    {
        if (string.IsNullOrWhiteSpace(contextName)) return this;
        return new LoggerFileContext(this, contextName, value);
    }

    public ILogger ForContext(Type sourceType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        return new LoggerFileContext(this, sourceType);
    }

    sealed class LoggerFileContext : ILogger
    {
        readonly LoggerFile _owner;
        readonly Serilog.ILogger _logger;

        public LoggerFileContext(LoggerFile owner, string contextName, object? value)
        {
            ArgumentNullException.ThrowIfNull(owner);
            _owner = owner;
            _logger = owner.ILogger.ForContext(contextName, value, destructureObjects: true);
        }

        public LoggerFileContext(LoggerFile owner, Type sourceType)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(sourceType);
            _owner = owner;
            _logger = owner.ILogger.ForContext(sourceType);
        }

        public void Information(string message) => _logger.Information(message);
        public void Warning(string message) => _logger.Warning(message);
        public void Error(string message, Exception exception) => _logger.Error(exception, message);
        public void Error(string message) => _logger.Error(message);
        public void Debug(string message) => _logger.Debug(message);
        public void Trace(string message) => _logger.Verbose(message);

        public Exception LogExceptionAndReturn(Exception exception) => _owner.LogExceptionAndReturn(exception);

        public Exception LogExceptionAndReturn(Exception exception, string message) => _owner.LogExceptionAndReturn(exception, message);

        public ILogger ForContext(string contextName, object? value)
        {
            if (string.IsNullOrWhiteSpace(contextName)) return this;
            return new SerilogContextLogger(_owner, _logger.ForContext(contextName, value, destructureObjects: true));
        }

        public ILogger ForContext(Type sourceType)
        {
            ArgumentNullException.ThrowIfNull(sourceType);
            return new SerilogContextLogger(_owner, _logger.ForContext(sourceType));
        }

        sealed class SerilogContextLogger : ILogger
        {
            readonly LoggerFile _owner;
            readonly Serilog.ILogger _logger;

            public SerilogContextLogger(LoggerFile owner, Serilog.ILogger logger)
            {
                ArgumentNullException.ThrowIfNull(owner);
                ArgumentNullException.ThrowIfNull(logger);
                _owner = owner;
                _logger = logger;
            }

            public void Information(string message) => _logger.Information(message);
            public void Warning(string message) => _logger.Warning(message);
            public void Error(string message, Exception exception) => _logger.Error(exception, message);
            public void Error(string message) => _logger.Error(message);
            public void Debug(string message) => _logger.Debug(message);
            public void Trace(string message) => _logger.Verbose(message);

            public Exception LogExceptionAndReturn(Exception exception) => _owner.LogExceptionAndReturn(exception);

            public Exception LogExceptionAndReturn(Exception exception, string message) => _owner.LogExceptionAndReturn(exception, message);

            public ILogger ForContext(string contextName, object? value)
            {
                if (string.IsNullOrWhiteSpace(contextName)) return this;
                return new SerilogContextLogger(_owner, _logger.ForContext(contextName, value, destructureObjects: true));
            }

            public ILogger ForContext(Type sourceType)
            {
                ArgumentNullException.ThrowIfNull(sourceType);
                return new SerilogContextLogger(_owner, _logger.ForContext(sourceType));
            }
        }
    }
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
