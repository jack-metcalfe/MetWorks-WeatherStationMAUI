namespace MetWorks.IoT.UDP.Tempest;
public interface IPacketDtoBase
{
    string SerialNumber { get; }
    string Type { get; }
    string HubSerialNumber { get; }
}
