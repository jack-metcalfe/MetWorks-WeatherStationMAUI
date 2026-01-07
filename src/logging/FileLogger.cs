namespace Logging;
public class FileLogger : IFileLogger
{
    public static IFileLogger CreateFileLoggerWithDefaults()
    {
        FileLogger fileLogger = new FileLogger();
        var result = fileLogger.InitializeAsync(
            fileSizeLimitBytes: 10485760,
            minimumLevel: "Information",
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            path: @"C:/Temp/log-.txt",
            retainedFileCountLimit: 31,
            rollingInterval: "Day",
            rollOnFileSizeLimit: true
            ).GetAwaiter().GetResult();

        return fileLogger;
    }
    Serilog.Core.Logger? _logger = null;
    Serilog.Core.Logger ILogger
    {
        get
        {
            if (!IsReady)
                throw new InvalidOperationException("Logger is not initialized.");

            return _logger!;
        }
    }
    private bool IsReady => _logger is not null;
    public FileLogger()
    {
    }
    public async Task<bool> InitializeAsync(
        Int32 fileSizeLimitBytes,
        String minimumLevel,
        String outputTemplate,
        String path,
        Int32 retainedFileCountLimit,
        String rollingInterval,
        Boolean rollOnFileSizeLimit
    )
    {
        if (IsReady)
            throw new InvalidOperationException("Logger is already initialized.");

        try
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Is(ParseLevel(minimumLevel))
                .WriteTo.File(
                    path: path,
                    rollingInterval: Enum.Parse<RollingInterval>(rollingInterval),
                    fileSizeLimitBytes: fileSizeLimitBytes,
                    rollOnFileSizeLimit: rollOnFileSizeLimit,
                    retainedFileCountLimit: retainedFileCountLimit,
                    outputTemplate: outputTemplate
                )
                .CreateLogger();
        }

        catch (Exception exception)
        {
            throw new InvalidOperationException("Failed to read settings configuration from the provided path.", exception);
        }

        return await Task.FromResult(true);
    }

    public void Information(string message)
    {
        ILogger.Information(message);
    }
    public void Warning(string message)
    {
        ILogger.Warning(message);
    }
    public void Error(string message, Exception exception)
    {
        ILogger.Error(message, exception);
    }
    public void Error(string message)
    {
        ILogger.Error(message);
    }
    public Exception LogExceptionAndReturn(Exception exception)
    {
        ILogger.Error("An error occurred", exception);
        return exception;
    }
    public void Debug(string message)
    {
        ILogger.Debug(message);
    }

    public void Trace(string message)
    {
        ILogger.Verbose(message);
    }

    private static LogEventLevel ParseLevel(string level) =>
        Enum.TryParse<LogEventLevel>(level, true, out var parsed)
            ? parsed
            : LogEventLevel.Information;

    public Exception LogExceptionAndReturn(Exception exception, string message)
    {
        ILogger.Error(message, exception);
        return exception;
    }
}
