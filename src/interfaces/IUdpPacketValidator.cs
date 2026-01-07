namespace InterfaceDefinition;
public interface IUdpPacketValidator : IValidator<
        (PacketEnum UdpPacketTypeEnum, JsonDocument JsonDocument, IValidationResult IValidationResult), 
        ReadOnlyMemory<char>>
{
}
