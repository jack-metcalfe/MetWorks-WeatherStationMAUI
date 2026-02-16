// Template:            ExposeToMauiDi
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Template:            File.Header
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Generated On:        2026-02-15T20:40:23.5606879Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    public partial class Registry
    {
        // This is the method MauiProgram.cs is calling.
        public async Task RegisterSingletonsInMauiAsync(
            IServiceCollection services,
            CancellationToken cancellationToken = default
		)
        {
            System.Threading.CancellationTokenSource
                        _TheRootCancellationTokenSource = GetTheRootCancellationTokenSource();
            services.AddSingleton<System.Threading.CancellationTokenSource>
                (_TheRootCancellationTokenSource);
            IPlatformPaths
                        _TheDefaultPlatformPaths = GetTheDefaultPlatformPaths();
            services.AddSingleton<IPlatformPaths>
                (_TheDefaultPlatformPaths);
            MetWorks.Interfaces.IEventRelayBasic
                        _TheEventRelayBasic = GetTheEventRelayBasic();
            services.AddSingleton<MetWorks.Interfaces.IEventRelayBasic>
                (_TheEventRelayBasic);
            MetWorks.Interfaces.IEventRelayPath
                        _TheEventRelayPath = GetTheEventRelayPath();
            services.AddSingleton<MetWorks.Interfaces.IEventRelayPath>
                (_TheEventRelayPath);
            MetWorks.Common.Utility.SqliteWriteCoordinator
                        _TheSqliteWriteCoordinator = GetTheSqliteWriteCoordinator();
            services.AddSingleton<MetWorks.Common.Utility.SqliteWriteCoordinator>
                (_TheSqliteWriteCoordinator);
            MetWorks.Common.Metrics.IMetricsLatestSnapshot
                        _TheMetricsLatestSnapshotStore = GetTheMetricsLatestSnapshotStore();
            services.AddSingleton<MetWorks.Common.Metrics.IMetricsLatestSnapshot>
                (_TheMetricsLatestSnapshotStore);
            MetWorks.Interfaces.ISettingRepository
                        _TheSettingRepository = GetTheSettingRepository();
            services.AddSingleton<MetWorks.Interfaces.ISettingRepository>
                (_TheSettingRepository);
            MetWorks.Interfaces.IInstanceIdentifier
                        _TheInstanceIdentifier = GetTheInstanceIdentifier();
            services.AddSingleton<MetWorks.Interfaces.IInstanceIdentifier>
                (_TheInstanceIdentifier);
            MetWorks.Data.Sqlite.ISqliteDatabase
                        _TheSqliteDatabase = GetTheSqliteDatabase();
            services.AddSingleton<MetWorks.Data.Sqlite.ISqliteDatabase>
                (_TheSqliteDatabase);
            MetWorks.Interfaces.ILogger
                        _TheLoggerResilient = GetTheLoggerResilient();
            services.AddSingleton<MetWorks.Interfaces.ILogger>
                (_TheLoggerResilient);
            MetWorks.Interfaces.IStationMetadataProvider
                        _TheStationMetadataProvider = GetTheStationMetadataProvider();
            services.AddSingleton<MetWorks.Interfaces.IStationMetadataProvider>
                (_TheStationMetadataProvider);
            MetWorks.Ingest.SQLite.StationMetadataIngestor
                        _TheSQLiteStationMetadataIngestor = GetTheSQLiteStationMetadataIngestor();
            services.AddSingleton<MetWorks.Ingest.SQLite.StationMetadataIngestor>
                (_TheSQLiteStationMetadataIngestor);

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
