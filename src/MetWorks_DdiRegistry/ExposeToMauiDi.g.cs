// Template:            ExposeToMauiDi
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Template:            File.Header
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Generated On:        2026-01-26T05:32:30.5671217Z
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
            MetWorks.Interfaces.ISettingRepository
                        _TheSettingRepository = GetTheSettingRepository();
            services.AddSingleton<MetWorks.Interfaces.ISettingRepository>
                (_TheSettingRepository);
            MetWorks.InstanceIdentifier.InstanceIdentifier
                        _TheInstanceIdentifier = GetTheInstanceIdentifier();
            services.AddSingleton<MetWorks.InstanceIdentifier.InstanceIdentifier>
                (_TheInstanceIdentifier);
            MetWorks.Interfaces.ILogger
                        _TheLoggerPostgreSQL = GetTheLoggerPostgreSQL();
            services.AddSingleton<MetWorks.Interfaces.ILogger>
                (_TheLoggerPostgreSQL);

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
