namespace MetWorks.Apps.MAUI.WeatherStationMaui.DeviceSelection;

/// <summary>
/// Selects the appropriate view based on current device characteristics.
/// Uses DeviceViewRegistry to find best matching view for the device.
/// </summary>
public static class DeviceViewSelector
{
    /// <summary>
    /// Get the appropriate ContentPage for the current device
    /// </summary>
    public static ContentPage GetPageForCurrentDevice(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic
    )
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
            return CreateViewFromTypeName(profile.ViewTypeName, iLogger, iSettingRepository, iEventRelayBasic);
        }

    
        
        // Fallback to default view
        Debug.WriteLine($"⚠️ No matching profile found for {widthPixels}x{heightPixels} @ {density:F1} DPI on {platform}");
        Debug.WriteLine($"   Model: {deviceModel}, Manufacturer: {manufacturer}");
        Debug.WriteLine($"   Using default WeatherPage");

        return new Pages.MainDeviceViews.MainView1920x1200(iLogger, iSettingRepository, iEventRelayBasic);
    }
    
    /// <summary>
    /// Create a view instance from its type name
    /// </summary>
    private static ContentPage CreateViewFromTypeName(
        string typeName, 
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic
    )
    {
        return typeName switch
        {
            "MainView1920x1200" => new Pages.MainDeviceViews.MainView1920x1200(iLogger, iSettingRepository, iEventRelayBasic),
            "MainView2304x1440" => new Pages.MainDeviceViews.MainView2304x1440(iLogger, iSettingRepository, iEventRelayBasic),
            "MainView1440x2304" => new Pages.MainDeviceViews.MainView1440x2304(iLogger, iSettingRepository, iEventRelayBasic),
            //"MainView1080x2400" => new Pages.MainDeviceViews.MainView1080x2400(),
            "MainView1812x2176" => new Pages.MainDeviceViews.MainView1812x2176(iLogger, iSettingRepository, iEventRelayBasic),
            "MainView2176x1812" => new Pages.MainDeviceViews.MainView2176x1812(iLogger, iSettingRepository, iEventRelayBasic),
            //"MainView2400x1080" => new Pages.MainDeviceViews.MainView2400x1080(),
            //"MainView2485x970" => new Pages.MainDeviceViews.MainView2485x970(),
            //"MainView970x2485" => new Pages.MainDeviceViews.MainView970x2485(),
            _ => new Pages.MainDeviceViews.MainView1920x1200(iLogger, iSettingRepository, iEventRelayBasic) // Fallback
        };
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