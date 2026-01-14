namespace Interfaces;
public interface IUdpMessageTypeDtoDictionary
{
    IUdpMessageTypeDto GetMessageType(PacketEnum udpPacketTypeKey);
}
