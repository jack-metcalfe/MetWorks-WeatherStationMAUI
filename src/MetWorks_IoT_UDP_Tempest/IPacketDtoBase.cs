namespace MetWorks.IoT.UDP.Tempest;
public interface IPacketDtoBase
{
    string HubSerialNumber { get; }
    string SerialNumber { get; }
    string Type { get; }
}
