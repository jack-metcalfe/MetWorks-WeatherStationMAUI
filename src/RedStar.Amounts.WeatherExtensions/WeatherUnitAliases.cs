namespace RedStar.Amounts.WeatherExtensions;

/// <summary>
/// Registers weather-specific unit aliases with RedStar.Amounts UnitManager.
/// Maps YAML configuration values (e.g., "mile/hour", "mph") to RedStar Unit instances.
/// Uses UnitResolve event - RedStar's designed extension point (no library modifications).
/// </summary>
public static class WeatherUnitAliases
{
    private static readonly Dictionary<string, Unit> _aliasMap = new Dictionary<string, Unit>(StringComparer.OrdinalIgnoreCase);
    private static bool _isRegistered = false;

    /// <summary>
    /// Registers weather unit aliases with RedStar.Amounts UnitManager.
    /// Call this ONCE at application startup, AFTER UnitManager.RegisterByAssembly().
    /// </summary>
    /// <remarks>
    /// Thread-safe: Multiple calls are safe; only first call takes effect.
    /// </remarks>
    public static void Register()
    {
        if (_isRegistered) return;

        // Build alias dictionary
        BuildAliasMap();

        // Hook into RedStar's UnitResolve event (their designed extension point)
        UnitManager.Instance.UnitResolve += OnUnitResolve;

        _isRegistered = true;
    }

    /// <summary>
    /// Unregisters the event handler. Call during shutdown if needed.
    /// </summary>
    public static void Unregister()
    {
        if (!_isRegistered) return;

        UnitManager.Instance.UnitResolve -= OnUnitResolve;
        _aliasMap.Clear();
        _isRegistered = false;
    }

    private static void BuildAliasMap()
    {
        // ========================================
        // Temperature (YAML: TemperatureOptions)
        // ========================================
        _aliasMap["degree celsius"] = TemperatureUnits.DegreeCelsius;
        _aliasMap["celsius"] = TemperatureUnits.DegreeCelsius;
        _aliasMap["degree fahrenheit"] = TemperatureUnits.DegreeFahrenheit;
        _aliasMap["fahrenheit"] = TemperatureUnits.DegreeFahrenheit;
        _aliasMap["kelvin"] = TemperatureUnits.Kelvin;

        // ========================================
        // Speed (YAML: WindspeedOptions)
        // ========================================
        _aliasMap["kilometer/hour"] = SpeedUnits.KilometerPerHour;
        _aliasMap["kph"] = SpeedUnits.KilometerPerHour;
        _aliasMap["km/h"] = SpeedUnits.KilometerPerHour;

        _aliasMap["knot"] = SpeedUnits.Knot;
        _aliasMap["knots"] = SpeedUnits.Knot;

        _aliasMap["meter/second"] = SpeedUnits.MeterPerSecond;
        _aliasMap["m/s"] = SpeedUnits.MeterPerSecond;

        _aliasMap["mile/hour"] = SpeedUnits.MilePerHour;
        _aliasMap["mph"] = SpeedUnits.MilePerHour;
        _aliasMap["mi/h"] = SpeedUnits.MilePerHour;

        // ========================================
        // Pressure (YAML: PressureOptions)
        // ========================================
        _aliasMap["atmosphere"] = PressureUnits.Atmosphere;
        _aliasMap["atm"] = PressureUnits.Atmosphere;

        _aliasMap["bar"] = PressureUnits.Bar;

        _aliasMap["hectopascal"] = PressureUnits.HectoPascal;
        _aliasMap["hpa"] = PressureUnits.HectoPascal;

        _aliasMap["inch of mercury"] = PressureUnits.InchOfMercury;
        _aliasMap["inhg"] = PressureUnits.InchOfMercury;

        _aliasMap["kilopascal"] = PressureUnits.KiloPascal;
        _aliasMap["kpa"] = PressureUnits.KiloPascal;

        _aliasMap["millibar"] = PressureUnits.MilliBar;
        _aliasMap["mbar"] = PressureUnits.MilliBar;

        _aliasMap["pascal"] = PressureUnits.Pascal;
        _aliasMap["pa"] = PressureUnits.Pascal;

        // ========================================
        // Distance (YAML: DistanceOptions)
        // ========================================
        _aliasMap["kilometer"] = LengthUnits.KiloMeter;
        _aliasMap["km"] = LengthUnits.KiloMeter;

        _aliasMap["meter"] = LengthUnits.Meter;
        _aliasMap["m"] = LengthUnits.Meter;
        _aliasMap["metre"] = LengthUnits.Meter;  // British spelling

        _aliasMap["mile"] = LengthUnits.Mile;
        _aliasMap["mi"] = LengthUnits.Mile;

        _aliasMap["nautical mile"] = LengthUnits.NauticalMile;
        _aliasMap["nmi"] = LengthUnits.NauticalMile;

        // ========================================
        // Precipitation (YAML: PrecipitationOptions)
        // ========================================
        _aliasMap["centimeter"] = LengthUnits.CentiMeter;
        _aliasMap["centimetre"] = LengthUnits.CentiMeter;  // British spelling
        _aliasMap["cm"] = LengthUnits.CentiMeter;

        _aliasMap["inch"] = LengthUnits.Inch;
        _aliasMap["in"] = LengthUnits.Inch;

        _aliasMap["millimeter"] = LengthUnits.MilliMeter;
        _aliasMap["millimetre"] = LengthUnits.MilliMeter;  // British spelling
        _aliasMap["mm"] = LengthUnits.MilliMeter;
    }

    /// <summary>
    /// Event handler for UnitManager.UnitResolve.
    /// Called by RedStar when GetUnitByName() fails to find a unit.
    /// Returns matching unit from alias map, or null if not found.
    /// </summary>
    private static Unit? OnUnitResolve(object sender, ResolveEventArgs args)
    {
        if (_aliasMap.TryGetValue(args.Name, out var unit))
        {
            return unit;
        }
        return null;
    }

    /// <summary>
    /// Gets all registered aliases for diagnostic/testing purposes.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if Register() has not been called.
    /// </exception>
    public static IReadOnlyDictionary<string, Unit> GetRegisteredAliases()
    {
        if (!_isRegistered)
        {
            throw new InvalidOperationException(
                "WeatherUnitAliases.Register() must be called before accessing aliases.");
        }
        return _aliasMap;
    }

    /// <summary>
    /// Checks if aliases are registered.
    /// </summary>
    public static bool IsRegistered => _isRegistered;
}