namespace MetWorks.IoT.UDP.Tempest;
public interface ILightningDto : IPacketDtoBase
{
    long DeviceReceivedUtcTimestampEpoch { get; }
    double LightningStrikeDistanceKm { get; }
    double RelativeEnergyContent { get; }
    JsonElement MeasurementPayload { get; }
}
