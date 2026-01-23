namespace MetWorks.IoT.UDP.Tempest;
public interface IObservationDto
{
    int FirmwareRevision { get; }
    JsonElement Measurements { get; }
    IObservationReadingDto[] Observations { get; }
}
