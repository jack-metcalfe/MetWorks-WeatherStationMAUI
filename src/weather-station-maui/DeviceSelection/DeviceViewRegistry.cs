using System;
using System.Collections.Generic;
using System.Linq;

namespace MetWorksWeather.DeviceSelection;

/// <summary>
/// Registry of known device profiles mapped to specific view implementations.
/// Add new devices here as they are tested and optimized.
/// </summary>
public static class DeviceViewRegistry
{
    private static readonly List<DeviceProfile> _knownDevices = new()
    {
        new DeviceProfile
        {
            DeviceName = "Z Fold 4 (Portrait)",
            WidthPixels = 1812,
            HeightPixels = 2176,
            Density = 2.625,
            Platform = "Android",
            DeviceModel = "SM-F936U1",
            Manufacturer = "samsung",
            ViewTypeName = "MainView1812x2176",
            PreferredOrientation = "Portrait"
        },
        new DeviceProfile
        {
            DeviceName = "Z Fold 4 (Landscape)",
            WidthPixels = 2176,
            HeightPixels = 1812,
            Density = 2.625,
            Platform = "Android",
            DeviceModel = "SM-F936U",
            Manufacturer = "samsung",
            ViewTypeName = "MainView2176x1812",
            PreferredOrientation = "Landscape"
        },
        new DeviceProfile
        {
            DeviceName = "GE68HX13V",
            WidthPixels = 1920,
            HeightPixels = 1200,
            Density = 1.25,
            Platform = "WinUI",
            DeviceModel = "Raider GE68HX 13VF",
            Manufacturer = "Micro-Star International Co., Ltd.",
            ViewTypeName = "MainView1920x1200",
            PreferredOrientation = "Landscape"
        },
    };
    
    /// <summary>
    /// Get all registered device profiles
    /// </summary>
    public static IReadOnlyList<DeviceProfile> AllDevices => _knownDevices.AsReadOnly();
    
    /// <summary>
    /// Find the best matching device profile for the current device
    /// </summary>
    public static DeviceProfile? FindBestMatch(
        int widthPixels, 
        int heightPixels, 
        double density, 
        string platform,
        string? deviceModel = null,
        string? manufacturer = null)
    {
        // Try exact match first (same resolution, density, and platform)
        var exactMatch = _knownDevices.FirstOrDefault(d =>
            d.WidthPixels == widthPixels &&
            d.HeightPixels == heightPixels &&
            Math.Abs(d.Density - density) < 10 && // Allow 10 DPI tolerance
            d.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null) return exactMatch;
        
        // Try matching by device model/manufacturer
        if (!string.IsNullOrEmpty(deviceModel) || !string.IsNullOrEmpty(manufacturer))
        {
            var modelMatch = _knownDevices.FirstOrDefault(d =>
                (!string.IsNullOrEmpty(d.DeviceModel) && 
                 d.DeviceModel.Equals(deviceModel, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(d.Manufacturer) && 
                 d.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase)));
            
            if (modelMatch != null)
                return modelMatch;
        }
        
        // Find closest match by resolution and aspect ratio
        var targetAspectRatio = (double)widthPixels / heightPixels;
        
        var closestMatch = _knownDevices
            .Where(d => d.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase))
            .Select(d => new
            {
                Profile = d,
                AspectRatioDiff = Math.Abs((double)d.WidthPixels / d.HeightPixels - targetAspectRatio),
                ResolutionDiff = Math.Abs(d.WidthPixels - widthPixels) + Math.Abs(d.HeightPixels - heightPixels),
                DensityDiff = Math.Abs(d.Density - density)
            })
            .OrderBy(m => m.AspectRatioDiff)
            .ThenBy(m => m.ResolutionDiff)
            .ThenBy(m => m.DensityDiff)
            .FirstOrDefault();
        
        return closestMatch?.Profile;
    }
    
    /// <summary>
    /// Register a new device profile at runtime
    /// </summary>
    public static void RegisterDevice(DeviceProfile profile)
    {
        _knownDevices.Add(profile);
    }
}