// Template:            Accessors
// Version:             1.1
// Template Requested:  Accessors
// Template:            File.Header
// Version:             1.1
// Template Requested:  Accessors
// Generated On:        2026-03-01T03:31:43.3092815Z
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
        public System.Threading.CancellationTokenSource GetTheRootCancellationTokenSource()
        {

            return _TheRootCancellationTokenSourceInstance;
        }

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
        public IPlatformPaths GetTheDefaultPlatformPaths()
        {

            return _TheDefaultPlatformPathsInstance;
        }

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
        public MetWorks.Interfaces.IEventRelayBasic GetTheEventRelayBasic()
        {

            return _TheEventRelayBasicInstance;
        }

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
        public MetWorks.Interfaces.IEventRelayPath GetTheEventRelayPath()
        {

            return _TheEventRelayPathInstance;
        }

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
        public MetWorks.Interfaces.ILogger GetTheLoggerStub()
        {

            return _TheLoggerStubInstance;
        }

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
        public MetWorks.Common.Utility.SqliteWriteCoordinator GetTheSqliteWriteCoordinator()
        {

            return _TheSqliteWriteCoordinatorInstance;
        }

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
        public MetWorks.Common.Metrics.IMetricsLatestSnapshot GetTheMetricsLatestSnapshotStore()
        {

            return _TheMetricsLatestSnapshotStoreInstance;
        }

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
        public MetWorks.Interfaces.ISettingProvider GetTheSettingProvider()
        {
            var initTask = _initTask_TheSettingProvider;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSettingProvider' was accessed before initialization started. Await registry.WhenTheSettingProviderInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSettingProvider().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSettingProvider' was accessed before initialization completed. Await registry.WhenTheSettingProviderInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSettingProvider().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSettingProvider' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSettingProvider' initialization failed.",
                    initTask.Exception);

            return _TheSettingProviderInstance;
        }

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
        public MetWorks.Interfaces.ISettingRepository GetTheSettingRepository()
        {
            var initTask = _initTask_TheSettingRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSettingRepository' was accessed before initialization started. Await registry.WhenTheSettingRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSettingRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSettingRepository' was accessed before initialization completed. Await registry.WhenTheSettingRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSettingRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSettingRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSettingRepository' initialization failed.",
                    initTask.Exception);

            return _TheSettingRepositoryInstance;
        }

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
        public MetWorks.Interfaces.IInstanceIdentifier GetTheInstanceIdentifier()
        {
            var initTask = _initTask_TheInstanceIdentifier;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheInstanceIdentifier' was accessed before initialization started. Await registry.WhenTheInstanceIdentifierInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheInstanceIdentifier().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheInstanceIdentifier' was accessed before initialization completed. Await registry.WhenTheInstanceIdentifierInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheInstanceIdentifier().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheInstanceIdentifier' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheInstanceIdentifier' initialization failed.",
                    initTask.Exception);

            return _TheInstanceIdentifierInstance;
        }

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
        public MetWorks.Interfaces.ILogger GetTheLoggerFile()
        {
            var initTask = _initTask_TheLoggerFile;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerFile' was accessed before initialization started. Await registry.WhenTheLoggerFileInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerFile().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerFile' was accessed before initialization completed. Await registry.WhenTheLoggerFileInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerFile().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerFile' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerFile' initialization failed.",
                    initTask.Exception);

            return _TheLoggerFileInstance;
        }

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
        public MetWorks.Common.Settings.SqliteDatabaseOptionsFactory GetTheSqliteDatabaseOptionsFactory()
        {
            var initTask = _initTask_TheSqliteDatabaseOptionsFactory;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabaseOptionsFactory' was accessed before initialization started. Await registry.WhenTheSqliteDatabaseOptionsFactoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSqliteDatabaseOptionsFactory().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabaseOptionsFactory' was accessed before initialization completed. Await registry.WhenTheSqliteDatabaseOptionsFactoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSqliteDatabaseOptionsFactory().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabaseOptionsFactory' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabaseOptionsFactory' initialization failed.",
                    initTask.Exception);

            return _TheSqliteDatabaseOptionsFactoryInstance;
        }

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
        public MetWorks.Data.Sqlite.SqliteDatabaseOptions GetTheSqliteDatabaseOptions()
        {
            var initTask = _initTask_TheSqliteDatabaseOptions;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabaseOptions' was accessed before initialization started. Await registry.WhenTheSqliteDatabaseOptionsInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSqliteDatabaseOptions().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabaseOptions' was accessed before initialization completed. Await registry.WhenTheSqliteDatabaseOptionsInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSqliteDatabaseOptions().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabaseOptions' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabaseOptions' initialization failed.",
                    initTask.Exception);

            return _TheSqliteDatabaseOptionsInstance;
        }

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
        public MetWorks.Data.Sqlite.ISqliteDatabase GetTheSqliteDatabase()
        {
            var initTask = _initTask_TheSqliteDatabase;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabase' was accessed before initialization started. Await registry.WhenTheSqliteDatabaseInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSqliteDatabase().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabase' was accessed before initialization completed. Await registry.WhenTheSqliteDatabaseInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSqliteDatabase().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabase' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSqliteDatabase' initialization failed.",
                    initTask.Exception);

            return _TheSqliteDatabaseInstance;
        }

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
        public MetWorks.Persistence.Metrics.IMetricsDatabaseReadiness GetTheMetricsDatabaseReadiness()
        {
            var initTask = _initTask_TheMetricsDatabaseReadiness;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsDatabaseReadiness' was accessed before initialization started. Await registry.WhenTheMetricsDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheMetricsDatabaseReadiness().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsDatabaseReadiness' was accessed before initialization completed. Await registry.WhenTheMetricsDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheMetricsDatabaseReadiness().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsDatabaseReadiness' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsDatabaseReadiness' initialization failed.",
                    initTask.Exception);

            return _TheMetricsDatabaseReadinessInstance;
        }

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
        public MetWorks.Persistence.Metrics.IMetricsSummaryRepository GetTheMetricsSummaryRepository()
        {
            var initTask = _initTask_TheMetricsSummaryRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSummaryRepository' was accessed before initialization started. Await registry.WhenTheMetricsSummaryRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheMetricsSummaryRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSummaryRepository' was accessed before initialization completed. Await registry.WhenTheMetricsSummaryRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheMetricsSummaryRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSummaryRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSummaryRepository' initialization failed.",
                    initTask.Exception);

            return _TheMetricsSummaryRepositoryInstance;
        }

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
        public MetWorks.Persistence.Rollups.IRollupsDatabaseReadiness GetTheRollupsDatabaseReadiness()
        {
            var initTask = _initTask_TheRollupsDatabaseReadiness;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRollupsDatabaseReadiness' was accessed before initialization started. Await registry.WhenTheRollupsDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheRollupsDatabaseReadiness().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRollupsDatabaseReadiness' was accessed before initialization completed. Await registry.WhenTheRollupsDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheRollupsDatabaseReadiness().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRollupsDatabaseReadiness' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRollupsDatabaseReadiness' initialization failed.",
                    initTask.Exception);

            return _TheRollupsDatabaseReadinessInstance;
        }

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
        public MetWorks.Persistence.Rollups.IObservationRollupRepository GetTheObservationRollupRepository()
        {
            var initTask = _initTask_TheObservationRollupRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheObservationRollupRepository' was accessed before initialization started. Await registry.WhenTheObservationRollupRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheObservationRollupRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheObservationRollupRepository' was accessed before initialization completed. Await registry.WhenTheObservationRollupRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheObservationRollupRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheObservationRollupRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheObservationRollupRepository' initialization failed.",
                    initTask.Exception);

            return _TheObservationRollupRepositoryInstance;
        }

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
        public MetWorks.Persistence.Rollups.IPrecipitationRollupRepository GetThePrecipitationRollupRepository()
        {
            var initTask = _initTask_ThePrecipitationRollupRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'ThePrecipitationRollupRepository' was accessed before initialization started. Await registry.WhenThePrecipitationRollupRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetThePrecipitationRollupRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'ThePrecipitationRollupRepository' was accessed before initialization completed. Await registry.WhenThePrecipitationRollupRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetThePrecipitationRollupRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'ThePrecipitationRollupRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'ThePrecipitationRollupRepository' initialization failed.",
                    initTask.Exception);

            return _ThePrecipitationRollupRepositoryInstance;
        }

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
        public MetWorks.Persistence.Rollups.IWindRollupRepository GetTheWindRollupRepository()
        {
            var initTask = _initTask_TheWindRollupRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheWindRollupRepository' was accessed before initialization started. Await registry.WhenTheWindRollupRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheWindRollupRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheWindRollupRepository' was accessed before initialization completed. Await registry.WhenTheWindRollupRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheWindRollupRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheWindRollupRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheWindRollupRepository' initialization failed.",
                    initTask.Exception);

            return _TheWindRollupRepositoryInstance;
        }

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
        public MetWorks.Persistence.Rollups.ILightningRollupRepository GetTheLightningRollupRepository()
        {
            var initTask = _initTask_TheLightningRollupRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLightningRollupRepository' was accessed before initialization started. Await registry.WhenTheLightningRollupRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLightningRollupRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLightningRollupRepository' was accessed before initialization completed. Await registry.WhenTheLightningRollupRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLightningRollupRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLightningRollupRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLightningRollupRepository' initialization failed.",
                    initTask.Exception);

            return _TheLightningRollupRepositoryInstance;
        }

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
        public MetWorks.Persistence.StreamShipping.IStreamShippingDatabaseReadiness GetTheStreamShippingDatabaseReadiness()
        {
            var initTask = _initTask_TheStreamShippingDatabaseReadiness;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingDatabaseReadiness' was accessed before initialization started. Await registry.WhenTheStreamShippingDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStreamShippingDatabaseReadiness().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingDatabaseReadiness' was accessed before initialization completed. Await registry.WhenTheStreamShippingDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStreamShippingDatabaseReadiness().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingDatabaseReadiness' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingDatabaseReadiness' initialization failed.",
                    initTask.Exception);

            return _TheStreamShippingDatabaseReadinessInstance;
        }

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
        public MetWorks.Persistence.StreamShipping.IStreamShippingRepository GetTheStreamShippingRepository()
        {
            var initTask = _initTask_TheStreamShippingRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingRepository' was accessed before initialization started. Await registry.WhenTheStreamShippingRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStreamShippingRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingRepository' was accessed before initialization completed. Await registry.WhenTheStreamShippingRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStreamShippingRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingRepository' initialization failed.",
                    initTask.Exception);

            return _TheStreamShippingRepositoryInstance;
        }

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
        public MetWorks.Persistence.StreamShipping.ILoggerStreamShippingRepository GetTheLoggerStreamShippingRepository()
        {
            var initTask = _initTask_TheLoggerStreamShippingRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerStreamShippingRepository' was accessed before initialization started. Await registry.WhenTheLoggerStreamShippingRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerStreamShippingRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerStreamShippingRepository' was accessed before initialization completed. Await registry.WhenTheLoggerStreamShippingRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerStreamShippingRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerStreamShippingRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerStreamShippingRepository' initialization failed.",
                    initTask.Exception);

            return _TheLoggerStreamShippingRepositoryInstance;
        }

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
        public MetWorks.Persistence.StationMetadata.IStationMetadataDatabaseReadiness GetTheStationMetadataDatabaseReadiness()
        {
            var initTask = _initTask_TheStationMetadataDatabaseReadiness;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataDatabaseReadiness' was accessed before initialization started. Await registry.WhenTheStationMetadataDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStationMetadataDatabaseReadiness().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataDatabaseReadiness' was accessed before initialization completed. Await registry.WhenTheStationMetadataDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStationMetadataDatabaseReadiness().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataDatabaseReadiness' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataDatabaseReadiness' initialization failed.",
                    initTask.Exception);

            return _TheStationMetadataDatabaseReadinessInstance;
        }

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
        public MetWorks.Persistence.StationMetadata.IStationMetadataRepository GetTheStationMetadataRepository()
        {
            var initTask = _initTask_TheStationMetadataRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataRepository' was accessed before initialization started. Await registry.WhenTheStationMetadataRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStationMetadataRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataRepository' was accessed before initialization completed. Await registry.WhenTheStationMetadataRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStationMetadataRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataRepository' initialization failed.",
                    initTask.Exception);

            return _TheStationMetadataRepositoryInstance;
        }

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
        public MetWorks.Persistence.Logging.ILoggingDatabaseReadiness GetTheLoggingDatabaseReadiness()
        {
            var initTask = _initTask_TheLoggingDatabaseReadiness;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggingDatabaseReadiness' was accessed before initialization started. Await registry.WhenTheLoggingDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggingDatabaseReadiness().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggingDatabaseReadiness' was accessed before initialization completed. Await registry.WhenTheLoggingDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggingDatabaseReadiness().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggingDatabaseReadiness' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggingDatabaseReadiness' initialization failed.",
                    initTask.Exception);

            return _TheLoggingDatabaseReadinessInstance;
        }

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
        public MetWorks.Persistence.Logging.ILoggerSqliteRepository GetTheLoggerSqliteRepository()
        {
            var initTask = _initTask_TheLoggerSqliteRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSqliteRepository' was accessed before initialization started. Await registry.WhenTheLoggerSqliteRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerSqliteRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSqliteRepository' was accessed before initialization completed. Await registry.WhenTheLoggerSqliteRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerSqliteRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSqliteRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSqliteRepository' initialization failed.",
                    initTask.Exception);

            return _TheLoggerSqliteRepositoryInstance;
        }

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
        public MetWorks.Interfaces.ILogger GetTheLoggerSQLite()
        {
            var initTask = _initTask_TheLoggerSQLite;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSQLite' was accessed before initialization started. Await registry.WhenTheLoggerSQLiteInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerSQLite().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSQLite' was accessed before initialization completed. Await registry.WhenTheLoggerSQLiteInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerSQLite().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSQLite' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSQLite' initialization failed.",
                    initTask.Exception);

            return _TheLoggerSQLiteInstance;
        }

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
        public MetWorks.Interfaces.ILogger GetTheLoggerResilient()
        {
            var initTask = _initTask_TheLoggerResilient;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerResilient' was accessed before initialization started. Await registry.WhenTheLoggerResilientInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerResilient().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerResilient' was accessed before initialization completed. Await registry.WhenTheLoggerResilientInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerResilient().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerResilient' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerResilient' initialization failed.",
                    initTask.Exception);

            return _TheLoggerResilientInstance;
        }

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
        public MetWorks.Common.ProvenanceTracker GetTheProvenanceTracker()
        {
            var initTask = _initTask_TheProvenanceTracker;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheProvenanceTracker' was accessed before initialization started. Await registry.WhenTheProvenanceTrackerInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheProvenanceTracker().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheProvenanceTracker' was accessed before initialization completed. Await registry.WhenTheProvenanceTrackerInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheProvenanceTracker().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheProvenanceTracker' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheProvenanceTracker' initialization failed.",
                    initTask.Exception);

            return _TheProvenanceTrackerInstance;
        }

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
        public MetWorks.Common.Networking.StreamShippingHttpClientProvider GetTheStreamShippingHttpClientProvider()
        {
            var initTask = _initTask_TheStreamShippingHttpClientProvider;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingHttpClientProvider' was accessed before initialization started. Await registry.WhenTheStreamShippingHttpClientProviderInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStreamShippingHttpClientProvider().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingHttpClientProvider' was accessed before initialization completed. Await registry.WhenTheStreamShippingHttpClientProviderInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStreamShippingHttpClientProvider().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingHttpClientProvider' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStreamShippingHttpClientProvider' initialization failed.",
                    initTask.Exception);

            return _TheStreamShippingHttpClientProviderInstance;
        }

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
        public ITempestRestClient GetTheTempestRestClient()
        {
            var initTask = _initTask_TheTempestRestClient;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheTempestRestClient' was accessed before initialization started. Await registry.WhenTheTempestRestClientInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheTempestRestClient().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheTempestRestClient' was accessed before initialization completed. Await registry.WhenTheTempestRestClientInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheTempestRestClient().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheTempestRestClient' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheTempestRestClient' initialization failed.",
                    initTask.Exception);

            return _TheTempestRestClientInstance;
        }

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
        public MetWorks.Interfaces.IStationMetadataProvider GetTheStationMetadataProvider()
        {
            var initTask = _initTask_TheStationMetadataProvider;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataProvider' was accessed before initialization started. Await registry.WhenTheStationMetadataProviderInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStationMetadataProvider().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataProvider' was accessed before initialization completed. Await registry.WhenTheStationMetadataProviderInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStationMetadataProvider().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataProvider' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataProvider' initialization failed.",
                    initTask.Exception);

            return _TheStationMetadataProviderInstance;
        }

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
        public MetWorks.RedStar.Amounts.WeatherExtensions.UnitsOfMeasureInitializer GetTheUnitsOfMeasureInitializer()
        {
            var initTask = _initTask_TheUnitsOfMeasureInitializer;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheUnitsOfMeasureInitializer' was accessed before initialization started. Await registry.WhenTheUnitsOfMeasureInitializerInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheUnitsOfMeasureInitializer().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheUnitsOfMeasureInitializer' was accessed before initialization completed. Await registry.WhenTheUnitsOfMeasureInitializerInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheUnitsOfMeasureInitializer().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheUnitsOfMeasureInitializer' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheUnitsOfMeasureInitializer' initialization failed.",
                    initTask.Exception);

            return _TheUnitsOfMeasureInitializerInstance;
        }

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
        public MetWorks.Persistence.Ingest.IRawPacketDatabaseReadiness GetTheRawPacketDatabaseReadiness()
        {
            var initTask = _initTask_TheRawPacketDatabaseReadiness;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRawPacketDatabaseReadiness' was accessed before initialization started. Await registry.WhenTheRawPacketDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheRawPacketDatabaseReadiness().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRawPacketDatabaseReadiness' was accessed before initialization completed. Await registry.WhenTheRawPacketDatabaseReadinessInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheRawPacketDatabaseReadiness().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRawPacketDatabaseReadiness' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRawPacketDatabaseReadiness' initialization failed.",
                    initTask.Exception);

            return _TheRawPacketDatabaseReadinessInstance;
        }

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
        public MetWorks.Persistence.Ingest.IRawPacketIngestRepository GetTheRawPacketIngestRepository()
        {
            var initTask = _initTask_TheRawPacketIngestRepository;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRawPacketIngestRepository' was accessed before initialization started. Await registry.WhenTheRawPacketIngestRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheRawPacketIngestRepository().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRawPacketIngestRepository' was accessed before initialization completed. Await registry.WhenTheRawPacketIngestRepositoryInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheRawPacketIngestRepository().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRawPacketIngestRepository' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRawPacketIngestRepository' initialization failed.",
                    initTask.Exception);

            return _TheRawPacketIngestRepositoryInstance;
        }

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
        public MetWorks.Ingest.SQLite.RawPacketIngestor GetTheSQLiteRawPacketIngestor()
        {
            var initTask = _initTask_TheSQLiteRawPacketIngestor;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSQLiteRawPacketIngestor' was accessed before initialization started. Await registry.WhenTheSQLiteRawPacketIngestorInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSQLiteRawPacketIngestor().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSQLiteRawPacketIngestor' was accessed before initialization completed. Await registry.WhenTheSQLiteRawPacketIngestorInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSQLiteRawPacketIngestor().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSQLiteRawPacketIngestor' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSQLiteRawPacketIngestor' initialization failed.",
                    initTask.Exception);

            return _TheSQLiteRawPacketIngestorInstance;
        }

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
        public MetWorks.Ingest.SQLite.Shipping.StationMetadataStreamShipper GetTheStationMetadataStreamShipper()
        {
            var initTask = _initTask_TheStationMetadataStreamShipper;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataStreamShipper' was accessed before initialization started. Await registry.WhenTheStationMetadataStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStationMetadataStreamShipper().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataStreamShipper' was accessed before initialization completed. Await registry.WhenTheStationMetadataStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheStationMetadataStreamShipper().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataStreamShipper' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheStationMetadataStreamShipper' initialization failed.",
                    initTask.Exception);

            return _TheStationMetadataStreamShipperInstance;
        }

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
        public MetWorks.Ingest.SQLite.Shipping.LightningStreamShipper GetTheLightningStreamShipper()
        {
            var initTask = _initTask_TheLightningStreamShipper;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLightningStreamShipper' was accessed before initialization started. Await registry.WhenTheLightningStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLightningStreamShipper().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLightningStreamShipper' was accessed before initialization completed. Await registry.WhenTheLightningStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLightningStreamShipper().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLightningStreamShipper' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLightningStreamShipper' initialization failed.",
                    initTask.Exception);

            return _TheLightningStreamShipperInstance;
        }

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
        public MetWorks.Ingest.SQLite.Shipping.LoggerSQLiteStreamShipper GetTheLoggerSQLiteStreamShipper()
        {
            var initTask = _initTask_TheLoggerSQLiteStreamShipper;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSQLiteStreamShipper' was accessed before initialization started. Await registry.WhenTheLoggerSQLiteStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerSQLiteStreamShipper().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSQLiteStreamShipper' was accessed before initialization completed. Await registry.WhenTheLoggerSQLiteStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheLoggerSQLiteStreamShipper().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSQLiteStreamShipper' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheLoggerSQLiteStreamShipper' initialization failed.",
                    initTask.Exception);

            return _TheLoggerSQLiteStreamShipperInstance;
        }

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
        public MetWorks.Ingest.SQLite.Shipping.ObservationStreamShipper GetTheObservationStreamShipper()
        {
            var initTask = _initTask_TheObservationStreamShipper;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheObservationStreamShipper' was accessed before initialization started. Await registry.WhenTheObservationStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheObservationStreamShipper().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheObservationStreamShipper' was accessed before initialization completed. Await registry.WhenTheObservationStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheObservationStreamShipper().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheObservationStreamShipper' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheObservationStreamShipper' initialization failed.",
                    initTask.Exception);

            return _TheObservationStreamShipperInstance;
        }

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
        public MetWorks.Ingest.SQLite.Shipping.PrecipitationStreamShipper GetThePrecipitationStreamShipper()
        {
            var initTask = _initTask_ThePrecipitationStreamShipper;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'ThePrecipitationStreamShipper' was accessed before initialization started. Await registry.WhenThePrecipitationStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetThePrecipitationStreamShipper().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'ThePrecipitationStreamShipper' was accessed before initialization completed. Await registry.WhenThePrecipitationStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetThePrecipitationStreamShipper().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'ThePrecipitationStreamShipper' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'ThePrecipitationStreamShipper' initialization failed.",
                    initTask.Exception);

            return _ThePrecipitationStreamShipperInstance;
        }

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
        public MetWorks.Ingest.SQLite.Shipping.WindStreamShipper GetTheWindStreamShipper()
        {
            var initTask = _initTask_TheWindStreamShipper;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheWindStreamShipper' was accessed before initialization started. Await registry.WhenTheWindStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheWindStreamShipper().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheWindStreamShipper' was accessed before initialization completed. Await registry.WhenTheWindStreamShipperInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheWindStreamShipper().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheWindStreamShipper' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheWindStreamShipper' initialization failed.",
                    initTask.Exception);

            return _TheWindStreamShipperInstance;
        }

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
        public IMetricsSummaryPersister GetTheMetricsSummaryIngestor()
        {
            var initTask = _initTask_TheMetricsSummaryIngestor;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSummaryIngestor' was accessed before initialization started. Await registry.WhenTheMetricsSummaryIngestorInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheMetricsSummaryIngestor().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSummaryIngestor' was accessed before initialization completed. Await registry.WhenTheMetricsSummaryIngestorInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheMetricsSummaryIngestor().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSummaryIngestor' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSummaryIngestor' initialization failed.",
                    initTask.Exception);

            return _TheMetricsSummaryIngestorInstance;
        }

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
        public MetWorks.Common.Metrics.MetricsSamplerService GetTheMetricsSamplerService()
        {
            var initTask = _initTask_TheMetricsSamplerService;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSamplerService' was accessed before initialization started. Await registry.WhenTheMetricsSamplerServiceInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheMetricsSamplerService().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSamplerService' was accessed before initialization completed. Await registry.WhenTheMetricsSamplerServiceInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheMetricsSamplerService().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSamplerService' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheMetricsSamplerService' initialization failed.",
                    initTask.Exception);

            return _TheMetricsSamplerServiceInstance;
        }

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
        public MetWorks.Ingest.SQLite.Rollups.RollupsWorker GetTheRollupsWorker()
        {
            var initTask = _initTask_TheRollupsWorker;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRollupsWorker' was accessed before initialization started. Await registry.WhenTheRollupsWorkerInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheRollupsWorker().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRollupsWorker' was accessed before initialization completed. Await registry.WhenTheRollupsWorkerInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheRollupsWorker().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRollupsWorker' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheRollupsWorker' initialization failed.",
                    initTask.Exception);

            return _TheRollupsWorkerInstance;
        }

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
        public MetWorks.Ingest.SQLite.StationMetadataIngestor GetTheSQLiteStationMetadataIngestor()
        {
            var initTask = _initTask_TheSQLiteStationMetadataIngestor;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSQLiteStationMetadataIngestor' was accessed before initialization started. Await registry.WhenTheSQLiteStationMetadataIngestorInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSQLiteStationMetadataIngestor().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSQLiteStationMetadataIngestor' was accessed before initialization completed. Await registry.WhenTheSQLiteStationMetadataIngestorInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSQLiteStationMetadataIngestor().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSQLiteStationMetadataIngestor' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSQLiteStationMetadataIngestor' initialization failed.",
                    initTask.Exception);

            return _TheSQLiteStationMetadataIngestorInstance;
        }

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
        public MetWorks.Ingest.Transformer.SensorReadingTransformer GetTheSensorReadingTransformer()
        {
            var initTask = _initTask_TheSensorReadingTransformer;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSensorReadingTransformer' was accessed before initialization started. Await registry.WhenTheSensorReadingTransformerInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSensorReadingTransformer().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSensorReadingTransformer' was accessed before initialization completed. Await registry.WhenTheSensorReadingTransformerInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheSensorReadingTransformer().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSensorReadingTransformer' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheSensorReadingTransformer' initialization failed.",
                    initTask.Exception);

            return _TheSensorReadingTransformerInstance;
        }

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
        public MetWorks.Networking.Udp.Transformer.TempestPacketTransformer GetTheUdpListener()
        {
            var initTask = _initTask_TheUdpListener;
            if (initTask is null)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheUdpListener' was accessed before initialization started. Await registry.WhenTheUdpListenerInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheUdpListener().");

            if (!initTask.IsCompleted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheUdpListener' was accessed before initialization completed. Await registry.WhenTheUdpListenerInitializedAsync() (or registry.InitializeAllAsync()) before calling GetTheUdpListener().");

            if (initTask.IsCanceled)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheUdpListener' initialization was canceled.",
                    initTask.Exception);

            if (initTask.IsFaulted)
                throw new global::System.InvalidOperationException(
                    "DDI: instance 'TheUdpListener' initialization failed.",
                    initTask.Exception);

            return _TheUdpListenerInstance;
        }

        // Internal accessor: always returns the concrete class.
        internal MetWorks.Networking.Udp.Transformer.TempestPacketTransformer GetTheUdpListener_Internal() =>
            _TheUdpListenerInstance;
    }
}