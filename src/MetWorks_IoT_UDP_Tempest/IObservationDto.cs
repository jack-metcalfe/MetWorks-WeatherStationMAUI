namespace MetWorks.IoT.UDP.Tempest;
public interface IObservationDto  : IPacketDtoBase
{
    int FirmwareRevision { get; }
    JsonElement Measurements { get; }
    IObservationEntryDto[] Observations { get; }
}
