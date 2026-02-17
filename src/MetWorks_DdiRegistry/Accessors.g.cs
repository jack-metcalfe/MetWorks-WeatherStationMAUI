// Template:            Accessors
// Version:             1.1
// Template Requested:  Accessors
// Template:            File.Header
// Version:             1.1
// Template Requested:  Accessors
// Generated On:        2026-02-17T04:12:33.7258009Z
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
        public void RegisterTheRootCancellationTokenSource(System.Threading.CancellationTokenSource instance) =>
            _TheRootCancellationTokenSourceInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public System.Threading.CancellationTokenSource GetTheRootCancellationTokenSource() =>
            _TheRootCancellationTokenSourceInstance;

        // Internal accessor: always returns the concrete class.
        internal System.Threading.CancellationTokenSource GetTheRootCancellationTokenSource_Internal() =>
            _TheRootCancellationTokenSourceInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheDefaultPlatformPaths(MetWorks.Common.DefaultPlatformPaths instance) =>
            _TheDefaultPlatformPathsInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public IPlatformPaths GetTheDefaultPlatformPaths() =>
            _TheDefaultPlatformPathsInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.DefaultPlatformPaths GetTheDefaultPlatformPaths_Internal() =>
            _TheDefaultPlatformPathsInstance;
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
        public MetWorks.Interfaces.ILogger GetTheLoggerStub() =>
            _TheLoggerStubInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Logging.LoggerStub GetTheLoggerStub_Internal() =>
            _TheLoggerStubInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSqliteWriteCoordinator(MetWorks.Common.Utility.SqliteWriteCoordinator instance) =>
            _TheSqliteWriteCoordinatorInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Common.Utility.SqliteWriteCoordinator GetTheSqliteWriteCoordinator() =>
            _TheSqliteWriteCoordinatorInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Utility.SqliteWriteCoordinator GetTheSqliteWriteCoordinator_Internal() =>
            _TheSqliteWriteCoordinatorInstance;
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
        public MetWorks.Interfaces.ILogger GetTheLoggerFile() =>
            _TheLoggerFileInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Logging.LoggerFile GetTheLoggerFile_Internal() =>
            _TheLoggerFileInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSqliteDatabaseOptionsFactory(MetWorks.Common.Settings.SqliteDatabaseOptionsFactory instance) =>
            _TheSqliteDatabaseOptionsFactoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Common.Settings.SqliteDatabaseOptionsFactory GetTheSqliteDatabaseOptionsFactory() =>
            _TheSqliteDatabaseOptionsFactoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Settings.SqliteDatabaseOptionsFactory GetTheSqliteDatabaseOptionsFactory_Internal() =>
            _TheSqliteDatabaseOptionsFactoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSqliteDatabaseOptions(MetWorks.Data.Sqlite.SqliteDatabaseOptions instance) =>
            _TheSqliteDatabaseOptionsInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Data.Sqlite.SqliteDatabaseOptions GetTheSqliteDatabaseOptions() =>
            _TheSqliteDatabaseOptionsInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Data.Sqlite.SqliteDatabaseOptions GetTheSqliteDatabaseOptions_Internal() =>
            _TheSqliteDatabaseOptionsInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSqliteDatabase(MetWorks.Data.Sqlite.SqliteDatabase instance) =>
            _TheSqliteDatabaseInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Data.Sqlite.ISqliteDatabase GetTheSqliteDatabase() =>
            _TheSqliteDatabaseInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Data.Sqlite.SqliteDatabase GetTheSqliteDatabase_Internal() =>
            _TheSqliteDatabaseInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheMetricsDatabaseReadiness(MetWorks.Persistence.Metrics.MetricsDatabaseReadiness instance) =>
            _TheMetricsDatabaseReadinessInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Metrics.IMetricsDatabaseReadiness GetTheMetricsDatabaseReadiness() =>
            _TheMetricsDatabaseReadinessInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Metrics.MetricsDatabaseReadiness GetTheMetricsDatabaseReadiness_Internal() =>
            _TheMetricsDatabaseReadinessInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheMetricsSummaryRepository(MetWorks.Persistence.Metrics.MetricsSummaryRepository instance) =>
            _TheMetricsSummaryRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Metrics.IMetricsSummaryRepository GetTheMetricsSummaryRepository() =>
            _TheMetricsSummaryRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Metrics.MetricsSummaryRepository GetTheMetricsSummaryRepository_Internal() =>
            _TheMetricsSummaryRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheRollupsDatabaseReadiness(MetWorks.Persistence.Rollups.RollupsDatabaseReadiness instance) =>
            _TheRollupsDatabaseReadinessInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Rollups.IRollupsDatabaseReadiness GetTheRollupsDatabaseReadiness() =>
            _TheRollupsDatabaseReadinessInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Rollups.RollupsDatabaseReadiness GetTheRollupsDatabaseReadiness_Internal() =>
            _TheRollupsDatabaseReadinessInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheObservationRollupRepository(MetWorks.Persistence.Rollups.ObservationRollupRepository instance) =>
            _TheObservationRollupRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Rollups.IObservationRollupRepository GetTheObservationRollupRepository() =>
            _TheObservationRollupRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Rollups.ObservationRollupRepository GetTheObservationRollupRepository_Internal() =>
            _TheObservationRollupRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterThePrecipitationRollupRepository(MetWorks.Persistence.Rollups.PrecipitationRollupRepository instance) =>
            _ThePrecipitationRollupRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Rollups.IPrecipitationRollupRepository GetThePrecipitationRollupRepository() =>
            _ThePrecipitationRollupRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Rollups.PrecipitationRollupRepository GetThePrecipitationRollupRepository_Internal() =>
            _ThePrecipitationRollupRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheWindRollupRepository(MetWorks.Persistence.Rollups.WindRollupRepository instance) =>
            _TheWindRollupRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Rollups.IWindRollupRepository GetTheWindRollupRepository() =>
            _TheWindRollupRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Rollups.WindRollupRepository GetTheWindRollupRepository_Internal() =>
            _TheWindRollupRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLightningRollupRepository(MetWorks.Persistence.Rollups.LightningRollupRepository instance) =>
            _TheLightningRollupRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Rollups.ILightningRollupRepository GetTheLightningRollupRepository() =>
            _TheLightningRollupRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Rollups.LightningRollupRepository GetTheLightningRollupRepository_Internal() =>
            _TheLightningRollupRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheStreamShippingDatabaseReadiness(MetWorks.Persistence.StreamShipping.StreamShippingDatabaseReadiness instance) =>
            _TheStreamShippingDatabaseReadinessInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.StreamShipping.IStreamShippingDatabaseReadiness GetTheStreamShippingDatabaseReadiness() =>
            _TheStreamShippingDatabaseReadinessInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.StreamShipping.StreamShippingDatabaseReadiness GetTheStreamShippingDatabaseReadiness_Internal() =>
            _TheStreamShippingDatabaseReadinessInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheStreamShippingRepository(MetWorks.Persistence.StreamShipping.StreamShippingRepository instance) =>
            _TheStreamShippingRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.StreamShipping.IStreamShippingRepository GetTheStreamShippingRepository() =>
            _TheStreamShippingRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.StreamShipping.StreamShippingRepository GetTheStreamShippingRepository_Internal() =>
            _TheStreamShippingRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerStreamShippingRepository(MetWorks.Persistence.StreamShipping.LoggerStreamShippingRepository instance) =>
            _TheLoggerStreamShippingRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.StreamShipping.ILoggerStreamShippingRepository GetTheLoggerStreamShippingRepository() =>
            _TheLoggerStreamShippingRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.StreamShipping.LoggerStreamShippingRepository GetTheLoggerStreamShippingRepository_Internal() =>
            _TheLoggerStreamShippingRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheStationMetadataDatabaseReadiness(MetWorks.Persistence.StationMetadata.StationMetadataDatabaseReadiness instance) =>
            _TheStationMetadataDatabaseReadinessInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.StationMetadata.IStationMetadataDatabaseReadiness GetTheStationMetadataDatabaseReadiness() =>
            _TheStationMetadataDatabaseReadinessInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.StationMetadata.StationMetadataDatabaseReadiness GetTheStationMetadataDatabaseReadiness_Internal() =>
            _TheStationMetadataDatabaseReadinessInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheStationMetadataRepository(MetWorks.Persistence.StationMetadata.StationMetadataRepository instance) =>
            _TheStationMetadataRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.StationMetadata.IStationMetadataRepository GetTheStationMetadataRepository() =>
            _TheStationMetadataRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.StationMetadata.StationMetadataRepository GetTheStationMetadataRepository_Internal() =>
            _TheStationMetadataRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggingDatabaseReadiness(MetWorks.Persistence.Logging.LoggingDatabaseReadiness instance) =>
            _TheLoggingDatabaseReadinessInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Logging.ILoggingDatabaseReadiness GetTheLoggingDatabaseReadiness() =>
            _TheLoggingDatabaseReadinessInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Logging.LoggingDatabaseReadiness GetTheLoggingDatabaseReadiness_Internal() =>
            _TheLoggingDatabaseReadinessInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerSqliteRepository(MetWorks.Persistence.Logging.LoggerSqliteRepository instance) =>
            _TheLoggerSqliteRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Logging.ILoggerSqliteRepository GetTheLoggerSqliteRepository() =>
            _TheLoggerSqliteRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Logging.LoggerSqliteRepository GetTheLoggerSqliteRepository_Internal() =>
            _TheLoggerSqliteRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerSQLite(MetWorks.Common.Logging.LoggerSQLite instance) =>
            _TheLoggerSQLiteInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.ILogger GetTheLoggerSQLite() =>
            _TheLoggerSQLiteInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Logging.LoggerSQLite GetTheLoggerSQLite_Internal() =>
            _TheLoggerSQLiteInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerResilient(MetWorks.Common.Logging.LoggerResilient instance) =>
            _TheLoggerResilientInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Interfaces.ILogger GetTheLoggerResilient() =>
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
        public void RegisterTheStreamShippingHttpClientProvider(MetWorks.Common.Networking.StreamShippingHttpClientProvider instance) =>
            _TheStreamShippingHttpClientProviderInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Common.Networking.StreamShippingHttpClientProvider GetTheStreamShippingHttpClientProvider() =>
            _TheStreamShippingHttpClientProviderInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Networking.StreamShippingHttpClientProvider GetTheStreamShippingHttpClientProvider_Internal() =>
            _TheStreamShippingHttpClientProviderInstance;
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
        public void RegisterTheRawPacketDatabaseReadiness(MetWorks.Persistence.Ingest.RawPacketDatabaseReadiness instance) =>
            _TheRawPacketDatabaseReadinessInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Ingest.IRawPacketDatabaseReadiness GetTheRawPacketDatabaseReadiness() =>
            _TheRawPacketDatabaseReadinessInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Ingest.RawPacketDatabaseReadiness GetTheRawPacketDatabaseReadiness_Internal() =>
            _TheRawPacketDatabaseReadinessInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheRawPacketIngestRepository(MetWorks.Persistence.Ingest.RawPacketIngestRepository instance) =>
            _TheRawPacketIngestRepositoryInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Persistence.Ingest.IRawPacketIngestRepository GetTheRawPacketIngestRepository() =>
            _TheRawPacketIngestRepositoryInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Persistence.Ingest.RawPacketIngestRepository GetTheRawPacketIngestRepository_Internal() =>
            _TheRawPacketIngestRepositoryInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSQLiteRawPacketIngestor(MetWorks.Ingest.SQLite.RawPacketIngestor instance) =>
            _TheSQLiteRawPacketIngestorInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.SQLite.RawPacketIngestor GetTheSQLiteRawPacketIngestor() =>
            _TheSQLiteRawPacketIngestorInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.SQLite.RawPacketIngestor GetTheSQLiteRawPacketIngestor_Internal() =>
            _TheSQLiteRawPacketIngestorInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheStationMetadataStreamShipper(MetWorks.Ingest.SQLite.Shipping.StationMetadataStreamShipper instance) =>
            _TheStationMetadataStreamShipperInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.SQLite.Shipping.StationMetadataStreamShipper GetTheStationMetadataStreamShipper() =>
            _TheStationMetadataStreamShipperInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.SQLite.Shipping.StationMetadataStreamShipper GetTheStationMetadataStreamShipper_Internal() =>
            _TheStationMetadataStreamShipperInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLightningStreamShipper(MetWorks.Ingest.SQLite.Shipping.LightningStreamShipper instance) =>
            _TheLightningStreamShipperInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.SQLite.Shipping.LightningStreamShipper GetTheLightningStreamShipper() =>
            _TheLightningStreamShipperInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.SQLite.Shipping.LightningStreamShipper GetTheLightningStreamShipper_Internal() =>
            _TheLightningStreamShipperInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheLoggerSQLiteStreamShipper(MetWorks.Ingest.SQLite.Shipping.LoggerSQLiteStreamShipper instance) =>
            _TheLoggerSQLiteStreamShipperInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.SQLite.Shipping.LoggerSQLiteStreamShipper GetTheLoggerSQLiteStreamShipper() =>
            _TheLoggerSQLiteStreamShipperInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.SQLite.Shipping.LoggerSQLiteStreamShipper GetTheLoggerSQLiteStreamShipper_Internal() =>
            _TheLoggerSQLiteStreamShipperInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheObservationStreamShipper(MetWorks.Ingest.SQLite.Shipping.ObservationStreamShipper instance) =>
            _TheObservationStreamShipperInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.SQLite.Shipping.ObservationStreamShipper GetTheObservationStreamShipper() =>
            _TheObservationStreamShipperInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.SQLite.Shipping.ObservationStreamShipper GetTheObservationStreamShipper_Internal() =>
            _TheObservationStreamShipperInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterThePrecipitationStreamShipper(MetWorks.Ingest.SQLite.Shipping.PrecipitationStreamShipper instance) =>
            _ThePrecipitationStreamShipperInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.SQLite.Shipping.PrecipitationStreamShipper GetThePrecipitationStreamShipper() =>
            _ThePrecipitationStreamShipperInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.SQLite.Shipping.PrecipitationStreamShipper GetThePrecipitationStreamShipper_Internal() =>
            _ThePrecipitationStreamShipperInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheWindStreamShipper(MetWorks.Ingest.SQLite.Shipping.WindStreamShipper instance) =>
            _TheWindStreamShipperInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.SQLite.Shipping.WindStreamShipper GetTheWindStreamShipper() =>
            _TheWindStreamShipperInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.SQLite.Shipping.WindStreamShipper GetTheWindStreamShipper_Internal() =>
            _TheWindStreamShipperInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheMetricsSummaryIngestor(MetWorks.Ingest.SQLite.MetricsSummaryIngestor instance) =>
            _TheMetricsSummaryIngestorInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public IMetricsSummaryPersister GetTheMetricsSummaryIngestor() =>
            _TheMetricsSummaryIngestorInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.SQLite.MetricsSummaryIngestor GetTheMetricsSummaryIngestor_Internal() =>
            _TheMetricsSummaryIngestorInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheMetricsSamplerService(MetWorks.Common.Metrics.MetricsSamplerService instance) =>
            _TheMetricsSamplerServiceInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Common.Metrics.MetricsSamplerService GetTheMetricsSamplerService() =>
            _TheMetricsSamplerServiceInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Common.Metrics.MetricsSamplerService GetTheMetricsSamplerService_Internal() =>
            _TheMetricsSamplerServiceInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheRollupsWorker(MetWorks.Ingest.SQLite.Rollups.RollupsWorker instance) =>
            _TheRollupsWorkerInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.SQLite.Rollups.RollupsWorker GetTheRollupsWorker() =>
            _TheRollupsWorkerInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.SQLite.Rollups.RollupsWorker GetTheRollupsWorker_Internal() =>
            _TheRollupsWorkerInstance;
        // Template:            Accessors.Triplet
        // Version:             1.1
        // Template Requested:  Accessors

        // Register method: stores the concrete instance in the backing field.
        public void RegisterTheSQLiteStationMetadataIngestor(MetWorks.Ingest.SQLite.StationMetadataIngestor instance) =>
            _TheSQLiteStationMetadataIngestorInstance = instance;

        // External accessor: returns the interface type when defined, otherwise the concrete class.
        public MetWorks.Ingest.SQLite.StationMetadataIngestor GetTheSQLiteStationMetadataIngestor() =>
            _TheSQLiteStationMetadataIngestorInstance;

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Ingest.SQLite.StationMetadataIngestor GetTheSQLiteStationMetadataIngestor_Internal() =>
            _TheSQLiteStationMetadataIngestorInstance;
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
    }
}