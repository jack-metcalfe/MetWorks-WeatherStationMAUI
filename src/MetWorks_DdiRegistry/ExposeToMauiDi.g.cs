// Template:            ExposeToMauiDi
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Template:            File.Header
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Generated On:        2026-02-09T02:55:58.0510994Z
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
            MetWorks.Common.Metrics.IMetricsLatestSnapshot
                        _TheMetricsLatestSnapshotStore = GetTheMetricsLatestSnapshotStore();
            services.AddSingleton<MetWorks.Common.Metrics.IMetricsLatestSnapshot>
                (_TheMetricsLatestSnapshotStore);
            MetWorks.Interfaces.IEventRelayBasic
                        _TheEventRelayBasic = GetTheEventRelayBasic();
            services.AddSingleton<MetWorks.Interfaces.IEventRelayBasic>
                (_TheEventRelayBasic);
            MetWorks.Interfaces.IEventRelayPath
                        _TheEventRelayPath = GetTheEventRelayPath();
            services.AddSingleton<MetWorks.Interfaces.IEventRelayPath>
                (_TheEventRelayPath);
            MetWorks.Interfaces.ILoggerStub
                        _TheLoggerStub = GetTheLoggerStub();
            services.AddSingleton<MetWorks.Interfaces.ILoggerStub>
                (_TheLoggerStub);
            MetWorks.Interfaces.ISettingRepository
                        _TheSettingRepository = GetTheSettingRepository();
            services.AddSingleton<MetWorks.Interfaces.ISettingRepository>
                (_TheSettingRepository);
            MetWorks.Interfaces.IInstanceIdentifier
                        _TheInstanceIdentifier = GetTheInstanceIdentifier();
            services.AddSingleton<MetWorks.Interfaces.IInstanceIdentifier>
                (_TheInstanceIdentifier);
            MetWorks.Interfaces.ILoggerFile
                        _TheLoggerFile = GetTheLoggerFile();
            services.AddSingleton<MetWorks.Interfaces.ILoggerFile>
                (_TheLoggerFile);
            MetWorks.Interfaces.ILoggerPostgreSQL
                        _TheLoggerPostgreSQL = GetTheLoggerPostgreSQL();
            services.AddSingleton<MetWorks.Interfaces.ILoggerPostgreSQL>
                (_TheLoggerPostgreSQL);
            MetWorks.Interfaces.ILoggerSQLite
                        _TheLoggerSQLite = GetTheLoggerSQLite();
            services.AddSingleton<MetWorks.Interfaces.ILoggerSQLite>
                (_TheLoggerSQLite);
            MetWorks.Interfaces.ILoggerResilient
                        _TheLoggerResilient = GetTheLoggerResilient();
            services.AddSingleton<MetWorks.Interfaces.ILoggerResilient>
                (_TheLoggerResilient);
            MetWorks.Interfaces.IStationMetadataProvider
                        _TheStationMetadataProvider = GetTheStationMetadataProvider();
            services.AddSingleton<MetWorks.Interfaces.IStationMetadataProvider>
                (_TheStationMetadataProvider);

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
