namespace UdpPackets;
public interface IWindDto
{
    long DeviceReceivedUtcTimestampEpoch { get; }
    double WindSpeed { get; }
    int WindDirection { get; }
}