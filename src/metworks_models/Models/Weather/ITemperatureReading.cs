using RedStar.Amounts;

namespace MetWorksModels.Weather;

/// <summary>
/// Temperature reading with unit support.
/// </summary>
public interface ITemperatureReading : IWeatherReading
{
    /// <summary>
    /// Temperature value with unit (e.g., 72°F or 22°C).
    /// </summary>
    Amount Temperature { get; }
}
