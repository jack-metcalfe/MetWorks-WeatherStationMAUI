namespace MetWorks.Apps.MAUI.WeatherStationMaui.DeviceSelection;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Selects the appropriate view based on current device characteristics.
/// Uses DeviceViewRegistry to find best matching view for the device.
/// </summary>
public static class DeviceViewSelector
{
    /// <summary>
    /// Get the appropriate ContentPage for the current device
    /// </summary>
    public static ContentPage GetPageForCurrentDevice()
    {
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        var deviceInfo = DeviceInfo.Current;
        
        // Get physical characteristics
        var widthPixels = (int)displayInfo.Width;
        var heightPixels = (int)displayInfo.Height;
        var density = displayInfo.Density;
        var platform = deviceInfo.Platform.ToString();
        var deviceModel = deviceInfo.Model;
        var manufacturer = deviceInfo.Manufacturer;

        // Find matching profile
        var profile = DeviceViewRegistry.FindBestMatch(
            widthPixels: widthPixels, 
            heightPixels: heightPixels, 
            density: density, 
            platform: platform,
            deviceModel: deviceModel,
            manufacturer: manufacturer
        );
        
        if (profile != null)
        {
            Debug.WriteLine($"🎯 Matched device: {profile.DeviceName} → {profile.ViewTypeName}");
            Debug.WriteLine($"   Resolution: {widthPixels}x{heightPixels}, Density: {density:F1}, Platform: {platform}");
            Debug.WriteLine($"   Diagonal: {profile.DiagonalInches:F1}\", Logical: {profile.LogicalWidth:F0}x{profile.LogicalHeight:F0}");
            return CreateViewFromTypeName(profile.ViewTypeName);
        }

    
        
        // Fallback to default view
        Debug.WriteLine($"⚠️ No matching profile found for {widthPixels}x{heightPixels} @ {density:F1} DPI on {platform}");
        Debug.WriteLine($"   Model: {deviceModel}, Manufacturer: {manufacturer}");
        Debug.WriteLine($"   Using default WeatherPage");

        return CreateViewFromTypeName("MainView1920x1200");
    }
    
    /// <summary>
    /// Create a view instance from its type name
    /// </summary>
    private static ContentPage CreateViewFromTypeName(string typeName)
    {
        // Map type names to concrete types
        Type? t = typeName switch
        {
            "MainView1920x1200" => typeof(Pages.MainDeviceViews.MainView1920x1200),
            "MainView2304x1440" => typeof(Pages.MainDeviceViews.MainView2304x1440),
            "MainView1440x2304" => typeof(Pages.MainDeviceViews.MainView1440x2304),
            "MainView1812x2176" => typeof(Pages.MainDeviceViews.MainView1812x2176),
            "MainView2176x1812" => typeof(Pages.MainDeviceViews.MainView2176x1812),
            _ => typeof(Pages.MainDeviceViews.MainView1920x1200)
        };

        // Try to resolve the page from MAUI DI so constructors with injected ViewModels work.
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services != null && t != null)
            {
                // Try to create page with DI
                var page = ActivatorUtilities.CreateInstance(services, t) as ContentPage;
                if (page != null) return page;

                // If the page requires a ViewModel in constructor, try resolving the ViewModel then create
                try
                {
                    var vm = services.GetService<WeatherViewModel>();
                    if (vm != null)
                    {
                        page = ActivatorUtilities.CreateInstance(services, t, vm) as ContentPage;
                        if (page != null) return page;
                    }
                }
                catch { }
            }
        }
        catch { /* fall back to registry-based creation below */ }

        // If DI not available, attempt to construct required ViewModel from the Registry and instantiate the page
        try
        {
            var vm = default(WeatherViewModel);
            try
            {
                if (StartupInitializer.IsInitialized)
                {
                    var reg = StartupInitializer.Registry;
                    var logger = reg.GetTheLoggerResilient();
                    var settings = reg.GetTheSettingRepository();
                    var relay = reg.GetTheEventRelayBasic();
                    vm = new WeatherViewModel(logger, settings, relay);
                }
            }
            catch { }

            if (vm != null && t != null)
            {
                try
                {
                    var obj = Activator.CreateInstance(t, vm) as ContentPage;
                    if (obj != null) return obj;
                }
                catch { }
            }
        }
        catch { }

        // As a last resort try parameterless create (some pages may have it)
        try
        {
            if (t != null)
            {
                var obj = Activator.CreateInstance(t) as ContentPage;
                if (obj != null) return obj;
            }
        }
        catch { }

        // Nothing worked - throw to make the failure obvious
        throw new InvalidOperationException($"Unable to create page for type '{typeName}' via DI or fallback constructors.");
    }
    
    /// <summary>
    /// Get current device characteristics as a string (for debugging/logging)
    /// </summary>
    public static string GetDeviceInfo()
    {
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        var deviceInfo = DeviceInfo.Current;
        
        return $"""
            Device: {deviceInfo.Manufacturer} {deviceInfo.Model}
            Platform: {deviceInfo.Platform} {deviceInfo.Version}
            Resolution: {displayInfo.Width}x{displayInfo.Height} pixels
            Density: {displayInfo.Density:F1} DPI
            Orientation: {displayInfo.Orientation}
            Diagonal: {CalculateDiagonalInches(displayInfo):F1} inches
            """;
    }
    
    private static double CalculateDiagonalInches(DisplayInfo display)
    {
        var widthInches = display.Width / display.Density;
        var heightInches = display.Height / display.Density;
        return Math.Sqrt(widthInches * widthInches + heightInches * heightInches);
    }

    /// <summary>
    /// Returns the route name (type name) for the appropriate device page without creating an instance.
    /// </summary>
    public static string GetRouteForCurrentDevice()
    {
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        var deviceInfo = DeviceInfo.Current;

        var widthPixels = (int)displayInfo.Width;
        var heightPixels = (int)displayInfo.Height;
        var density = displayInfo.Density;
        var platform = deviceInfo.Platform.ToString();
        var deviceModel = deviceInfo.Model;
        var manufacturer = deviceInfo.Manufacturer;

        var profile = DeviceViewRegistry.FindBestMatch(
            widthPixels: widthPixels,
            heightPixels: heightPixels,
            density: density,
            platform: platform,
            deviceModel: deviceModel,
            manufacturer: manufacturer
        );

        if (profile != null && !string.IsNullOrWhiteSpace(profile.ViewTypeName))
            return profile.ViewTypeName;

        return nameof(Pages.MainDeviceViews.MainView1920x1200);
    }
}