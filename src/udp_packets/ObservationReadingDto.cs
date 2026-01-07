namespace UdpPackets;
internal sealed class ObservationReadingDto : IObservationReadingDto
{
    public long EpochTimestampUtc { get; init; }
    public double WindLull { get; init; }
    public double WindAverage { get; init; }
    public double WindGust { get; init; }
    public int WindDirection { get; init; }
    public int WindSampleInterval { get; init; }
    public double StationPressure { get; init; }
    public double AirTemperature { get; init; }
    public double RelativeHumidity { get; init; }
    public int Illuminance { get; init; }
    public double UvIndex { get; init; }
    public double SolarRadiation { get; init; }
    public double RainAccumulation { get; init; }
    public int PrecipitationType { get; init; }
    public int LightningStrikeAvgDistance { get; init; }
    public int LightningStrikeCount { get; init; }
    public double BatteryVoltage { get; init; }
    public int ReportingInterval { get; init; }
}
