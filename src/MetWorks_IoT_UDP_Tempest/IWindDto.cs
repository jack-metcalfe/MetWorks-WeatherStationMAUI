namespace MetWorks.IoT.UDP.Tempest;
public interface IWindDto : IPacketDtoBase
{
    long DeviceReceivedUtcTimestampEpoch { get; }
    double WindSpeed { get; }
    int WindDirection { get; }
}