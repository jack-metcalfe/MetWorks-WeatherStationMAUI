using RedStar.Amounts;

namespace MetWorksModels.Weather;

/// <summary>
/// Atmospheric pressure reading with unit support.
/// </summary>
public interface IPressureReading : IWeatherReading
{
    /// <summary>
    /// Atmospheric pressure value with unit (e.g., 29.92 inHg or 1013 mb).
    /// </summary>
    Amount Pressure { get; }
}
