namespace MetWorks.Apps.MAUI.WeatherStationMaui.DeviceSelection;

/// <summary>
/// Single source of truth for supported main device views.
/// Keeps view names and CLR types in one place to avoid string/type duplication across the app.
/// </summary>
public static class MainDeviceViewsCatalog
{
    public const string DefaultViewTypeName = nameof(MainView1920x1200);

    static readonly IReadOnlyDictionary<string, Type> NameToViewType = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [nameof(MainView1920x1200)] = typeof(MainView1920x1200),
        [nameof(MainView2304x1440)] = typeof(MainView2304x1440),
        [nameof(MainView1440x2304)] = typeof(MainView1440x2304),
        [nameof(MainView1812x2176)] = typeof(MainView1812x2176),
        [nameof(MainView2176x1812)] = typeof(MainView2176x1812),
    };

    public static IEnumerable<string> AllViewTypeNames => NameToViewType.Keys;

    public static IEnumerable<Type> AllViewTypes => NameToViewType.Values;

    public static bool TryGetViewType(string viewTypeName, out Type viewType)
    {
        if (string.IsNullOrWhiteSpace(viewTypeName))
        {
            viewType = default!;
            return false;
        }

        return NameToViewType.TryGetValue(viewTypeName, out viewType!);
    }
}
