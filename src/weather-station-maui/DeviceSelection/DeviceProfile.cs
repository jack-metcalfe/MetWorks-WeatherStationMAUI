namespace MetWorksWeather.DeviceSelection;

/// <summary>
/// Represents a specific device profile with detailed display characteristics.
/// Used to match physical devices to appropriate XAML views.
/// </summary>
public record DeviceProfile
{
    /// <summary>
    /// Friendly name for the device (e.g., "Surface Pro 9", "Pixel 7")
    /// </summary>
    public required string DeviceName { get; init; }
    
    /// <summary>
    /// Physical width in pixels (native resolution)
    /// </summary>
    public required int WidthPixels { get; init; }
    
    /// <summary>
    /// Physical height in pixels (native resolution)
    /// </summary>
    public required int HeightPixels { get; init; }
    
    /// <summary>
    /// Pixels per inch (density)
    /// </summary>
    public required double Density { get; init; }
    
    /// <summary>
    /// Platform (Android, Windows, iOS)
    /// </summary>
    public required string Platform { get; init; }
    
    /// <summary>
    /// Optional: Specific device model identifier
    /// </summary>
    public string? DeviceModel { get; init; }
    
    /// <summary>
    /// Optional: Manufacturer (Samsung, Microsoft, Google)
    /// </summary>
    public string? Manufacturer { get; init; }
    
    /// <summary>
    /// View type name to load for this device
    /// </summary>
    public required string ViewTypeName { get; init; }
    
    /// <summary>
    /// Optional: Override orientation (Portrait, Landscape, null for auto)
    /// </summary>
    public string? PreferredOrientation { get; init; }
    
    /// <summary>
    /// Calculated diagonal size in inches
    /// </summary>
    public double DiagonalInches => 
        Math.Sqrt(Math.Pow(WidthPixels / Density, 2) + Math.Pow(HeightPixels / Density, 2));
    
    /// <summary>
    /// Logical width (density-independent)
    /// </summary>
    public double LogicalWidth => WidthPixels / Density;
    
    /// <summary>
    /// Logical height (density-independent)
    /// </summary>
    public double LogicalHeight => HeightPixels / Density;
}