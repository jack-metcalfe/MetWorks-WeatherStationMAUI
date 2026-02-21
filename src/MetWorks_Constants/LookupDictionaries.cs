namespace MetWorks.Constants;

public static class LookupDictionaries
{
    public static readonly GroupSettingDefinition InstanceGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.Instance_groupName,
        settingNames: [
            SettingConstants.Instance_installationId
        ]
    );
    public static readonly GroupSettingDefinition LoggerFileGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.LoggerFile_groupName,
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
    public static readonly GroupSettingDefinition LoggerSQLiteGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.LoggerSQLite_groupName,
        settingNames: [
            SettingConstants.LoggerSQLite_tableName,
            SettingConstants.LoggerSQLite_minimumLevel,
            SettingConstants.LoggerSQLite_autoCreateTable
        ]
    );
    public static readonly GroupSettingDefinition UdpListenerGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.UdpListener_groupName,
        settingNames: [
            SettingConstants.UdpListener_Port
        ]
    );
    public static readonly GroupSettingDefinition UnitOfMeasureGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.UnitOfMeasure_groupName,
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
    public static readonly GroupSettingDefinition SqliteGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.Sqlite_groupName,
        settingNames:

        [
            SettingConstants.Sqlite_busyTimeoutMs,
            SettingConstants.Sqlite_connectionString,
            SettingConstants.Sqlite_dbPath,
            SettingConstants.Sqlite_journalMode
        ]
    );

    public static readonly GroupSettingDefinition TempestGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.Tempest_groupName,
        settingNames:

        [
            SettingConstants.Tempest_apiKey,
            SettingConstants.Tempest_stationId
        ]
    );

    public static readonly GroupSettingDefinition TempestForecastGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.TempestForecast_groupName,
        settingNames:

        [
            SettingConstants.TempestForecast_refreshIntervalMinutes
        ]
    );

    public static readonly GroupSettingDefinition TempestObservationsGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.TempestObservations_groupName,
        settingNames:

        [
            SettingConstants.TempestObservations_refreshIntervalMinutes
        ]
    );

    public static readonly GroupSettingDefinition WeatherIngestGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.WeatherIngest_groupName,
        settingNames:

        [
            SettingConstants.WeatherIngest_restStaleMinutes,
            SettingConstants.WeatherIngest_sourceMode,
            SettingConstants.WeatherIngest_udpStaleSeconds
        ]
    );

    public static readonly GroupSettingDefinition MetricsGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.Metrics_groupName,
        settingNames:
        [
            SettingConstants.Metrics_enabled,
            SettingConstants.Metrics_captureIntervalSeconds,
            SettingConstants.Metrics_applicationId,
            SettingConstants.Metrics_relayEnabled,
            SettingConstants.Metrics_relayTopN,
            SettingConstants.Metrics_pipelineEnabled,
            SettingConstants.Metrics_pipelineTopN
        ]
    );

    public static readonly GroupSettingDefinition StreamShippingGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.StreamShipping_groupName,
        settingNames:
        [
            SettingConstants.StreamShipping_enabled,
            SettingConstants.StreamShipping_endpointUrl,
            SettingConstants.StreamShipping_shipIntervalSeconds,
            SettingConstants.StreamShipping_maxBatchRows
        ]
    );

    public static readonly GroupSettingDefinition StreamShippingHttpGroupSettingsDefinition = new GroupSettingDefinition(
        basePath: SettingConstants.StreamShippingHttp_groupName,
        settingNames:
        [
            SettingConstants.StreamShippingHttp_allowInvalidTlsForEndpointHost,
            SettingConstants.StreamShippingHttp_timeoutSeconds
        ]
    );
}