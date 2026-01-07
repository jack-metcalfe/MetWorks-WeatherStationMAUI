namespace UdpPackets;
public interface IObservationReadingDto
{
    long EpochTimestampUtc { get; }
    double WindLull { get; }
    double WindAverage { get; }
    double WindGust { get; }
    int WindDirection { get; }
    int WindSampleInterval { get; }
    double StationPressure { get; }
    double AirTemperature { get; }
    double RelativeHumidity { get; }
    int Illuminance { get; }
    double UvIndex { get; }
    double SolarRadiation { get; }
    double RainAccumulation { get; }
    int PrecipitationType { get; }
    int LightningStrikeAvgDistance { get; }
    int LightningStrikeCount { get; }
    double BatteryVoltage { get; }
    int ReportingInterval { get; }
}