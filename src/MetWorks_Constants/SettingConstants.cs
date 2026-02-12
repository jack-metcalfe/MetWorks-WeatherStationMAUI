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

    public const string LoggerSQLite_groupName = "loggerSQLite";
    public const string LoggerSQLite_dbPath = "dbPath";
    public const string LoggerSQLite_tableName = "tableName";
    public const string LoggerSQLite_minimumLevel = "minimumLevel";
    public const string LoggerSQLite_autoCreateTable = "autoCreateTable";

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

    public const string jsonToPostgreSQL_groupName = "jsonToPostgreSQL";
    public const string jsonToPostgreSQL_connectionString = "connectionString";
    public const string jsonToPostgreSQL_enableBuffering = "enableBuffering";

    public const string JsonToSQLite_groupName = "jsonToSQLite";
    public const string JsonToSQLite_connectionString = "connectionString";
    public const string JsonToSQLite_dbPath = "dbPath";
    public const string JsonToSQLite_enableBuffering = "enableBuffering";

    public const string Tempest_groupName = "tempest";
    public const string Tempest_apiKey = "apiKey";
    public const string Tempest_stationId = "stationId";

    public const string Metrics_groupName = "metrics";
    public const string Metrics_enabled = "enabled";
    public const string Metrics_captureIntervalSeconds = "captureIntervalSeconds";
    public const string Metrics_applicationId = "applicationId";
    public const string Metrics_connectionString = "connectionString";
    public const string Metrics_tableName = "tableName";
    public const string Metrics_autoCreateTable = "autoCreateTable";
    // Metrics persistence uses /services/metrics/{connectionString,tableName,autoCreateTable}
    // Legacy compatibility aliases. Prefer Metrics_* for /services/metrics/*.
    public const string MetricsPostgreSQL_connectionString = Metrics_connectionString;
    public const string MetricsPostgreSQL_tableName = Metrics_tableName;
    public const string MetricsPostgreSQL_autoCreateTable = Metrics_autoCreateTable;

    public const string Metrics_relayEnabled = "relayEnabled";
    public const string Metrics_relayTopN = "relayTopN";

    public const string Metrics_pipelineEnabled = "pipelineEnabled";
    public const string Metrics_pipelineTopN = "pipelineTopN";

    public const string Metrics_storageEnabled = "storageEnabled";
    public const string Metrics_storageTopN = "storageTopN";

    public const string StreamShipping_groupName = "streamShipping";
    public const string StreamShipping_enabled = "enabled";
    public const string StreamShipping_endpointUrl = "endpointUrl";
    public const string StreamShipping_shipIntervalSeconds = "shipIntervalSeconds";
    public const string StreamShipping_maxBatchRows = "maxBatchRows";

    public const string StreamShippingHttp_groupName = "streamShippingHttp";
    public const string StreamShippingHttp_timeoutSeconds = "timeoutSeconds";
}
