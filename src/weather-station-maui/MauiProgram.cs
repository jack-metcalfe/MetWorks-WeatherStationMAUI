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
        try
        {
            if (!StartupInitializer.IsInitialized)
            {
                // This blocks startup to ensure DDI initialization happens before DI registrations.
                // StartupInitializer.InitializeAsync is idempotent and safe to call multiple times.
                try
                {
                    Debug.WriteLine("🔄 Running StartupInitializer.InitializeAsync() from CreateMauiApp...");
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    StartupInitializer.InitializeAsync().GetAwaiter().GetResult();
                    sw.Stop();
                    Debug.WriteLine($"🔁 StartupInitializer completed in {sw.Elapsed.TotalMilliseconds:n0} ms");
                }
                catch (Exception ex)
                {
                    // Don't crash the app here; log for diagnostics and continue - registry may still be available.
                    try { Debug.WriteLine($"⚠️ StartupInitializer failed during CreateMauiApp: {ex.Message}"); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            try { Debug.WriteLine($"⚠️ Unexpected error while ensuring StartupInitializer: {ex.Message}"); } catch { }
        }

        // ---- Invoke generated DDI registry to create/initialize and register singletons ----
        // This expects the generated registry to expose:
        //   Task RegisterSingletonsAsync(IServiceCollection services, CancellationToken cancellationToken = default)
        //
        // The generated DDI remains responsible for creation + async initialization.
        // Here we call into the generated registry to perform those steps and have it
        // register instances directly into the MAUI IServiceCollection.
        try
        {
            var registry = StartupInitializer.Registry;
            if (registry != null)
            {
                // Prefer a meaningful external token if the registry exposes one; otherwise use None.
                CancellationToken token = CancellationToken.None;
                try
                {
                    // Many generated registries provide an accessor for the external cancellation token.
                    // If present this will supply a token that follows application lifetime.
                    token = registry.GetRootCancellationTokenSource().Token;
                }
                catch
                {
                    // ignore - fall back to CancellationToken.None
                }

                try
                {
                    // Block here because CreateMauiApp is synchronous; generated method performs async init.
                    registry.RegisterSingletonsInMauiAsync(builder.Services, token).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    // Avoid failing app startup on DDI registration errors; log for diagnostics.
                    try { Debug.WriteLine($"DDI registration failed: {ex.Message}"); } catch { }
                }
            }
            else
            {
                try { Debug.WriteLine("DDI registry not available at startup; skipping DDI registrations."); } catch { }
            }
        }
        catch (Exception ex)
        {
            try { Debug.WriteLine($"Unexpected error while registering DDI singletons: {ex.Message}"); } catch { }
        }

        return builder.Build();
    }
}
