using Microsoft.Extensions.DependencyInjection;
namespace MetWorks.Apps.MAUI.WeatherStationMaui;
public static class MauiProgram
{
    static bool _ddiRegistered = false;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // NOTE: MockWeatherReadingService is now started in StartupInitializer.cs
        // No need to register it here - it publishes directly to ISingletonEventRelay
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Ensure the generated DDI registry has been created and initialized BEFORE
        // we ask it to register singletons into MAUI's IServiceCollection.
        // This avoids the chicken-or-egg: CreateMauiApp -> registry calls happening
        // before StartupInitializer has created/initialized the registry.
        // Startup initialization is started by the App at runtime to avoid blocking the UI thread here.
        // The generated DDI registry will be registered if available; CreateMauiApp does not block on startup initialization.

        // Create registry and register created (but not yet initialized) instances into MAUI DI.
        // This performs the DDI "create" phase synchronously so builder.Services can be populated
        // before the service provider is built. The longer async initialization runs later (in App).
        try
        {
            StartupInitializer.CreateRegistryAndRegisterServices(builder.Services);
        }
        catch (Exception ex)
        {
            try { Debug.WriteLine($"Failed to create/register DDI singletons at startup: {ex.Message}"); } catch { }
        }

        // Register AppShell so it can be resolved with injected services
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}
