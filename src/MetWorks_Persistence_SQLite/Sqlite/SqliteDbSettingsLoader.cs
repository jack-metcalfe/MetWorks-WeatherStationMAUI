namespace MetWorks.Persistence.SQLite;

using MetWorks.Interfaces;
using MetWorks.Constants;

public static class SqliteDbSettingsLoader
{
    public static SqliteDbSettings Load(ISettingProvider settingProvider)
    {
        ArgumentNullException.ThrowIfNull(settingProvider);

        var connectionStringPath = $"/services/{SettingConstants.Sqlite_groupName}/{SettingConstants.Sqlite_connectionString}";
        var dbPathPath = $"/services/{SettingConstants.Sqlite_groupName}/{SettingConstants.Sqlite_dbPath}";
        var journalModePath = $"/services/{SettingConstants.Sqlite_groupName}/{SettingConstants.Sqlite_journalMode}";
        var busyTimeoutMsPath = $"/services/{SettingConstants.Sqlite_groupName}/{SettingConstants.Sqlite_busyTimeoutMs}";

        var connectionString = settingProvider.ISettingValueDictionary.TryGetValue(connectionStringPath, out var cs)
            ? cs.Value
            : null;

        var dbPath = settingProvider.ISettingValueDictionary.TryGetValue(dbPathPath, out var dp)
            ? dp.Value
            : "metworks.sqlite";

        var journalMode = settingProvider.ISettingValueDictionary.TryGetValue(journalModePath, out var jm)
            ? jm.Value
            : "WAL";

        var busyTimeoutMsString = settingProvider.ISettingValueDictionary.TryGetValue(busyTimeoutMsPath, out var bt)
            ? bt.Value
            : "5000";

        _ = int.TryParse(busyTimeoutMsString, out var busyTimeoutMs);
        if (busyTimeoutMs <= 0) busyTimeoutMs = 5000;

        return new SqliteDbSettings(
            ConnectionString: string.IsNullOrWhiteSpace(connectionString) ? null : connectionString,
            DbPath: string.IsNullOrWhiteSpace(dbPath) ? "metworks.sqlite" : dbPath,
            JournalMode: string.IsNullOrWhiteSpace(journalMode) ? "WAL" : journalMode,
            BusyTimeoutMs: busyTimeoutMs
        );
    }
}
