// Template:            ExposeToMauiDi
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Template:            File.Header
// Version:             1.1
// Template Requested:  ExposeToMauiDi
// Generated On:        2026-01-22T04:50:43.7490342Z
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
            MetWorks.Interfaces.ILogger
                        _TheLoggerFile = GetTheLoggerFile();
            services.AddSingleton<MetWorks.Interfaces.ILogger>
                (_TheLoggerFile);

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
