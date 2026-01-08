namespace MetWorksModels.Weather;

/// <summary>
/// Relative humidity reading (percentage).
/// </summary>
public interface IHumidityReading : IWeatherReading
{
    /// <summary>
    /// Relative humidity as a percentage (0-100).
    /// </summary>
    double RelativeHumidity { get; }
}
