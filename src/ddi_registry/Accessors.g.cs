// Template:            Accessors
// Version:             1.1
// Template Requested:  Accessors
// Template:            File.Header
// Version:             1.1
// Template Requested:  Accessors
// Generated On:        2026-01-07T05:31:08.6249926Z
#nullable enable

namespace ServiceRegistry
{
    // The Registry class provides dual accessors for each named instance.
    // External accessors return interfaces when available, ensuring API safety.
    // Internal accessors always return concrete types, enabling initialization and internal wiring.
    // Register methods accept concrete instances and populate backing fields.
    public partial class Registry
    {
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterRootCancellationTokenSource(System.Threading.CancellationTokenSource instance) =>
            _RootCancellationTokenSourceInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public System.Threading.CancellationTokenSource GetRootCancellationTokenSource() =>
            _RootCancellationTokenSourceInstance;

        // Internal accessor: always returns the concrete class.
        internal System.Threading.CancellationTokenSource GetRootCancellationTokenSource_Internal() =>
            _RootCancellationTokenSourceInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheFileLogger(Logging.FileLogger instance) =>
            _TheFileLoggerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.IFileLogger GetTheFileLogger() =>
            _TheFileLoggerInstance;

        // Internal accessor: always returns the concrete class.
        internal Logging.FileLogger GetTheFileLogger_Internal() =>
            _TheFileLoggerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheProvenanceTracker(MetWorksServices.ProvenanceTracker instance) =>
            _TheProvenanceTrackerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorksServices.ProvenanceTracker GetTheProvenanceTracker() =>
            _TheProvenanceTrackerInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorksServices.ProvenanceTracker GetTheProvenanceTracker_Internal() =>
            _TheProvenanceTrackerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheUdpPortSetting(Settings.SettingConfiguration instance) =>
            _TheUdpPortSettingInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingConfiguration GetTheUdpPortSetting() =>
            _TheUdpPortSettingInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingConfiguration GetTheUdpPortSetting_Internal() =>
            _TheUdpPortSettingInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheUdpPortSettings(Settings.SettingConfiguration[] instance) =>
            _TheUdpPortSettingsInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingConfiguration[] GetTheUdpPortSettings() =>
            _TheUdpPortSettingsInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingConfiguration[] GetTheUdpPortSettings_Internal() =>
            _TheUdpPortSettingsInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTemperatureOptions(System.String[] instance) =>
            _TemperatureOptionsInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public System.String[] GetTemperatureOptions() =>
            _TemperatureOptionsInstance;

        // Internal accessor: always returns the concrete class.
        internal System.String[] GetTemperatureOptions_Internal() =>
            _TemperatureOptionsInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterUdpTemperatureSetting(Settings.SettingConfiguration instance) =>
            _UdpTemperatureSettingInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingConfiguration GetUdpTemperatureSetting() =>
            _UdpTemperatureSettingInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingConfiguration GetUdpTemperatureSetting_Internal() =>
            _UdpTemperatureSettingInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterWindspeedOptions(System.String[] instance) =>
            _WindspeedOptionsInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public System.String[] GetWindspeedOptions() =>
            _WindspeedOptionsInstance;

        // Internal accessor: always returns the concrete class.
        internal System.String[] GetWindspeedOptions_Internal() =>
            _WindspeedOptionsInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterUdpWindspeedSetting(Settings.SettingConfiguration instance) =>
            _UdpWindspeedSettingInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingConfiguration GetUdpWindspeedSetting() =>
            _UdpWindspeedSettingInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingConfiguration GetUdpWindspeedSetting_Internal() =>
            _UdpWindspeedSettingInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterPressureOptions(System.String[] instance) =>
            _PressureOptionsInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public System.String[] GetPressureOptions() =>
            _PressureOptionsInstance;

        // Internal accessor: always returns the concrete class.
        internal System.String[] GetPressureOptions_Internal() =>
            _PressureOptionsInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterUdpPressureSetting(Settings.SettingConfiguration instance) =>
            _UdpPressureSettingInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingConfiguration GetUdpPressureSetting() =>
            _UdpPressureSettingInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingConfiguration GetUdpPressureSetting_Internal() =>
            _UdpPressureSettingInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterPrecipitationOptions(System.String[] instance) =>
            _PrecipitationOptionsInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public System.String[] GetPrecipitationOptions() =>
            _PrecipitationOptionsInstance;

        // Internal accessor: always returns the concrete class.
        internal System.String[] GetPrecipitationOptions_Internal() =>
            _PrecipitationOptionsInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterUdpPrecipitationSetting(Settings.SettingConfiguration instance) =>
            _UdpPrecipitationSettingInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingConfiguration GetUdpPrecipitationSetting() =>
            _UdpPrecipitationSettingInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingConfiguration GetUdpPrecipitationSetting_Internal() =>
            _UdpPrecipitationSettingInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterDistanceOptions(System.String[] instance) =>
            _DistanceOptionsInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public System.String[] GetDistanceOptions() =>
            _DistanceOptionsInstance;

        // Internal accessor: always returns the concrete class.
        internal System.String[] GetDistanceOptions_Internal() =>
            _DistanceOptionsInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterUdpDistanceSetting(Settings.SettingConfiguration instance) =>
            _UdpDistanceSettingInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingConfiguration GetUdpDistanceSetting() =>
            _UdpDistanceSettingInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingConfiguration GetUdpDistanceSetting_Internal() =>
            _UdpDistanceSettingInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheUdpListenerSettings(Settings.SettingConfiguration[] instance) =>
            _TheUdpListenerSettingsInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingConfiguration[] GetTheUdpListenerSettings() =>
            _TheUdpListenerSettingsInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingConfiguration[] GetTheUdpListenerSettings_Internal() =>
            _TheUdpListenerSettingsInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheAppDataSettingOverridesProvider(MauiSettingOverridesProviders.AppDataSettingOverridesProvider instance) =>
            _TheAppDataSettingOverridesProviderInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.Settings.ISettingOverridesProvider GetTheAppDataSettingOverridesProvider() =>
            _TheAppDataSettingOverridesProviderInstance;

        // Internal accessor: always returns the concrete class.
        internal MauiSettingOverridesProviders.AppDataSettingOverridesProvider GetTheAppDataSettingOverridesProvider_Internal() =>
            _TheAppDataSettingOverridesProviderInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheUDPSettingsRepository(Settings.SettingsRepository instance) =>
            _TheUDPSettingsRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingsRepository GetTheUDPSettingsRepository() =>
            _TheUDPSettingsRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingsRepository GetTheUDPSettingsRepository_Internal() =>
            _TheUDPSettingsRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheWeatherDataTransformer(MetWorksServices.WeatherDataTransformer instance) =>
            _TheWeatherDataTransformerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorksServices.WeatherDataTransformer GetTheWeatherDataTransformer() =>
            _TheWeatherDataTransformerInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorksServices.WeatherDataTransformer GetTheWeatherDataTransformer_Internal() =>
            _TheWeatherDataTransformerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheUdpListener(UdpInRawPacketRecordTypedOut.Transformer instance) =>
            _TheUdpListenerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.IBackgroundService GetTheUdpListener() =>
            _TheUdpListenerInstance;

        // Internal accessor: always returns the concrete class.
        internal UdpInRawPacketRecordTypedOut.Transformer GetTheUdpListener_Internal() =>
            _TheUdpListenerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterThePostgresConnection(Settings.SettingConfiguration instance) =>
            _ThePostgresConnectionInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingConfiguration GetThePostgresConnection() =>
            _ThePostgresConnectionInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingConfiguration GetThePostgresConnection_Internal() =>
            _ThePostgresConnectionInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterThePostgresSettings(Settings.SettingConfiguration[] instance) =>
            _ThePostgresSettingsInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingConfiguration[] GetThePostgresSettings() =>
            _ThePostgresSettingsInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingConfiguration[] GetThePostgresSettings_Internal() =>
            _ThePostgresSettingsInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterThePostgresSettingsRepository(Settings.SettingsRepository instance) =>
            _ThePostgresSettingsRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.ISettingsRepository GetThePostgresSettingsRepository() =>
            _ThePostgresSettingsRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingsRepository GetThePostgresSettingsRepository_Internal() =>
            _ThePostgresSettingsRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheRawPacketRecordTypedInPostgresOut(RawPacketRecordTypedInPostgresOut.ListenerSink instance) =>
            _TheRawPacketRecordTypedInPostgresOutInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public InterfaceDefinition.IBackgroundService GetTheRawPacketRecordTypedInPostgresOut() =>
            _TheRawPacketRecordTypedInPostgresOutInstance;

        // Internal accessor: always returns the concrete class.
        internal RawPacketRecordTypedInPostgresOut.ListenerSink GetTheRawPacketRecordTypedInPostgresOut_Internal() =>
            _TheRawPacketRecordTypedInPostgresOutInstance;
    }
}