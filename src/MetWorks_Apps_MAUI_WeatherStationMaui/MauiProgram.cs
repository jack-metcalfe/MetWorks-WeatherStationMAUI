namespace MetWorks.Apps.MAUI.WeatherStationMaui;
public static class MauiProgram
{
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

        // Register ViewModels and Pages for DI-driven page activation
        builder.Services.AddTransient<WeatherViewModel>();
        builder.Services.AddTransient<MetricsOneViewModel>();
        builder.Services.AddTransient<MainSwipeHostPage>();
        builder.Services.AddTransient<LiveWindAdaptive>();
        builder.Services.AddTransient<MetricsOne>();
        builder.Services.AddTransient<MainView1920x1200>();
        builder.Services.AddTransient<MainView2176x1812>();

        // Tempest station metadata persistence (PostgreSQL)
        builder.Services.AddSingleton<MetWorks.Ingest.Postgres.StationMetadataIngestor>();

        builder.Services.AddSingleton<IContentVariantCatalog, ContentVariantCatalog>();
        builder.Services.AddSingleton<IDeviceOverrideSource, YamlDeviceOverrideSource>();
        builder.Services.AddSingleton<IHostCompositionCatalog, HostCompositionCatalog>();
        builder.Services.AddTransient<IContentViewFactory, ContentViewFactory>();

        return builder.Build();
    }
}
