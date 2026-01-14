namespace Interfaces;
public interface IUdpPacketValidateFactory 
    : IFactory<IUdpPacketValidator, Dictionary<PacketEnum, string>>
{
}