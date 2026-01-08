using MetWorksWeather.Pages;
using MetWorksWeather.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MetWorksWeather;

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

        return builder.Build();
    }
}
