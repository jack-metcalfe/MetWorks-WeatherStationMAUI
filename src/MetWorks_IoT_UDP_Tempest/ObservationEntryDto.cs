namespace MetWorks.IoT.UDP.Tempest;
internal record class ObservationEntryDto : ReadingDto, IObservationEntryDto
{
    /// Direct from device measurements
    /// <summary>
    /// Temperature measurement with unit (e.g., 72.5°F or 22.5°C).
    /// Unit determined by user preferences at transformation time.
    /// Direct from device in °C
    /// Index 7 in observation array
    /// </summary>
    public required double AirTemperature { get; init; }
    /// Direct from device in Volts
    /// Index 16 in observation array
    public required double BatteryLevel { get; init; }
    /// <summary>
    /// Epoch time of measurement (UTC)
    /// Direct from device in seconds since Jan 1, 1970.
    /// Index 0 in observation array
    /// </summary>
    public required long EpochTimestampUtc { get; init; }
    /// <summary>
    /// Relative humidity as a percentage (0-100).
    /// Direct from device in Lux
    /// Index 9 in observation array
    /// </summary>
    public required int Illuminance { get; init; }
    /// Direct from device
    /// Index 14 in observation array
    /// Direct from device in km
    public required int LightningStrikeAverageDistance { get; init; }
    /// Direct from device - count of strikes
    /// Index 15 in observation array
    public required int LightningStrikeCount { get; init; }
    /// Direct from device
    /// 0 = none, 1 = rain, 2 = hail, 3 = rain + hail (experimental)
    /// Index 13 in observation array
    public required int PrecipitationType { get; init; }
    /// Direct from device in mm over previous minute
    /// 0 = none, 1 = rain, 2 = hail, 3 = rain + hail (experimental)
    /// Index 12 in observation array
    public required double RainAccumulation { get; init; }
    /// Direct from device in percent
    /// Index 8 in observation array
    public required double RelativeHumidity { get; init; }
    /// Direct from device in minutes
    /// Index 17 in observation array
    public required int ReportingInterval { get; init; }
    /// <summary>
    /// Solar radiation
    /// Direct from device in W/m²
    /// Index 11 in observation array
    /// </summary>
    public required double SolarRadiation { get; init; }
    /// <summary>
    /// Station pressure (pressure at station elevation, not sea level adjusted).
    /// Direct from device in MB/millibars
    /// Index 6 in observation array
    /// </summary>
    public required double StationPressure { get; init; }
    /// <summary>
    /// UV index (0-11+ scale).
    /// Direct from device in UV index scale
    /// Index 10 in observation array
    /// </summary>
    public required double UvIndex { get; init; }
    /// Direct from device in average over report interval
    /// Index 2 in observation array
    public required double WindAverage { get; init; }
    /// Direct from device in degrees
    /// Index 4 in observation array
    public required int WindDirection { get; init; }
    /// Direct from device in highest over report interval, minimum 3 second sample
    /// Index 3 in observation array
    public required double WindGust { get; init; }
    /// Direct from device in lowest over report interval, minimum 3 second sample
    /// Index 4 in observation array
    public required double WindLull { get; init; }
    /// Direct from device in seconds
    /// Index 5 in observation array
    public required int WindSampleInterval { get; init; }
}