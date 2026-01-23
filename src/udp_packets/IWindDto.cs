namespace MetWorks.IoT.UDP.Tempest;
public interface IWindDto
{
    long DeviceReceivedUtcTimestampEpoch { get; }
    double WindSpeed { get; }
    int WindDirection { get; }
}