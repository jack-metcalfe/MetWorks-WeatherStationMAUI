// Template:            ExposeToMauiDi
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Template:            File.Header
// Version:             1.1
// Template Requested:  ExposeToMauiDi
#nullable enable

namespace MetWorks.ServiceRegistry
{
    public partial class Registry
    {
        public void RegisterSingletonsInMaui(
            IServiceCollection services
		)
        {
            System.Threading.CancellationToken
                        _TheRootCancellationTokenSource = GetTheRootCancellationTokenSource_Internal().Token;
            services.AddSingleton(typeof(System.Threading.CancellationToken), _TheRootCancellationTokenSource);
            IPlatformPaths
                        _TheDefaultPlatformPaths = GetTheDefaultPlatformPaths_Internal();
            services.AddSingleton<IPlatformPaths>
                (_TheDefaultPlatformPaths);
            MetWorks.Interfaces.IEventRelayBasic
                        _TheEventRelayBasic = GetTheEventRelayBasic_Internal();
            services.AddSingleton<MetWorks.Interfaces.IEventRelayBasic>
                (_TheEventRelayBasic);
            MetWorks.Interfaces.IEventRelayPath
                        _TheEventRelayPath = GetTheEventRelayPath_Internal();
            services.AddSingleton<MetWorks.Interfaces.IEventRelayPath>
                (_TheEventRelayPath);
            MetWorks.Common.Utility.SqliteWriteCoordinator
                        _TheSqliteWriteCoordinator = GetTheSqliteWriteCoordinator_Internal();
            services.AddSingleton<MetWorks.Common.Utility.SqliteWriteCoordinator>
                (_TheSqliteWriteCoordinator);
            MetWorks.Common.Metrics.IMetricsLatestSnapshot
                        _TheMetricsLatestSnapshotStore = GetTheMetricsLatestSnapshotStore_Internal();
            services.AddSingleton<MetWorks.Common.Metrics.IMetricsLatestSnapshot>
                (_TheMetricsLatestSnapshotStore);
            MetWorks.Interfaces.ISettingRepository
                        _TheSettingRepository = GetTheSettingRepository_Internal();
            services.AddSingleton<MetWorks.Interfaces.ISettingRepository>
                (_TheSettingRepository);
            MetWorks.Interfaces.IInstanceIdentifier
                        _TheInstanceIdentifier = GetTheInstanceIdentifier_Internal();
            services.AddSingleton<MetWorks.Interfaces.IInstanceIdentifier>
                (_TheInstanceIdentifier);
            MetWorks.Data.Sqlite.ISqliteDatabase
                        _TheSqliteDatabase = GetTheSqliteDatabase_Internal();
            services.AddSingleton<MetWorks.Data.Sqlite.ISqliteDatabase>
                (_TheSqliteDatabase);
            MetWorks.Interfaces.ILogger
                        _TheLoggerResilient = GetTheLoggerResilient_Internal();
            services.AddSingleton<MetWorks.Interfaces.ILogger>
                (_TheLoggerResilient);
            MetWorks.Interfaces.ITempestOAuthTokenProvider
                        _TheTempestOAuthTokenProvider = GetTheTempestOAuthTokenProvider_Internal();
            services.AddSingleton<MetWorks.Interfaces.ITempestOAuthTokenProvider>
                (_TheTempestOAuthTokenProvider);
            MetWorks.Interfaces.ITempestRestObservationsProvider
                        _TheTempestRestObservationsProvider = GetTheTempestRestObservationsProvider_Internal();
            services.AddSingleton<MetWorks.Interfaces.ITempestRestObservationsProvider>
                (_TheTempestRestObservationsProvider);
            MetWorks.Interfaces.IStationMetadataProvider
                        _TheStationMetadataProvider = GetTheStationMetadataProvider_Internal();
            services.AddSingleton<MetWorks.Interfaces.IStationMetadataProvider>
                (_TheStationMetadataProvider);
            MetWorks.Interfaces.ITempestForecastProvider
                        _TheTempestForecastProvider = GetTheTempestForecastProvider_Internal();
            services.AddSingleton<MetWorks.Interfaces.ITempestForecastProvider>
                (_TheTempestForecastProvider);
            MetWorks.Ingest.SQLite.StationMetadataIngestor
                        _TheSQLiteStationMetadataIngestor = GetTheSQLiteStationMetadataIngestor_Internal();
            services.AddSingleton<MetWorks.Ingest.SQLite.StationMetadataIngestor>
                (_TheSQLiteStationMetadataIngestor);
        }

        // This is the method MauiProgram.cs is calling.
        public Task RegisterSingletonsInMauiAsync(
            IServiceCollection services,
            CancellationToken cancellationToken = default
		)
        {
            RegisterSingletonsInMaui(services);
            return Task.CompletedTask;
        }
    }
}
