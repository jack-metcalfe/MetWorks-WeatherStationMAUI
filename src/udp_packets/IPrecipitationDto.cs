namespace MetWorks.IoT.UDP.Tempest;
public interface IPrecipitationDto
{
    int FirmwareRevision { get; init; }
    long DeviceReceivedUtcTimestampEpoch { get; init; }
    JsonElement Measurements { get; init; }
}