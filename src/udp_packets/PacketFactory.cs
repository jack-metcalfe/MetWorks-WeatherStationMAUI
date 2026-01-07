using System.Reflection;
namespace UdpPackets;
internal class PacketFactory : IPacketFactory
{
    internal static (PacketEnum PacketEnum, IPacketDtoBase PacketDtoBase) CreateTupleOfPacketEnumPacketDtoBaseFrom(
        ReadOnlyMemory<char> udpPacketAsReadOnlyMemoryOfChar)
    {
        return (CreatePacketDtoBaseFrom(udpPacketAsReadOnlyMemoryOfChar.ToString()));
    }
    internal static (PacketEnum PacketEnum, IPacketDtoBase PacketDtoBase) CreateTupleOfPacketEnumPacketDtoBaseFrom(
        Span<char> udpPacketAsSpanOfChar)
    {
        return CreatePacketDtoBaseFrom(udpPacketAsSpanOfChar.ToString());
    }
    static (PacketEnum PacketEnum, PacketDtoBase PacketDtoBase) CreatePacketDtoBaseFrom(string udpPacketAsString)
    {
        var udpPacketAsJsonDocument = JsonDocument.Parse(udpPacketAsString);

        var packetEnumKeyAsString = udpPacketAsJsonDocument.RootElement.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString()
                        ?? throw new InvalidOperationException("'type' field is null.")
                    : throw new InvalidOperationException("Missing 'type' field in JSON document.");     

        var packetEnumKey = DictionaryOfPacketTypeStringToPacketEnumKey.Get(packetEnumKeyAsString);
        var exception = new InvalidOperationException($"Failed to deserialize to '{packetEnumKey}'.");

        return packetEnumKey switch
        {
            PacketEnum.Wind =>
                (packetEnumKey, (udpPacketAsJsonDocument.Deserialize<WindDto>() ?? throw exception)),
            PacketEnum.Lightning =>
                (packetEnumKey, (udpPacketAsJsonDocument.Deserialize<LightningDto>() ?? throw exception)),
            PacketEnum.Observation =>
                (packetEnumKey, (udpPacketAsJsonDocument.Deserialize<ObservationDto>() ?? throw exception)),
            PacketEnum.Precipitation =>
                (packetEnumKey, (udpPacketAsJsonDocument.Deserialize<PrecipitationDto>() ?? throw exception)),
            _ => throw new NotSupportedException($"Packet type '{packetEnumKey}' not supported.")
        };
    }
}