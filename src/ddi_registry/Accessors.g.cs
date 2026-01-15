// Template:            Accessors
// Version:             1.1
// Template Requested:  Accessors
// Template:            File.Header
// Version:             1.1
// Template Requested:  Accessors
// Generated On:        2026-01-15T03:06:48.6676207Z
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
        public void RegisterTheEventRelayBasic(EventRelay.EventRelayBasic instance) =>
            _TheEventRelayBasicInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public Interfaces.IEventRelayBasic GetTheEventRelayBasic() =>
            _TheEventRelayBasicInstance;

        // Internal accessor: always returns the concrete class.
        internal EventRelay.EventRelayBasic GetTheEventRelayBasic_Internal() =>
            _TheEventRelayBasicInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheEventRelayPath(EventRelay.EventRelayPath instance) =>
            _TheEventRelayPathInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public Interfaces.IEventRelayPath GetTheEventRelayPath() =>
            _TheEventRelayPathInstance;

        // Internal accessor: always returns the concrete class.
        internal EventRelay.EventRelayPath GetTheEventRelayPath_Internal() =>
            _TheEventRelayPathInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerStub(Logging.LoggerStub instance) =>
            _TheLoggerStubInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public Interfaces.ILogger GetTheLoggerStub() =>
            _TheLoggerStubInstance;

        // Internal accessor: always returns the concrete class.
        internal Logging.LoggerStub GetTheLoggerStub_Internal() =>
            _TheLoggerStubInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSettingProvider(Settings.SettingProvider instance) =>
            _TheSettingProviderInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public Interfaces.ISettingProvider GetTheSettingProvider() =>
            _TheSettingProviderInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingProvider GetTheSettingProvider_Internal() =>
            _TheSettingProviderInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSettingRepository(Settings.SettingRepository instance) =>
            _TheSettingRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public Interfaces.ISettingRepository GetTheSettingRepository() =>
            _TheSettingRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal Settings.SettingRepository GetTheSettingRepository_Internal() =>
            _TheSettingRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerFile(Logging.LoggerFile instance) =>
            _TheLoggerFileInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public Interfaces.ILogger GetTheLoggerFile() =>
            _TheLoggerFileInstance;

        // Internal accessor: always returns the concrete class.
        internal Logging.LoggerFile GetTheLoggerFile_Internal() =>
            _TheLoggerFileInstance;
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
        public void RegisterTheUnitsOfMeasureInitializer(RedStar.Amounts.WeatherExtensions.UnitsOfMeasureInitializer instance) =>
            _TheUnitsOfMeasureInitializerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public RedStar.Amounts.WeatherExtensions.UnitsOfMeasureInitializer GetTheUnitsOfMeasureInitializer() =>
            _TheUnitsOfMeasureInitializerInstance;

        // Internal accessor: always returns the concrete class.
        internal RedStar.Amounts.WeatherExtensions.UnitsOfMeasureInitializer GetTheUnitsOfMeasureInitializer_Internal() =>
            _TheUnitsOfMeasureInitializerInstance;
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
        public Interfaces.IBackgroundService GetTheUdpListener() =>
            _TheUdpListenerInstance;

        // Internal accessor: always returns the concrete class.
        internal UdpInRawPacketRecordTypedOut.Transformer GetTheUdpListener_Internal() =>
            _TheUdpListenerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheRawPacketRecordTypedInPostgresOut(RawPacketRecordTypedInPostgresOut.ListenerSink instance) =>
            _TheRawPacketRecordTypedInPostgresOutInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public Interfaces.IBackgroundService GetTheRawPacketRecordTypedInPostgresOut() =>
            _TheRawPacketRecordTypedInPostgresOutInstance;

        // Internal accessor: always returns the concrete class.
        internal RawPacketRecordTypedInPostgresOut.ListenerSink GetTheRawPacketRecordTypedInPostgresOut_Internal() =>
            _TheRawPacketRecordTypedInPostgresOutInstance;
    }
}