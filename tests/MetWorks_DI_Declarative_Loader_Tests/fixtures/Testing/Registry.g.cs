// Template:            Registry
// Version:             1.1
// Template Requested:  Registry
// Template:            File.Header
// Version:             1.1
// Template Requested:  Registry
// Generated On:        2026-03-01T03:31:43.3092815Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The Registry class orchestrates the full lifecycle of all named instances.
    // Phase 1: Creation (synchronous)
    // Phase 2: Initialization (asynchronous)
    // Phase 3: Disposal (optional)
    public partial class Registry
    {
        private static Task EnsureInitialized(ref Task? taskField, Func<Task> factory)
        {
            var existing = System.Threading.Volatile.Read(ref taskField);
            if (existing is not null)
                return existing;

            var created = factory();
            var prior = System.Threading.Interlocked.CompareExchange(ref taskField, created, null);
            return prior ?? created;
        }

        private Task? _initTask_TheSettingProvider;
        public Task WhenTheSettingProviderInitializedAsync()
            => EnsureInitialized(ref _initTask_TheSettingProvider, () => TheSettingProvider_Initializer.Initialize_TheSettingProviderAsync(this));

        public Task WhenTheSettingProviderInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheSettingProviderInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheSettingProviderInitializedAsync();

        private Task? _initTask_TheSettingRepository;
        public Task WhenTheSettingRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheSettingRepository, () => TheSettingRepository_Initializer.Initialize_TheSettingRepositoryAsync(this));

        public Task WhenTheSettingRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheSettingRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheSettingRepositoryInitializedAsync();

        private Task? _initTask_TheInstanceIdentifier;
        public Task WhenTheInstanceIdentifierInitializedAsync()
            => EnsureInitialized(ref _initTask_TheInstanceIdentifier, () => TheInstanceIdentifier_Initializer.Initialize_TheInstanceIdentifierAsync(this));

        public Task WhenTheInstanceIdentifierInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheInstanceIdentifierInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheInstanceIdentifierInitializedAsync();

        private Task? _initTask_TheLoggerFile;
        public Task WhenTheLoggerFileInitializedAsync()
            => EnsureInitialized(ref _initTask_TheLoggerFile, () => TheLoggerFile_Initializer.Initialize_TheLoggerFileAsync(this));

        public Task WhenTheLoggerFileInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheLoggerFileInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheLoggerFileInitializedAsync();

        private Task? _initTask_TheSqliteDatabaseOptionsFactory;
        public Task WhenTheSqliteDatabaseOptionsFactoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheSqliteDatabaseOptionsFactory, () => TheSqliteDatabaseOptionsFactory_Initializer.Initialize_TheSqliteDatabaseOptionsFactoryAsync(this));

        public Task WhenTheSqliteDatabaseOptionsFactoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheSqliteDatabaseOptionsFactoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheSqliteDatabaseOptionsFactoryInitializedAsync();

        private Task? _initTask_TheSqliteDatabaseOptions;
        public Task WhenTheSqliteDatabaseOptionsInitializedAsync()
            => EnsureInitialized(ref _initTask_TheSqliteDatabaseOptions, () => TheSqliteDatabaseOptions_Initializer.Initialize_TheSqliteDatabaseOptionsAsync(this));

        public Task WhenTheSqliteDatabaseOptionsInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheSqliteDatabaseOptionsInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheSqliteDatabaseOptionsInitializedAsync();

        private Task? _initTask_TheSqliteDatabase;
        public Task WhenTheSqliteDatabaseInitializedAsync()
            => EnsureInitialized(ref _initTask_TheSqliteDatabase, () => TheSqliteDatabase_Initializer.Initialize_TheSqliteDatabaseAsync(this));

        public Task WhenTheSqliteDatabaseInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheSqliteDatabaseInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheSqliteDatabaseInitializedAsync();

        private Task? _initTask_TheMetricsDatabaseReadiness;
        public Task WhenTheMetricsDatabaseReadinessInitializedAsync()
            => EnsureInitialized(ref _initTask_TheMetricsDatabaseReadiness, () => TheMetricsDatabaseReadiness_Initializer.Initialize_TheMetricsDatabaseReadinessAsync(this));

        public Task WhenTheMetricsDatabaseReadinessInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheMetricsDatabaseReadinessInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheMetricsDatabaseReadinessInitializedAsync();

        private Task? _initTask_TheMetricsSummaryRepository;
        public Task WhenTheMetricsSummaryRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheMetricsSummaryRepository, () => TheMetricsSummaryRepository_Initializer.Initialize_TheMetricsSummaryRepositoryAsync(this));

        public Task WhenTheMetricsSummaryRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheMetricsSummaryRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheMetricsSummaryRepositoryInitializedAsync();

        private Task? _initTask_TheRollupsDatabaseReadiness;
        public Task WhenTheRollupsDatabaseReadinessInitializedAsync()
            => EnsureInitialized(ref _initTask_TheRollupsDatabaseReadiness, () => TheRollupsDatabaseReadiness_Initializer.Initialize_TheRollupsDatabaseReadinessAsync(this));

        public Task WhenTheRollupsDatabaseReadinessInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheRollupsDatabaseReadinessInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheRollupsDatabaseReadinessInitializedAsync();

        private Task? _initTask_TheObservationRollupRepository;
        public Task WhenTheObservationRollupRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheObservationRollupRepository, () => TheObservationRollupRepository_Initializer.Initialize_TheObservationRollupRepositoryAsync(this));

        public Task WhenTheObservationRollupRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheObservationRollupRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheObservationRollupRepositoryInitializedAsync();

        private Task? _initTask_ThePrecipitationRollupRepository;
        public Task WhenThePrecipitationRollupRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_ThePrecipitationRollupRepository, () => ThePrecipitationRollupRepository_Initializer.Initialize_ThePrecipitationRollupRepositoryAsync(this));

        public Task WhenThePrecipitationRollupRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenThePrecipitationRollupRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenThePrecipitationRollupRepositoryInitializedAsync();

        private Task? _initTask_TheWindRollupRepository;
        public Task WhenTheWindRollupRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheWindRollupRepository, () => TheWindRollupRepository_Initializer.Initialize_TheWindRollupRepositoryAsync(this));

        public Task WhenTheWindRollupRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheWindRollupRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheWindRollupRepositoryInitializedAsync();

        private Task? _initTask_TheLightningRollupRepository;
        public Task WhenTheLightningRollupRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheLightningRollupRepository, () => TheLightningRollupRepository_Initializer.Initialize_TheLightningRollupRepositoryAsync(this));

        public Task WhenTheLightningRollupRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheLightningRollupRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheLightningRollupRepositoryInitializedAsync();

        private Task? _initTask_TheStreamShippingDatabaseReadiness;
        public Task WhenTheStreamShippingDatabaseReadinessInitializedAsync()
            => EnsureInitialized(ref _initTask_TheStreamShippingDatabaseReadiness, () => TheStreamShippingDatabaseReadiness_Initializer.Initialize_TheStreamShippingDatabaseReadinessAsync(this));

        public Task WhenTheStreamShippingDatabaseReadinessInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheStreamShippingDatabaseReadinessInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheStreamShippingDatabaseReadinessInitializedAsync();

        private Task? _initTask_TheStreamShippingRepository;
        public Task WhenTheStreamShippingRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheStreamShippingRepository, () => TheStreamShippingRepository_Initializer.Initialize_TheStreamShippingRepositoryAsync(this));

        public Task WhenTheStreamShippingRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheStreamShippingRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheStreamShippingRepositoryInitializedAsync();

        private Task? _initTask_TheLoggerStreamShippingRepository;
        public Task WhenTheLoggerStreamShippingRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheLoggerStreamShippingRepository, () => TheLoggerStreamShippingRepository_Initializer.Initialize_TheLoggerStreamShippingRepositoryAsync(this));

        public Task WhenTheLoggerStreamShippingRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheLoggerStreamShippingRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheLoggerStreamShippingRepositoryInitializedAsync();

        private Task? _initTask_TheStationMetadataDatabaseReadiness;
        public Task WhenTheStationMetadataDatabaseReadinessInitializedAsync()
            => EnsureInitialized(ref _initTask_TheStationMetadataDatabaseReadiness, () => TheStationMetadataDatabaseReadiness_Initializer.Initialize_TheStationMetadataDatabaseReadinessAsync(this));

        public Task WhenTheStationMetadataDatabaseReadinessInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheStationMetadataDatabaseReadinessInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheStationMetadataDatabaseReadinessInitializedAsync();

        private Task? _initTask_TheStationMetadataRepository;
        public Task WhenTheStationMetadataRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheStationMetadataRepository, () => TheStationMetadataRepository_Initializer.Initialize_TheStationMetadataRepositoryAsync(this));

        public Task WhenTheStationMetadataRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheStationMetadataRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheStationMetadataRepositoryInitializedAsync();

        private Task? _initTask_TheLoggingDatabaseReadiness;
        public Task WhenTheLoggingDatabaseReadinessInitializedAsync()
            => EnsureInitialized(ref _initTask_TheLoggingDatabaseReadiness, () => TheLoggingDatabaseReadiness_Initializer.Initialize_TheLoggingDatabaseReadinessAsync(this));

        public Task WhenTheLoggingDatabaseReadinessInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheLoggingDatabaseReadinessInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheLoggingDatabaseReadinessInitializedAsync();

        private Task? _initTask_TheLoggerSqliteRepository;
        public Task WhenTheLoggerSqliteRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheLoggerSqliteRepository, () => TheLoggerSqliteRepository_Initializer.Initialize_TheLoggerSqliteRepositoryAsync(this));

        public Task WhenTheLoggerSqliteRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheLoggerSqliteRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheLoggerSqliteRepositoryInitializedAsync();

        private Task? _initTask_TheLoggerSQLite;
        public Task WhenTheLoggerSQLiteInitializedAsync()
            => EnsureInitialized(ref _initTask_TheLoggerSQLite, () => TheLoggerSQLite_Initializer.Initialize_TheLoggerSQLiteAsync(this));

        public Task WhenTheLoggerSQLiteInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheLoggerSQLiteInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheLoggerSQLiteInitializedAsync();

        private Task? _initTask_TheLoggerResilient;
        public Task WhenTheLoggerResilientInitializedAsync()
            => EnsureInitialized(ref _initTask_TheLoggerResilient, () => TheLoggerResilient_Initializer.Initialize_TheLoggerResilientAsync(this));

        public Task WhenTheLoggerResilientInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheLoggerResilientInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheLoggerResilientInitializedAsync();

        private Task? _initTask_TheProvenanceTracker;
        public Task WhenTheProvenanceTrackerInitializedAsync()
            => EnsureInitialized(ref _initTask_TheProvenanceTracker, () => TheProvenanceTracker_Initializer.Initialize_TheProvenanceTrackerAsync(this));

        public Task WhenTheProvenanceTrackerInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheProvenanceTrackerInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheProvenanceTrackerInitializedAsync();

        private Task? _initTask_TheStreamShippingHttpClientProvider;
        public Task WhenTheStreamShippingHttpClientProviderInitializedAsync()
            => EnsureInitialized(ref _initTask_TheStreamShippingHttpClientProvider, () => TheStreamShippingHttpClientProvider_Initializer.Initialize_TheStreamShippingHttpClientProviderAsync(this));

        public Task WhenTheStreamShippingHttpClientProviderInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheStreamShippingHttpClientProviderInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheStreamShippingHttpClientProviderInitializedAsync();

        private Task? _initTask_TheTempestRestClient;
        public Task WhenTheTempestRestClientInitializedAsync()
            => EnsureInitialized(ref _initTask_TheTempestRestClient, () => TheTempestRestClient_Initializer.Initialize_TheTempestRestClientAsync(this));

        public Task WhenTheTempestRestClientInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheTempestRestClientInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheTempestRestClientInitializedAsync();

        private Task? _initTask_TheStationMetadataProvider;
        public Task WhenTheStationMetadataProviderInitializedAsync()
            => EnsureInitialized(ref _initTask_TheStationMetadataProvider, () => TheStationMetadataProvider_Initializer.Initialize_TheStationMetadataProviderAsync(this));

        public Task WhenTheStationMetadataProviderInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheStationMetadataProviderInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheStationMetadataProviderInitializedAsync();

        private Task? _initTask_TheUnitsOfMeasureInitializer;
        public Task WhenTheUnitsOfMeasureInitializerInitializedAsync()
            => EnsureInitialized(ref _initTask_TheUnitsOfMeasureInitializer, () => TheUnitsOfMeasureInitializer_Initializer.Initialize_TheUnitsOfMeasureInitializerAsync(this));

        public Task WhenTheUnitsOfMeasureInitializerInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheUnitsOfMeasureInitializerInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheUnitsOfMeasureInitializerInitializedAsync();

        private Task? _initTask_TheRawPacketDatabaseReadiness;
        public Task WhenTheRawPacketDatabaseReadinessInitializedAsync()
            => EnsureInitialized(ref _initTask_TheRawPacketDatabaseReadiness, () => TheRawPacketDatabaseReadiness_Initializer.Initialize_TheRawPacketDatabaseReadinessAsync(this));

        public Task WhenTheRawPacketDatabaseReadinessInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheRawPacketDatabaseReadinessInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheRawPacketDatabaseReadinessInitializedAsync();

        private Task? _initTask_TheRawPacketIngestRepository;
        public Task WhenTheRawPacketIngestRepositoryInitializedAsync()
            => EnsureInitialized(ref _initTask_TheRawPacketIngestRepository, () => TheRawPacketIngestRepository_Initializer.Initialize_TheRawPacketIngestRepositoryAsync(this));

        public Task WhenTheRawPacketIngestRepositoryInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheRawPacketIngestRepositoryInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheRawPacketIngestRepositoryInitializedAsync();

        private Task? _initTask_TheSQLiteRawPacketIngestor;
        public Task WhenTheSQLiteRawPacketIngestorInitializedAsync()
            => EnsureInitialized(ref _initTask_TheSQLiteRawPacketIngestor, () => TheSQLiteRawPacketIngestor_Initializer.Initialize_TheSQLiteRawPacketIngestorAsync(this));

        public Task WhenTheSQLiteRawPacketIngestorInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheSQLiteRawPacketIngestorInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheSQLiteRawPacketIngestorInitializedAsync();

        private Task? _initTask_TheStationMetadataStreamShipper;
        public Task WhenTheStationMetadataStreamShipperInitializedAsync()
            => EnsureInitialized(ref _initTask_TheStationMetadataStreamShipper, () => TheStationMetadataStreamShipper_Initializer.Initialize_TheStationMetadataStreamShipperAsync(this));

        public Task WhenTheStationMetadataStreamShipperInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheStationMetadataStreamShipperInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheStationMetadataStreamShipperInitializedAsync();

        private Task? _initTask_TheLightningStreamShipper;
        public Task WhenTheLightningStreamShipperInitializedAsync()
            => EnsureInitialized(ref _initTask_TheLightningStreamShipper, () => TheLightningStreamShipper_Initializer.Initialize_TheLightningStreamShipperAsync(this));

        public Task WhenTheLightningStreamShipperInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheLightningStreamShipperInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheLightningStreamShipperInitializedAsync();

        private Task? _initTask_TheLoggerSQLiteStreamShipper;
        public Task WhenTheLoggerSQLiteStreamShipperInitializedAsync()
            => EnsureInitialized(ref _initTask_TheLoggerSQLiteStreamShipper, () => TheLoggerSQLiteStreamShipper_Initializer.Initialize_TheLoggerSQLiteStreamShipperAsync(this));

        public Task WhenTheLoggerSQLiteStreamShipperInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheLoggerSQLiteStreamShipperInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheLoggerSQLiteStreamShipperInitializedAsync();

        private Task? _initTask_TheObservationStreamShipper;
        public Task WhenTheObservationStreamShipperInitializedAsync()
            => EnsureInitialized(ref _initTask_TheObservationStreamShipper, () => TheObservationStreamShipper_Initializer.Initialize_TheObservationStreamShipperAsync(this));

        public Task WhenTheObservationStreamShipperInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheObservationStreamShipperInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheObservationStreamShipperInitializedAsync();

        private Task? _initTask_ThePrecipitationStreamShipper;
        public Task WhenThePrecipitationStreamShipperInitializedAsync()
            => EnsureInitialized(ref _initTask_ThePrecipitationStreamShipper, () => ThePrecipitationStreamShipper_Initializer.Initialize_ThePrecipitationStreamShipperAsync(this));

        public Task WhenThePrecipitationStreamShipperInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenThePrecipitationStreamShipperInitializedAsync().WaitAsync(cancellationToken)
                : WhenThePrecipitationStreamShipperInitializedAsync();

        private Task? _initTask_TheWindStreamShipper;
        public Task WhenTheWindStreamShipperInitializedAsync()
            => EnsureInitialized(ref _initTask_TheWindStreamShipper, () => TheWindStreamShipper_Initializer.Initialize_TheWindStreamShipperAsync(this));

        public Task WhenTheWindStreamShipperInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheWindStreamShipperInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheWindStreamShipperInitializedAsync();

        private Task? _initTask_TheMetricsSummaryIngestor;
        public Task WhenTheMetricsSummaryIngestorInitializedAsync()
            => EnsureInitialized(ref _initTask_TheMetricsSummaryIngestor, () => TheMetricsSummaryIngestor_Initializer.Initialize_TheMetricsSummaryIngestorAsync(this));

        public Task WhenTheMetricsSummaryIngestorInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheMetricsSummaryIngestorInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheMetricsSummaryIngestorInitializedAsync();

        private Task? _initTask_TheMetricsSamplerService;
        public Task WhenTheMetricsSamplerServiceInitializedAsync()
            => EnsureInitialized(ref _initTask_TheMetricsSamplerService, () => TheMetricsSamplerService_Initializer.Initialize_TheMetricsSamplerServiceAsync(this));

        public Task WhenTheMetricsSamplerServiceInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheMetricsSamplerServiceInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheMetricsSamplerServiceInitializedAsync();

        private Task? _initTask_TheRollupsWorker;
        public Task WhenTheRollupsWorkerInitializedAsync()
            => EnsureInitialized(ref _initTask_TheRollupsWorker, () => TheRollupsWorker_Initializer.Initialize_TheRollupsWorkerAsync(this));

        public Task WhenTheRollupsWorkerInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheRollupsWorkerInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheRollupsWorkerInitializedAsync();

        private Task? _initTask_TheSQLiteStationMetadataIngestor;
        public Task WhenTheSQLiteStationMetadataIngestorInitializedAsync()
            => EnsureInitialized(ref _initTask_TheSQLiteStationMetadataIngestor, () => TheSQLiteStationMetadataIngestor_Initializer.Initialize_TheSQLiteStationMetadataIngestorAsync(this));

        public Task WhenTheSQLiteStationMetadataIngestorInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheSQLiteStationMetadataIngestorInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheSQLiteStationMetadataIngestorInitializedAsync();

        private Task? _initTask_TheSensorReadingTransformer;
        public Task WhenTheSensorReadingTransformerInitializedAsync()
            => EnsureInitialized(ref _initTask_TheSensorReadingTransformer, () => TheSensorReadingTransformer_Initializer.Initialize_TheSensorReadingTransformerAsync(this));

        public Task WhenTheSensorReadingTransformerInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheSensorReadingTransformerInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheSensorReadingTransformerInitializedAsync();

        private Task? _initTask_TheUdpListener;
        public Task WhenTheUdpListenerInitializedAsync()
            => EnsureInitialized(ref _initTask_TheUdpListener, () => TheUdpListener_Initializer.Initialize_TheUdpListenerAsync(this));

        public Task WhenTheUdpListenerInitializedAsync(CancellationToken cancellationToken)
            => cancellationToken.CanBeCanceled
                ? WhenTheUdpListenerInitializedAsync().WaitAsync(cancellationToken)
                : WhenTheUdpListenerInitializedAsync();


        // Phase 1: Creation (synchronous, safe).
        // Each instance is created via its dedicated InstanceFactory.
        // Element-driven and assignment-driven construction logic is encapsulated per instance.
        public void CreateAll()
        {
            TheRootCancellationTokenSource_InstanceFactory.Create(this);
            TheDefaultPlatformPaths_InstanceFactory.Create(this);
            TheEventRelayBasic_InstanceFactory.Create(this);
            TheEventRelayPath_InstanceFactory.Create(this);
            TheLoggerStub_InstanceFactory.Create(this);
            TheSqliteWriteCoordinator_InstanceFactory.Create(this);
            TheMetricsLatestSnapshotStore_InstanceFactory.Create(this);
            TheSettingProvider_InstanceFactory.Create(this);
            TheSettingRepository_InstanceFactory.Create(this);
            TheInstanceIdentifier_InstanceFactory.Create(this);
            TheLoggerFile_InstanceFactory.Create(this);
            TheSqliteDatabaseOptionsFactory_InstanceFactory.Create(this);
            TheSqliteDatabaseOptions_InstanceFactory.Create(this);
            TheSqliteDatabase_InstanceFactory.Create(this);
            TheMetricsDatabaseReadiness_InstanceFactory.Create(this);
            TheMetricsSummaryRepository_InstanceFactory.Create(this);
            TheRollupsDatabaseReadiness_InstanceFactory.Create(this);
            TheObservationRollupRepository_InstanceFactory.Create(this);
            ThePrecipitationRollupRepository_InstanceFactory.Create(this);
            TheWindRollupRepository_InstanceFactory.Create(this);
            TheLightningRollupRepository_InstanceFactory.Create(this);
            TheStreamShippingDatabaseReadiness_InstanceFactory.Create(this);
            TheStreamShippingRepository_InstanceFactory.Create(this);
            TheLoggerStreamShippingRepository_InstanceFactory.Create(this);
            TheStationMetadataDatabaseReadiness_InstanceFactory.Create(this);
            TheStationMetadataRepository_InstanceFactory.Create(this);
            TheLoggingDatabaseReadiness_InstanceFactory.Create(this);
            TheLoggerSqliteRepository_InstanceFactory.Create(this);
            TheLoggerSQLite_InstanceFactory.Create(this);
            TheLoggerResilient_InstanceFactory.Create(this);
            TheProvenanceTracker_InstanceFactory.Create(this);
            TheStreamShippingHttpClientProvider_InstanceFactory.Create(this);
            TheTempestRestClient_InstanceFactory.Create(this);
            TheStationMetadataProvider_InstanceFactory.Create(this);
            TheUnitsOfMeasureInitializer_InstanceFactory.Create(this);
            TheRawPacketDatabaseReadiness_InstanceFactory.Create(this);
            TheRawPacketIngestRepository_InstanceFactory.Create(this);
            TheSQLiteRawPacketIngestor_InstanceFactory.Create(this);
            TheStationMetadataStreamShipper_InstanceFactory.Create(this);
            TheLightningStreamShipper_InstanceFactory.Create(this);
            TheLoggerSQLiteStreamShipper_InstanceFactory.Create(this);
            TheObservationStreamShipper_InstanceFactory.Create(this);
            ThePrecipitationStreamShipper_InstanceFactory.Create(this);
            TheWindStreamShipper_InstanceFactory.Create(this);
            TheMetricsSummaryIngestor_InstanceFactory.Create(this);
            TheMetricsSamplerService_InstanceFactory.Create(this);
            TheRollupsWorker_InstanceFactory.Create(this);
            TheSQLiteStationMetadataIngestor_InstanceFactory.Create(this);
            TheSensorReadingTransformer_InstanceFactory.Create(this);
            TheUdpListener_InstanceFactory.Create(this);
        }

        // Phase 2: Initialization (async, potentially slow).
        // Only instances with assignments require async initialization.
        // Element-driven instances are fully constructed during creation.
        public Task InitializeAllAsync()
            => InitializeAllAsync(CancellationToken.None);

        public async Task InitializeAllAsync(CancellationToken cancellationToken)
        {
            var tasks = new Task[]
            {
                WhenTheSettingProviderInitializedAsync(),
                WhenTheSettingRepositoryInitializedAsync(),
                WhenTheInstanceIdentifierInitializedAsync(),
                WhenTheLoggerFileInitializedAsync(),
                WhenTheSqliteDatabaseOptionsFactoryInitializedAsync(),
                WhenTheSqliteDatabaseOptionsInitializedAsync(),
                WhenTheSqliteDatabaseInitializedAsync(),
                WhenTheMetricsDatabaseReadinessInitializedAsync(),
                WhenTheMetricsSummaryRepositoryInitializedAsync(),
                WhenTheRollupsDatabaseReadinessInitializedAsync(),
                WhenTheObservationRollupRepositoryInitializedAsync(),
                WhenThePrecipitationRollupRepositoryInitializedAsync(),
                WhenTheWindRollupRepositoryInitializedAsync(),
                WhenTheLightningRollupRepositoryInitializedAsync(),
                WhenTheStreamShippingDatabaseReadinessInitializedAsync(),
                WhenTheStreamShippingRepositoryInitializedAsync(),
                WhenTheLoggerStreamShippingRepositoryInitializedAsync(),
                WhenTheStationMetadataDatabaseReadinessInitializedAsync(),
                WhenTheStationMetadataRepositoryInitializedAsync(),
                WhenTheLoggingDatabaseReadinessInitializedAsync(),
                WhenTheLoggerSqliteRepositoryInitializedAsync(),
                WhenTheLoggerSQLiteInitializedAsync(),
                WhenTheLoggerResilientInitializedAsync(),
                WhenTheProvenanceTrackerInitializedAsync(),
                WhenTheStreamShippingHttpClientProviderInitializedAsync(),
                WhenTheTempestRestClientInitializedAsync(),
                WhenTheStationMetadataProviderInitializedAsync(),
                WhenTheUnitsOfMeasureInitializerInitializedAsync(),
                WhenTheRawPacketDatabaseReadinessInitializedAsync(),
                WhenTheRawPacketIngestRepositoryInitializedAsync(),
                WhenTheSQLiteRawPacketIngestorInitializedAsync(),
                WhenTheStationMetadataStreamShipperInitializedAsync(),
                WhenTheLightningStreamShipperInitializedAsync(),
                WhenTheLoggerSQLiteStreamShipperInitializedAsync(),
                WhenTheObservationStreamShipperInitializedAsync(),
                WhenThePrecipitationStreamShipperInitializedAsync(),
                WhenTheWindStreamShipperInitializedAsync(),
                WhenTheMetricsSummaryIngestorInitializedAsync(),
                WhenTheMetricsSamplerServiceInitializedAsync(),
                WhenTheRollupsWorkerInitializedAsync(),
                WhenTheSQLiteStationMetadataIngestorInitializedAsync(),
                WhenTheSensorReadingTransformerInitializedAsync(),
                WhenTheUdpListenerInitializedAsync(),
            };

            await Task.WhenAll(tasks)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public Task InitializeAllAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            return InitializeAllAsync(cts.Token);
        }

        // Phase 3: Disposal (optional).
        // Emitted only for instances that require cleanup.
        public void DisposeAll()
        {
        }
    }
}
