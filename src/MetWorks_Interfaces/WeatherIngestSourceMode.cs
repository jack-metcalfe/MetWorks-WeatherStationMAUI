namespace MetWorks.Interfaces;

/// <summary>
/// Source selection mode for the UDP/REST mux.
/// </summary>
public enum WeatherIngestSourceMode
{
    Auto = 0,
    UdpOnly = 1,
    RestOnly = 2
}
