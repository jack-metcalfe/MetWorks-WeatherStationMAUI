namespace MetWorks.IoT.UDP.Tempest;

public interface IObservationEntryDto : IPacketDtoBase
{
    /// Direct from device measurements
    /// <summary>
    /// Temperature measurement with unit (e.g., 72.5°F or 22.5°C).
    /// Unit determined by user preferences at transformation time.
    /// Direct from device in °C
    /// Index 7 in observation array
    /// </summary>
    double AirTemperature { get; }
    /// Direct from device in Volts
    /// Index 16 in observation array
    double BatteryLevel { get; }
    /// <summary>
    /// Epoch time of measurement (UTC)
    /// Direct from device in seconds since Jan 1, 1970.
    /// Index 0 in observation array
    /// </summary>
    long EpochTimestampUtc { get; }
    /// <summary>
    /// Relative humidity as a percentage (0-100).
    /// Direct from device in Lux
    /// Index 9 in observation array
    /// </summary>
    int Illuminance { get; }
    /// Direct from device
    /// Index 14 in observation array
    /// Direct from device in km
    int LightningStrikeAverageDistance { get; }
    /// Direct from device - count of strikes
    /// Index 15 in observation array
    int LightningStrikeCount { get; }
    /// Direct from device
    /// 0 = none, 1 = rain, 2 = hail, 3 = rain + hail (experimental)
    /// Index 13 in observation array
    int PrecipitationType { get; }
    /// Direct from device in mm over previous minute
    /// 0 = none, 1 = rain, 2 = hail, 3 = rain + hail (experimental)
    /// Index 12 in observation array
    double RainAccumulation { get; }
    /// Direct from device in percent
    /// Index 8 in observation array
    double RelativeHumidity { get; }
    /// Direct from device in minutes
    /// Index 17 in observation array
    int ReportingInterval { get; }
    /// <summary>
    /// Solar radiation
    /// Direct from device in W/m²
    /// Index 11 in observation array
    /// </summary>
    double SolarRadiation { get; }
    /// <summary>
    /// Station pressure (pressure at station elevation, not sea level adjusted).
    /// Direct from device in MB/millibars
    /// Index 6 in observation array
    /// </summary>
    double StationPressure { get; }
    /// <summary>
    /// UV index (0-11+ scale).
    /// Direct from device in UV index scale
    /// Index 10 in observation array
    /// </summary>
    double UvIndex { get; }
    /// Direct from device in average over report interval
    /// Index 2 in observation array
    double WindAverage { get; }
    /// Direct from device in degrees
    /// Index 4 in observation array
    int WindDirection { get; }
    /// Direct from device in highest over report interval, minimum 3 second sample
    /// Index 3 in observation array
    double WindGust { get; }
    /// Direct from device in lowest over report interval, minimum 3 second sample
    /// Index 4 in observation array
    double WindLull { get; }
    /// Direct from device in seconds
    /// Index 5 in observation array
    int WindSampleInterval { get; }
}