namespace MetWorks.Interfaces;

/// <summary>
/// Identifies the active data source used for UI-facing weather readings.
/// </summary>
public enum WeatherIngestSource
{
    None = 0,
    Udp = 1,
    Rest = 2
}
