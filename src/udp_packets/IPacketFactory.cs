namespace UdpPackets;
public interface IPacketFactory
{
    static (PacketEnum PacketEnum, IPacketDtoBase PacketDtoBase) CreateTupleOfPacketEnumPacketDtoBaseFrom(
        ReadOnlyMemory<char> udpPacketAsReadOnlyMemoryOfChar)
            => PacketFactory.CreateTupleOfPacketEnumPacketDtoBaseFrom(udpPacketAsReadOnlyMemoryOfChar);

    static (PacketEnum PacketEnum, IPacketDtoBase PacketDtoBase) CreateTupleOfPacketEnumPacketDtoBaseFrom(
        Span<char> udpPacketAsSpanOfChar)
            => PacketFactory.CreateTupleOfPacketEnumPacketDtoBaseFrom(udpPacketAsSpanOfChar);
}
