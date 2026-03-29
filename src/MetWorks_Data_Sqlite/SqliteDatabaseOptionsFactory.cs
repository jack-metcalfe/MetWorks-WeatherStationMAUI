namespace MetWorks.Data.Sqlite;

public sealed class SqliteDatabaseOptionsFactory
{
    public SqliteDatabaseOptions? Options { get; private set; }

    public string? Options_ConnectionString => Options?.ConnectionString;
    public string? Options_JournalMode => Options?.JournalMode;
    public int? Options_BusyTimeoutMs => Options?.BusyTimeoutMs;

    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IPlatformPaths iPlatformPaths,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iPlatformPaths);

        var connectionString = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.SqliteGroupSettingsDefinition.BuildPath(SettingConstants.Sqlite_connectionString));

        var dbPath = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.SqliteGroupSettingsDefinition.BuildPath(SettingConstants.Sqlite_dbPath));

        var journalMode = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.SqliteGroupSettingsDefinition.BuildPath(SettingConstants.Sqlite_journalMode));

        var busyTimeoutMs = iSettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.SqliteGroupSettingsDefinition.BuildPath(SettingConstants.Sqlite_busyTimeoutMs));

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                iLogger.Warning("SQLite dbPath not configured; cannot create SqliteDatabaseOptions.");
                Options = null;
                return true;
            }

            var resolvedDbPath = Path.IsPathRooted(dbPath)
                ? dbPath
                : Path.Combine(iPlatformPaths.AppDataDirectory, dbPath);

            try
            {
                var dir = Path.GetDirectoryName(resolvedDbPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            }
            catch
            {
            }

            connectionString = $"Data Source={resolvedDbPath};Mode=ReadWriteCreate;Cache=Shared";
        }

        Options = new SqliteDatabaseOptions();
        _ = await Options.InitializeAsync(
            connectionString,
            string.IsNullOrWhiteSpace(journalMode) ? "WAL" : journalMode,
            busyTimeoutMs <= 0 ? (int?)5000 : busyTimeoutMs,
            cancellationToken).ConfigureAwait(false);

        return true;
    }
}
