namespace MetWorks.Apps.MAUI.WeatherStationMaui.DeviceSelection;

using System;

using MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices;

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

        var viewTypeName = profile?.ViewTypeName;
        if (string.IsNullOrWhiteSpace(viewTypeName))
        {
            Debug.WriteLine($"⚠️ No matching profile found for {widthPixels}x{heightPixels} @ {density:F1} DPI on {platform}");
            viewTypeName = "MainView1920x1200";
        }
        else
        {
            Debug.WriteLine($"🎯 Matched device: {profile!.DeviceName} → {viewTypeName}");
        }

        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services is null)
            throw new InvalidOperationException("MAUI IServiceProvider was not available to create MainViewPage via DI.");

        var page = ActivatorUtilities.CreateInstance(services, typeof(Pages.MainDeviceViews.MainViewPage)) as Pages.MainDeviceViews.MainViewPage;
        if (page is null)
            throw new InvalidOperationException("Failed to create MainViewPage via DI.");

        var view = CreateViewFromTypeNameView(viewTypeName);
        page.SetContent(view);
        return page;
    }

    /// <summary>
    /// Get the appropriate ContentView (View) for the current device.
    /// Preferred for embedding inside other containers.
    /// </summary>
    public static View GetViewForCurrentDevice()
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

        if (profile != null)
        {
            Debug.WriteLine($"🎯 Matched device: {profile.DeviceName} → {profile.ViewTypeName}");
            return CreateViewFromTypeNameView(profile.ViewTypeName);
        }

        Debug.WriteLine($"⚠️ No matching profile found for {widthPixels}x{heightPixels} @ {density:F1} DPI on {platform}");
        return CreateViewFromTypeNameView("MainView1920x1200");
    }

    private static ContentPage CreateViewFromTypeName(string typeName)
    {
        Type? t = typeName switch
        {
            // Kept for backward compatibility if anything is still routing directly to a page.
            // Primary flow should use MainViewPage + ContentView layout selection.
            "MainView1920x1200" => typeof(Pages.MainDeviceViews.MainViewPage),
            _ => typeof(Pages.MainDeviceViews.MainViewPage)
        };

        // For route-based navigation, always return a MainViewPage hosting the selected view.
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services != null)
            {
                var page = ActivatorUtilities.CreateInstance(services, typeof(Pages.MainDeviceViews.MainViewPage)) as Pages.MainDeviceViews.MainViewPage;
                if (page != null)
                {
                    var view = CreateViewFromTypeNameView(typeName);
                    // attach the same resolved WeatherViewModel when available
                    var vm = services.GetService<WeatherViewModel>();
                    view.BindingContext = vm;
                    page.SetContent(view);
                    return page;
                }
            }
        }
        catch { }

        throw new InvalidOperationException("MAUI IServiceProvider was not available to create MainViewPage via DI.");
    }

    private static View CreateViewFromTypeNameView(string typeName)
    {
        if (!MainDeviceViewsCatalog.TryGetViewType(typeName, out var viewType))
        {
            viewType = null;
        }

        if (viewType != null)
        {
            try
            {
                var services = Application.Current?.Handler?.MauiContext?.Services;
                if (services is not null)
                {
                    // Prefer DI creation so constructor-injected dependencies (ViewModels, loggers) are satisfied
                    var view = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(services, viewType) as View;
                    if (view != null) return view;
                }
            }
            catch { }
        }

        // Fallback: try to create the full page and return its Content
        try
        {
            var page = CreateViewFromTypeName(typeName);
            if (page?.Content is View contentView)
                return contentView;
        }
        catch { }

        // final fallback
        return new ContentView
        {
            Content = new Label { Text = $"Missing view: {typeName}", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
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

        return MainDeviceViewsCatalog.DefaultViewTypeName;
    }
}
