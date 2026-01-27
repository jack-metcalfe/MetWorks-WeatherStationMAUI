// Template:            ExposeToMauiDi
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Template:            File.Header
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Generated On:        2026-01-26T23:47:15.2298447Z
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
            MetWorks.InstanceIdentifier.InstanceIdentifier
                        _TheInstanceIdentifier = GetTheInstanceIdentifier();
            services.AddSingleton<MetWorks.InstanceIdentifier.InstanceIdentifier>
                (_TheInstanceIdentifier);
            MetWorks.Interfaces.ILoggerFile
                        _TheLoggerFile = GetTheLoggerFile();
            services.AddSingleton<MetWorks.Interfaces.ILoggerFile>
                (_TheLoggerFile);
            MetWorks.Interfaces.ILoggerPostgreSQL
                        _TheLoggerPostgreSQL = GetTheLoggerPostgreSQL();
            services.AddSingleton<MetWorks.Interfaces.ILoggerPostgreSQL>
                (_TheLoggerPostgreSQL);
            MetWorks.Interfaces.ILoggerResilient
                        _TheLoggerResilient = GetTheLoggerResilient();
            services.AddSingleton<MetWorks.Interfaces.ILoggerResilient>
                (_TheLoggerResilient);

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
