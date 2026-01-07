namespace UdpPackets;
public interface IPacketDtoBase
{
    string SerialNumber { get; }
    string Type { get; }
    string HubSerialNumber { get; }
}
