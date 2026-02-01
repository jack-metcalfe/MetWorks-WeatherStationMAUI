namespace MetWorks.Constants;
public static class SettingConstants
{
    public const string Instance_groupName = "instance";
    public const string Instance_installationId = "installationId";

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

    public const string ProviderFilename = @"data.settings.yaml";

    public const string UdpListener_groupName = "udpListener";
    public const string UdpListener_preferredPort = "preferredPort";

    public const string UnitOfMeasure_groupName = "unitOfMeasure";
    public const string UnitOfMeasure_airPressure = "airPressure";
    public const string UnitOfMeasure_airTemperature = "airTemperature";
    public const string UnitOfMeasure_batteryLevel = "batteryLevel";
    public const string UnitOfMeasure_illuminance = "illuminance";
    public const string UnitOfMeasure_lightningDistance = "lightningDistance";
    public const string UnitOfMeasure_rainAccumulation = "rainAccumulation";
    public const string UnitOfMeasure_solarRadiation = "solarRadiation";
    public const string UnitOfMeasure_windSpeed = "windSpeed";

    public const string XMLToPostgreSQL_groupName = "xmlToPostgreSQL";
    public const string XMLToPostgreSQL_connectionString = "connectionString";
    public const string XMLToPostgreSQL_enableBuffering = "enableBuffering";

    public const string Tempest_groupName = "tempest";
    public const string Tempest_apiKey = "apiKey";
    public const string Tempest_stationId = "stationId";
}
