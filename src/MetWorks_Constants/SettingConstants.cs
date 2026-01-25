namespace MetWorks.Constants;
public static class SettingConstants
{
    const string UnitOfMeasureBasePath = "/services/unitOfMeasure/";
    const string UnitOfMeasureSelectionPathSuffix = "/selection";
    public const string UnitOfMeasure_groupName = "UnitOfMeasure";
    public const string UnitOfMeasure_airPressure = "airPressure";
    public const string UnitOfMeasure_airTemperature = "airTemperature";
    public const string UnitOfMeasure_lightningDistance = "lightningDistance";
    public const string UnitOfMeasure_precipitationAmount = "precipitationAmount";
    public const string UnitOfMeasure_windSpeed = "windSpeed";

    public const string XMLToPostgreSQL_groupName = "xmlToPostgreSQL";
    public const string XMLToPostgreSQL_connectionString = "connectionString";
    public const string XMLToPostgreSQL_enableBuffering = "enableBuffering";

    public const string LoggerFile_groupName = @"loggerFile";
    public const string LoggerFile_fileSizeLimitBytes = @"fileSizeLimitBytes";
    public const string LoggerFile_minimumLevel = @"minimumLevel";
    public const string LoggerFile_outputTemplate = @"outputTemplate";
    public const string LoggerFile_relativeLogPath = @"relativeLogPath";
    public const string LoggerFile_retainedFileCountLimit = @"retainedFileCountLimit";
    public const string LoggerFile_rollingInterval = @"rollingInterval";
    public const string LoggerFile_rollOnFileSizeLimit = @"rollOnFileSizeLimit";

    public const string LoggerPostgreSQL_groupName = "loggerPostgreSQL";
    public const string LoggerPostgreSQL_connectionString = "connectionString";
    public const string LoggerPostgreSQL_tableName = "tableName";
    public const string LoggerPostgreSQL_minimumLevel = "minimumLevel";
    public const string LoggerPostgreSQL_autoCreateTable = "autoCreateTable";

    public const string UdpListener_groupName = "udpListener";
    public const string UdpListener_preferredPort = "preferredPort";

    public const string ProviderFilename = @"data.settings.yaml";
}
public record GroupSettingDefinitions
{
    public string GroupName { get; }
    public string GroupBasePath { get; }
    public string[] SettingNames { get; }

    public string BuildSettingPath(string settingName)
        => $"{GroupBasePath}{settingName}";
    public GroupSettingDefinitions(
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
public static class LookupDictionaries
{
    public static Dictionary<MeasurementTypeEnum, string> MeasurementTypeEnumToName = new()
    {
        { MeasurementTypeEnum.AirPressure, SettingConstants.UnitOfMeasure_airPressure },
        { MeasurementTypeEnum.AirTemperature, SettingConstants.UnitOfMeasure_airTemperature },
        { MeasurementTypeEnum.LightningDistance, SettingConstants.UnitOfMeasure_lightningDistance },
        { MeasurementTypeEnum.PrecipitationAmount, SettingConstants.UnitOfMeasure_precipitationAmount },
        { MeasurementTypeEnum.WindSpeed, SettingConstants.UnitOfMeasure_windSpeed },
    };
    public static Dictionary<string, MeasurementTypeEnum> NameToMeasurementTypeEnum = new()
    {
        { SettingConstants.UnitOfMeasure_airPressure, MeasurementTypeEnum.AirPressure },
        { SettingConstants.UnitOfMeasure_airTemperature, MeasurementTypeEnum.AirTemperature },
        { SettingConstants.UnitOfMeasure_lightningDistance, MeasurementTypeEnum.LightningDistance },
        { SettingConstants.UnitOfMeasure_precipitationAmount, MeasurementTypeEnum.PrecipitationAmount },
        { SettingConstants.UnitOfMeasure_windSpeed, MeasurementTypeEnum.WindSpeed },
    };
    public static Dictionary<MeasurementTypeEnum, UnitType> MeasurementTypeEnumToUnitType = new()
    {
        { MeasurementTypeEnum.AirPressure, PressureUnits.InchOfMercury.UnitType },
        { MeasurementTypeEnum.AirTemperature, TemperatureUnits.DegreeFahrenheit.UnitType },
        { MeasurementTypeEnum.LightningDistance, LengthUnits.Mile.UnitType },
        { MeasurementTypeEnum.PrecipitationAmount, LengthUnits.Inch.UnitType },
        { MeasurementTypeEnum.WindSpeed, SpeedUnits.MilePerHour.UnitType }
    };
    static readonly GroupSettingDefinitions[] Definitions =
    [
        new GroupSettingDefinitions(
            groupName: SettingConstants.LoggerFile_groupName,
            groupBasePath: "/services/logging/loggerFile/",
            settingNames:
            [
                SettingConstants.LoggerFile_fileSizeLimitBytes,
                SettingConstants.LoggerFile_minimumLevel,
                SettingConstants.LoggerFile_outputTemplate,
                SettingConstants.LoggerFile_relativeLogPath,
                SettingConstants.LoggerFile_retainedFileCountLimit,
                SettingConstants.LoggerFile_rollingInterval,
                SettingConstants.LoggerFile_rollOnFileSizeLimit
            ]
        ),
        new GroupSettingDefinitions(
            groupName: SettingConstants.UnitOfMeasure_groupName,
            groupBasePath: "/services/unitOfMeasure/",
            settingNames:
            [
                SettingConstants.UnitOfMeasure_airPressure,
                SettingConstants.UnitOfMeasure_airTemperature,
                SettingConstants.UnitOfMeasure_lightningDistance,
                SettingConstants.UnitOfMeasure_precipitationAmount,
                SettingConstants.UnitOfMeasure_windSpeed
            ]
        ),
        new GroupSettingDefinitions(
            groupName: SettingConstants.UdpListener_groupName,
            groupBasePath: "/services/udpListener/",
            settingNames:
            [
                SettingConstants.UdpListener_preferredPort
            ]
        ),
        new GroupSettingDefinitions(
            groupName: SettingConstants.XMLToPostgreSQL_groupName,
            groupBasePath: "/services/xmlToPostgreSQL/",
            settingNames:
            [
                SettingConstants.XMLToPostgreSQL_connectionString,
                SettingConstants.XMLToPostgreSQL_enableBuffering
            ]
        ),
        new GroupSettingDefinitions(
                groupName: SettingConstants.LoggerPostgreSQL_groupName,
                groupBasePath: "/services/logging/loggerPostgreSQL/",
                settingNames:
                [
                    SettingConstants.LoggerPostgreSQL_connectionString,
                    SettingConstants.LoggerPostgreSQL_tableName,
                    SettingConstants.LoggerPostgreSQL_minimumLevel,
                    SettingConstants.LoggerPostgreSQL_autoCreateTable
                ]
            )
    ];
    public static Dictionary<string, GroupSettingDefinitions> DefinitionsDictionary
        => Definitions.ToDictionary(def => def.GroupName, def => def);
    public static GroupSettingDefinitions LoggerFileGroupSettingsDefinition
        => DefinitionsDictionary[SettingConstants.LoggerFile_groupName];
    public static GroupSettingDefinitions UnitOfMeasureGroupSettingsDefinition
        => DefinitionsDictionary[SettingConstants.UnitOfMeasure_groupName];
    public static GroupSettingDefinitions UdpListenerGroupSettingsDefinition
        => DefinitionsDictionary[SettingConstants.UdpListener_groupName];
    public static GroupSettingDefinitions XMLToPostgreSQLGroupSettingsDefinition
        => DefinitionsDictionary[SettingConstants.XMLToPostgreSQL_groupName];
    public static GroupSettingDefinitions LoggerPostgreSQLGroupSettingsDefinition
        => DefinitionsDictionary[SettingConstants.LoggerPostgreSQL_groupName];
}