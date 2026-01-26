namespace MetWorks.Constants;

public static class LookupDictionaries
{
    public static readonly GroupSettingDefinition InstanceGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.Instance_groupName,
        groupBasePath: SettingConstants.Instance_groupName,
        settingNames: [
            SettingConstants.Instance_installationId
        ]
    );
    public static readonly GroupSettingDefinition LoggerFileGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.LoggerFile_groupName,
        groupBasePath: SettingConstants.LoggerFile_groupName,
        settingNames: [
            SettingConstants.LoggerFile_fileSizeLimitBytes,
            SettingConstants.LoggerFile_minimumLevel,
            SettingConstants.LoggerFile_outputTemplate,
            SettingConstants.LoggerFile_relativeLogPath,
            SettingConstants.LoggerFile_retainedFileCountLimit,
            SettingConstants.LoggerFile_rollingInterval,
            SettingConstants.LoggerFile_rollOnFileSizeLimit
        ]
    );
    public static readonly GroupSettingDefinition LoggerPostgreSQLGroupSettingsDefinition = new GroupSettingDefinition(
            groupName: SettingConstants.LoggerPostgreSQL_groupName,
            groupBasePath: SettingConstants.LoggerPostgreSQL_groupName,
            settingNames: [
                SettingConstants.LoggerPostgreSQL_connectionString,
                    SettingConstants.LoggerPostgreSQL_tableName,
                    SettingConstants.LoggerPostgreSQL_minimumLevel,
                    SettingConstants.LoggerPostgreSQL_autoCreateTable
            ]
        );
    public static readonly GroupSettingDefinition UdpListenerGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.UdpListener_groupName,
        groupBasePath: SettingConstants.UdpListener_groupName,
        settingNames: [
            SettingConstants.UdpListener_preferredPort
        ]
    );
    public static readonly GroupSettingDefinition UnitOfMeasureGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.UnitOfMeasure_groupName,
        groupBasePath: SettingConstants.UnitOfMeasure_groupName,
        settingNames: [
            SettingConstants.UnitOfMeasure_airPressure,
            SettingConstants.UnitOfMeasure_airTemperature,
            SettingConstants.UnitOfMeasure_lightningDistance,
            SettingConstants.UnitOfMeasure_precipitationAmount,
            SettingConstants.UnitOfMeasure_windSpeed
        ]
    );
    public static readonly GroupSettingDefinition XMLToPostgreSQLGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.XMLToPostgreSQL_groupName,
        groupBasePath: SettingConstants.XMLToPostgreSQL_groupName,
        settingNames:

        [
            SettingConstants.XMLToPostgreSQL_connectionString,
            SettingConstants.XMLToPostgreSQL_enableBuffering
        ]
    );
}