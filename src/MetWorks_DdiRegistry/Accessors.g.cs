// Template:            Accessors
// Version:             1.1
// Template Requested:  Accessors
// Template:            File.Header
// Version:             1.1
// Template Requested:  Accessors
// Generated On:        2026-02-04T20:48:31.7351773Z
#nullable enable

namespace MetWorks.ServiceRegistry
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
        public void RegisterTheMetricsLatestSnapshotStore(MetWorks.Common.Metrics.MetricsLatestSnapshotStore instance) =>
            _TheMetricsLatestSnapshotStoreInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Common.Metrics.IMetricsLatestSnapshot GetTheMetricsLatestSnapshotStore() =>
            _TheMetricsLatestSnapshotStoreInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Metrics.MetricsLatestSnapshotStore GetTheMetricsLatestSnapshotStore_Internal() =>
            _TheMetricsLatestSnapshotStoreInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheEventRelayBasic(MetWorks.EventRelay.EventRelayBasic instance) =>
            _TheEventRelayBasicInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.IEventRelayBasic GetTheEventRelayBasic() =>
            _TheEventRelayBasicInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.EventRelay.EventRelayBasic GetTheEventRelayBasic_Internal() =>
            _TheEventRelayBasicInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheEventRelayPath(MetWorks.EventRelay.EventRelayPath instance) =>
            _TheEventRelayPathInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.IEventRelayPath GetTheEventRelayPath() =>
            _TheEventRelayPathInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.EventRelay.EventRelayPath GetTheEventRelayPath_Internal() =>
            _TheEventRelayPathInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerStub(MetWorks.Common.Logging.LoggerStub instance) =>
            _TheLoggerStubInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.ILoggerStub GetTheLoggerStub() =>
            _TheLoggerStubInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Logging.LoggerStub GetTheLoggerStub_Internal() =>
            _TheLoggerStubInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSettingProvider(MetWorks.Common.Settings.SettingProvider instance) =>
            _TheSettingProviderInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.ISettingProvider GetTheSettingProvider() =>
            _TheSettingProviderInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Settings.SettingProvider GetTheSettingProvider_Internal() =>
            _TheSettingProviderInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSettingRepository(MetWorks.Common.Settings.SettingRepository instance) =>
            _TheSettingRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.ISettingRepository GetTheSettingRepository() =>
            _TheSettingRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Settings.SettingRepository GetTheSettingRepository_Internal() =>
            _TheSettingRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheInstanceIdentifier(MetWorks.InstanceIdentifier.InstanceIdentifier instance) =>
            _TheInstanceIdentifierInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.IInstanceIdentifier GetTheInstanceIdentifier() =>
            _TheInstanceIdentifierInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.InstanceIdentifier.InstanceIdentifier GetTheInstanceIdentifier_Internal() =>
            _TheInstanceIdentifierInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerFile(MetWorks.Common.Logging.LoggerFile instance) =>
            _TheLoggerFileInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.ILoggerFile GetTheLoggerFile() =>
            _TheLoggerFileInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Logging.LoggerFile GetTheLoggerFile_Internal() =>
            _TheLoggerFileInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerPostgreSQL(MetWorks.Common.Logging.LoggerPostgreSQL instance) =>
            _TheLoggerPostgreSQLInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.ILoggerPostgreSQL GetTheLoggerPostgreSQL() =>
            _TheLoggerPostgreSQLInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Logging.LoggerPostgreSQL GetTheLoggerPostgreSQL_Internal() =>
            _TheLoggerPostgreSQLInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerResilient(MetWorks.Common.Logging.LoggerResilient instance) =>
            _TheLoggerResilientInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.ILoggerResilient GetTheLoggerResilient() =>
            _TheLoggerResilientInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Logging.LoggerResilient GetTheLoggerResilient_Internal() =>
            _TheLoggerResilientInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheProvenanceTracker(MetWorks.Common.ProvenanceTracker instance) =>
            _TheProvenanceTrackerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Common.ProvenanceTracker GetTheProvenanceTracker() =>
            _TheProvenanceTrackerInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.ProvenanceTracker GetTheProvenanceTracker_Internal() =>
            _TheProvenanceTrackerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheMetricsSampler(MetWorks.Common.Metrics.MetricsSamplerService instance) =>
            _TheMetricsSamplerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Common.Metrics.MetricsSamplerService GetTheMetricsSampler() =>
            _TheMetricsSamplerInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Metrics.MetricsSamplerService GetTheMetricsSampler_Internal() =>
            _TheMetricsSamplerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheMetricsSummaryIngestor(MetWorks.Common.Metrics.MetricsSummaryIngestor instance) =>
            _TheMetricsSummaryIngestorInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Common.Metrics.MetricsSummaryIngestor GetTheMetricsSummaryIngestor() =>
            _TheMetricsSummaryIngestorInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Metrics.MetricsSummaryIngestor GetTheMetricsSummaryIngestor_Internal() =>
            _TheMetricsSummaryIngestorInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheTempestRestClient(MetWorks.Common.TempestRestClient instance) =>
            _TheTempestRestClientInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public ITempestRestClient GetTheTempestRestClient() =>
            _TheTempestRestClientInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.TempestRestClient GetTheTempestRestClient_Internal() =>
            _TheTempestRestClientInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheStationMetadataProvider(MetWorks.Common.StationMetadataProvider instance) =>
            _TheStationMetadataProviderInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.IStationMetadataProvider GetTheStationMetadataProvider() =>
            _TheStationMetadataProviderInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.StationMetadataProvider GetTheStationMetadataProvider_Internal() =>
            _TheStationMetadataProviderInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheUnitsOfMeasureInitializer(MetWorks.RedStar.Amounts.WeatherExtensions.UnitsOfMeasureInitializer instance) =>
            _TheUnitsOfMeasureInitializerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.RedStar.Amounts.WeatherExtensions.UnitsOfMeasureInitializer GetTheUnitsOfMeasureInitializer() =>
            _TheUnitsOfMeasureInitializerInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.RedStar.Amounts.WeatherExtensions.UnitsOfMeasureInitializer GetTheUnitsOfMeasureInitializer_Internal() =>
            _TheUnitsOfMeasureInitializerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSensorReadingTransformer(MetWorks.Ingest.Transformer.SensorReadingTransformer instance) =>
            _TheSensorReadingTransformerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.Transformer.SensorReadingTransformer GetTheSensorReadingTransformer() =>
            _TheSensorReadingTransformerInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.Transformer.SensorReadingTransformer GetTheSensorReadingTransformer_Internal() =>
            _TheSensorReadingTransformerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheUdpListener(MetWorks.Networking.Udp.Transformer.TempestPacketTransformer instance) =>
            _TheUdpListenerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Networking.Udp.Transformer.TempestPacketTransformer GetTheUdpListener() =>
            _TheUdpListenerInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Networking.Udp.Transformer.TempestPacketTransformer GetTheUdpListener_Internal() =>
            _TheUdpListenerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterThePostgresRawPacketIngestor(MetWorks.Ingest.Postgres.RawPacketIngestor instance) =>
            _ThePostgresRawPacketIngestorInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.Postgres.RawPacketIngestor GetThePostgresRawPacketIngestor() =>
            _ThePostgresRawPacketIngestorInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.Postgres.RawPacketIngestor GetThePostgresRawPacketIngestor_Internal() =>
            _ThePostgresRawPacketIngestorInstance;
    }
}