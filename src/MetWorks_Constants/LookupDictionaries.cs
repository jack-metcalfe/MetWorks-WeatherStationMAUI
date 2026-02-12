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

    public static readonly GroupSettingDefinition LoggerSQLiteGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.LoggerSQLite_groupName,
        groupBasePath: SettingConstants.LoggerSQLite_groupName,
        settingNames: [
            SettingConstants.LoggerSQLite_dbPath,
            SettingConstants.LoggerSQLite_tableName,
            SettingConstants.LoggerSQLite_minimumLevel,
            SettingConstants.LoggerSQLite_autoCreateTable
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
            SettingConstants.UnitOfMeasure_batteryLevel,
            SettingConstants.UnitOfMeasure_illuminance,
            SettingConstants.UnitOfMeasure_lightningDistance,
            SettingConstants.UnitOfMeasure_rainAccumulation,
            SettingConstants.UnitOfMeasure_windSpeed
        ]
    );
    public static readonly GroupSettingDefinition jsonToPostgreSQLGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.jsonToPostgreSQL_groupName,
        groupBasePath: SettingConstants.jsonToPostgreSQL_groupName,
        settingNames:

        [
            SettingConstants.jsonToPostgreSQL_connectionString,
            SettingConstants.jsonToPostgreSQL_enableBuffering
        ]
    );

    public static readonly GroupSettingDefinition JsonToSQLiteGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.JsonToSQLite_groupName,
        groupBasePath: SettingConstants.JsonToSQLite_groupName,
        settingNames:

        [
            SettingConstants.JsonToSQLite_connectionString,
            SettingConstants.JsonToSQLite_dbPath,
            SettingConstants.JsonToSQLite_enableBuffering
        ]
    );

    public static readonly GroupSettingDefinition TempestGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.Tempest_groupName,
        groupBasePath: SettingConstants.Tempest_groupName,
        settingNames:

        [
            SettingConstants.Tempest_apiKey,
            SettingConstants.Tempest_stationId
        ]
    );

    public static readonly GroupSettingDefinition MetricsGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.Metrics_groupName,
        groupBasePath: SettingConstants.Metrics_groupName,
        settingNames:
        [
            SettingConstants.Metrics_enabled,
            SettingConstants.Metrics_captureIntervalSeconds,
            SettingConstants.Metrics_applicationId,
            SettingConstants.MetricsPostgreSQL_connectionString,
            SettingConstants.MetricsPostgreSQL_tableName,
            SettingConstants.MetricsPostgreSQL_autoCreateTable,
            SettingConstants.Metrics_relayEnabled,
            SettingConstants.Metrics_relayTopN,
            SettingConstants.Metrics_pipelineEnabled,
            SettingConstants.Metrics_pipelineTopN
        ]
    );

    public static readonly GroupSettingDefinition StreamShippingGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.StreamShipping_groupName,
        groupBasePath: SettingConstants.StreamShipping_groupName,
        settingNames:
        [
            SettingConstants.StreamShipping_enabled,
            SettingConstants.StreamShipping_endpointUrl,
            SettingConstants.StreamShipping_shipIntervalSeconds,
            SettingConstants.StreamShipping_maxBatchRows
        ]
    );

    public static readonly GroupSettingDefinition StreamShippingHttpGroupSettingsDefinition = new GroupSettingDefinition(
        groupName: SettingConstants.StreamShippingHttp_groupName,
        groupBasePath: SettingConstants.StreamShippingHttp_groupName,
        settingNames:
        [
            SettingConstants.StreamShippingHttp_timeoutSeconds
        ]
    );
}