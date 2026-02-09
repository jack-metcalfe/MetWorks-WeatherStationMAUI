namespace MetWorks.Common.Metrics.Storage;

public sealed class LocalStorageSizeCollector
{
    public LocalStorageSizeSnapshot Capture(
        string? settingsOverrideFilePath,
        string? absoluteLogFilePath,
        string? appDataDirectory,
        string? loggerSqliteDbPath,
        string? readingsSqliteDbPath,
        int topN)
    {
        if (topN <= 0) topN = 10;

        var settingsBytes = TryGetFileLength(settingsOverrideFilePath);

        LocalStorageFileSize? logFile = null;
        if (!string.IsNullOrWhiteSpace(absoluteLogFilePath))
        {
            var bytes = TryGetFileLength(absoluteLogFilePath);
            if (bytes >= 0)
                logFile = new LocalStorageFileSize(absoluteLogFilePath, bytes);
        }

        var loggerSqliteBytes = TryGetFileLength(loggerSqliteDbPath);
        var readingsSqliteBytes = TryGetFileLength(readingsSqliteDbPath);

        IReadOnlyList<LocalStorageFileSize> topLogFiles = Array.Empty<LocalStorageFileSize>();
        try
        {
            // If file logger is configured under AppDataDirectory (it is by convention), scan that directory branch.
            // This surfaces rolled log files too (log-.txt, log-*.txt, etc.).
            if (!string.IsNullOrWhiteSpace(appDataDirectory))
            {
                var root = Path.GetFullPath(appDataDirectory);
                if (Directory.Exists(root))
                {
                    // Heuristic: only include *.txt under any Logs directory.
                    var candidates = Directory.EnumerateFiles(root, "*.txt", SearchOption.AllDirectories)
                        .Where(p => p.IndexOf(Path.DirectorySeparatorChar + "Logs" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0)
                        .Select(p => new LocalStorageFileSize(p, TryGetFileLength(p)))
                        .Where(f => f.Bytes >= 0)
                        .OrderByDescending(f => f.Bytes)
                        .Take(topN)
                        .ToArray();

                    topLogFiles = candidates;
                }
            }
        }
        catch
        {
            // Best-effort metric: ignore directory enumeration failures.
        }

        return new LocalStorageSizeSnapshot(
            SettingsOverrideBytes: settingsBytes,
            LogFile: logFile,
            LoggerSqliteBytes: loggerSqliteBytes,
            ReadingsSqliteBytes: readingsSqliteBytes,
            TopLogFiles: topLogFiles);
    }

    static long TryGetFileLength(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return 0;
            if (!File.Exists(path)) return 0;
            return new FileInfo(path).Length;
        }
        catch
        {
            return 0;
        }
    }
}
