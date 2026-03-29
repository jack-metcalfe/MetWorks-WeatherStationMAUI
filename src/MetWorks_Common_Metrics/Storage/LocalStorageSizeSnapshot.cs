namespace MetWorks.Common.Metrics.Storage;

public sealed record LocalStorageSizeSnapshot(
    long SettingsOverrideBytes,
    LocalStorageFileSize? LogFile,
    long LoggerSqliteBytes,
    long ReadingsSqliteBytes,
    IReadOnlyList<LocalStorageFileSize> TopLogFiles
);

public sealed record LocalStorageFileSize(
    string Path,
    long Bytes
);
