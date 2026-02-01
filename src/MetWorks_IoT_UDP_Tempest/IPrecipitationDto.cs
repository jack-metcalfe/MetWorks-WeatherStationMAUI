namespace MetWorks.IoT.UDP.Tempest;
public interface IPrecipitationDto : IPacketDtoBase
{
    int FirmwareRevision { get; init; }
    long DeviceReceivedUtcTimestampEpoch { get; init; }
    JsonElement Measurements { get; init; }
}