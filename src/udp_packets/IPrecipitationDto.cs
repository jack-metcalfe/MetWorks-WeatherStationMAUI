namespace UdpPackets;
public interface IPrecipitationDto
{
    int FirmwareRevision { get; init; }
    long DeviceReceivedUtcTimestampEpoch { get; init; }
    JsonElement Measurements { get; init; }
}