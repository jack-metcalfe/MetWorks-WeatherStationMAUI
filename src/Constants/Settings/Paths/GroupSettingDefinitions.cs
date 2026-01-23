namespace MetWorks.Constants.Settings.Paths;
public record GroupSettingDefinition
{
    public string GroupName { get; }
    public string GroupBasePath { get; }
    public string[] SettingNames { get; }
    public string BuildSettingPath(string settingName)
        => $"{GroupBasePath}{settingName}";
    public GroupSettingDefinition(
        string groupName,
        string groupBasePath,
        string[] settingNames
    )
    {
        GroupName = groupName;
        GroupBasePath = groupBasePath;
        SettingNames = settingNames;
    }
}
public static class GroupSettingDefinitions
{
    public const string Section = "settingDefinitions";

    public const string LoggerFile_groupName = @"Logging.LoggerFile";
    public const string LoggerFile_fileSizeLimitBytes = @"fileSizeLimitBytes";
    public const string LoggerFile_minimumLevel = @"minimumLevel";
    public const string LoggerFile_outputTemplate = @"outputTemplate";
    public const string LoggerFile_relativeLogPath = @"relativeLogPath";
    public const string LoggerFile_retainedFileCountLimit = @"retainedFileCountLimit";
    public const string LoggerFile_rollingInterval = @"rollingInterval";
    public const string LoggerFile_rollOnFileSizeLimit = @"rollOnFileSizeLimit";

    public const string UnitOfMeasure_groupName = "UnitOfMeasure";

    public const string UdpListener_groupName = "UdpListener";
    public const string UdpListener_preferredPort = "preferredPort";

    public const string XMLToPostgreSQL_groupName = "xmlToPostgreSQL";
    public const string XMLToPostgreSQL_connectionString = "connectionString";
    public const string XMLToPostgreSQL_enableBuffering = "enableBuffering";

    public const string LoggerPostgreSQL_groupName = "Logging.LoggerPostgreSQL";
    public const string LoggerPostgreSQL_connectionString = "connectionString";
    public const string LoggerPostgreSQL_tableName = "tableName";
    public const string LoggerPostgreSQL_minimumLevel = "minimumLevel";
    public const string LoggerPostgreSQL_autoCreateTable = "autoCreateTable";

    static readonly GroupSettingDefinition[] Definitions =
    [
        new GroupSettingDefinition(
            groupName: LoggerFile_groupName,
            groupBasePath: "/services/logging/loggerFile/",
            settingNames:
            [
                LoggerFile_fileSizeLimitBytes,
                LoggerFile_minimumLevel,
                LoggerFile_outputTemplate,
                LoggerFile_relativeLogPath,
                LoggerFile_retainedFileCountLimit,
                LoggerFile_rollingInterval,
                LoggerFile_rollOnFileSizeLimit
            ]
        ),
        new GroupSettingDefinition(
            groupName: UnitOfMeasure_groupName,
            groupBasePath: "/services/unitOfMeasure/",
            settingNames:
            [
                MeasurementHelper.UnitOfMeasure_airPressure,
                MeasurementHelper.UnitOfMeasure_airTemperature,
                MeasurementHelper.UnitOfMeasure_lightningDistance,
                MeasurementHelper.UnitOfMeasure_precipitationAmount,
                MeasurementHelper.UnitOfMeasure_windSpeed
            ]
        ),
        new GroupSettingDefinition(
            groupName: UdpListener_groupName,
            groupBasePath: "/services/udpListener/",
            settingNames:
            [
                UdpListener_preferredPort
            ]
        ),
        new GroupSettingDefinition(
            groupName: XMLToPostgreSQL_groupName,
            groupBasePath: "/services/xmlToPostgreSQL/",
            settingNames:
            [
                XMLToPostgreSQL_connectionString,
                XMLToPostgreSQL_enableBuffering
            ]
        ),
        new GroupSettingDefinition(
                groupName: LoggerPostgreSQL_groupName,
                groupBasePath: "/services/logging/loggerPostgreSQL/",
                settingNames:
                [
                    LoggerPostgreSQL_connectionString,
                    LoggerPostgreSQL_tableName,
                    LoggerPostgreSQL_minimumLevel,
                    LoggerPostgreSQL_autoCreateTable
                ]
            )
    ];
    public static Dictionary<string, GroupSettingDefinition> DefinitionsDictionary
        => Definitions.ToDictionary(def => def.GroupName, def => def);
    public static GroupSettingDefinition LoggerFileGroupSettingsDefinition
        => DefinitionsDictionary[LoggerFile_groupName];
    public static GroupSettingDefinition UnitOfMeasureGroupSettingsDefinition
        => DefinitionsDictionary[UnitOfMeasure_groupName];
    public static GroupSettingDefinition UdpListenerGroupSettingsDefinition
        => DefinitionsDictionary[UdpListener_groupName];
    public static GroupSettingDefinition XMLToPostgreSQLGroupSettingsDefinition
        => DefinitionsDictionary[XMLToPostgreSQL_groupName];
    public static GroupSettingDefinition LoggerPostgreSQLGroupSettingsDefinition
        => DefinitionsDictionary[LoggerPostgreSQL_groupName];
}