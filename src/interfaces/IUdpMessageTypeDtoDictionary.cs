namespace InterfaceDefinition;
public interface IUdpMessageTypeDtoDictionary
{
    IUdpMessageTypeDto GetMessageType(PacketEnum udpPacketTypeKey);
}
