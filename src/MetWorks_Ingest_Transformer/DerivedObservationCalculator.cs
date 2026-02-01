namespace MetWorks.Ingest.Transformer;
internal static class DerivedObservationCalculator
{
    // Atmospheric pressure: convert station pressure (at elevation) to sea-level pressure.
    // Uses a common barometric formula approximation.
    public static Amount? TryComputeSeaLevelPressure(
        Amount? stationPressure, 
        Amount? airTemperature, 
        double? stationElevationMeters
    )
    {
        if (stationPressure is null || airTemperature is null || stationElevationMeters is null) return null;

        // Convert inputs to required units.
        var pStationMbar = stationPressure.ConvertedTo(PressureUnits.MilliBar).Value;
        var tC = airTemperature.ConvertedTo(TemperatureUnits.DegreeCelsius).Value;
        var h = stationElevationMeters.Value;

        if (pStationMbar <= 0) return null;

        // https://en.wikipedia.org/wiki/Barometric_formula
        // P0 = P * (1 - (L*h)/(T+L*h+273.15))^(-g*M/(R*L))
        // Using standard constants for troposphere.
        const double L = 0.0065; // K/m
        const double gM_over_RL = 5.257; // approx exponent

        var tK = tC + 273.15;
        var denom = tK + (L * h);
        if (denom <= 0) return null;

        var ratio = 1.0 - (L * h) / denom;
        if (ratio <= 0) return null;

        var p0 = pStationMbar * Math.Pow(ratio, -gM_over_RL);
        return new Amount(p0, PressureUnits.MilliBar);
    }
    public static Amount? TryComputeWindChill(Amount? airTemperature, Amount? windSpeed)
    {
        if (airTemperature is null || windSpeed is null) return null;

        // NOAA wind chill formula valid for:
        // - T <= 50 F
        // - v >= 3 mph
        var tF = airTemperature.ConvertedTo(TemperatureUnits.DegreeFahrenheit).Value;
        var vMph = windSpeed.ConvertedTo(SpeedUnits.MilePerHour).Value;

        if (tF > 50) return null;
        if (vMph < 3) return null;

        var wcF = 35.74 + 0.6215 * tF - 35.75 * Math.Pow(vMph, 0.16) + 0.4275 * tF * Math.Pow(vMph, 0.16);
        return new Amount(wcF, TemperatureUnits.DegreeFahrenheit);
    }
    public static Amount? TryComputeHeatIndex(Amount? airTemperature, double? relativeHumidityPercent)
    {
        if (airTemperature is null || relativeHumidityPercent is null) return null;

        // NOAA heat index regression. Valid for T >= 80 F and RH >= 40%.
        var tF = airTemperature.ConvertedTo(TemperatureUnits.DegreeFahrenheit).Value;
        var rh = relativeHumidityPercent.Value;

        if (tF < 80) return null;
        if (rh < 40) return null;

        var hiF =
            -42.379 +
            2.04901523 * tF +
            10.14333127 * rh +
            -0.22475541 * tF * rh +
            -0.00683783 * tF * tF +
            -0.05481717 * rh * rh +
            0.00122874 * tF * tF * rh +
            0.00085282 * tF * rh * rh +
            -0.00000199 * tF * tF * rh * rh;

        return new Amount(hiF, TemperatureUnits.DegreeFahrenheit);
    }
    public static Amount? ComputeFeelsLike(Amount airTemperature, Amount? windChill, Amount? heatIndex)
    {
        // Common convention: if heat index is present, prefer it; else if wind chill is present, prefer it; else ambient.
        if (heatIndex is not null) return heatIndex;
        if (windChill is not null) return windChill;
        return airTemperature;
    }
    public static Amount? TryComputeDewPoint(Amount? airTemperature, double? relativeHumidityPercent)
    {
        if (airTemperature is null || relativeHumidityPercent is null) return null;

        // Magnus formula.
        var tC = airTemperature.ConvertedTo(TemperatureUnits.DegreeCelsius).Value;
        var rh = relativeHumidityPercent.Value;
        if (rh <= 0 || rh > 100) return null;

        const double a = 17.625;
        const double b = 243.04; // Celsius

        var gamma = (a * tC) / (b + tC) + Math.Log(rh / 100.0);
        var dpC = (b * gamma) / (a - gamma);

        return new Amount(dpC, TemperatureUnits.DegreeCelsius);
    }
}
