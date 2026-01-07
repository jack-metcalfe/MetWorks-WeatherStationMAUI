namespace UdpPackets;
public interface ILightningDto : IPacketDtoBase
{
    long DeviceReceivedUtcTimestampEpoch { get; }
    double LightningStrikeDistanceKm { get; }
    double RelativeEnergyContent { get; }
    JsonElement MeasurementPayload { get; }
}
