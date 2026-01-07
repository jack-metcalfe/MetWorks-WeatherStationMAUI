namespace InterfaceDefinition;
public interface IUdpPacketValidateFactory 
    : IFactory<IUdpPacketValidator, Dictionary<PacketEnum, string>>
{
}