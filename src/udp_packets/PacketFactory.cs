namespace UdpPackets;

internal class PacketFactory : IPacketFactory
{
    // Shared options for all deserializations
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = false, // We're using [JsonPropertyName] explicitly
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        IncludeFields = false
    };

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

        try
        {
            return packetEnumKey switch
            {
                PacketEnum.Wind =>
                    (packetEnumKey, (udpPacketAsJsonDocument.Deserialize<WindDto>(_jsonOptions) ?? throw exception)),
                PacketEnum.Lightning =>
                    (packetEnumKey, (udpPacketAsJsonDocument.Deserialize<LightningDto>(_jsonOptions) ?? throw exception)),
                PacketEnum.Observation =>
                    (packetEnumKey, (udpPacketAsJsonDocument.Deserialize<ObservationDto>(_jsonOptions) ?? throw exception)),
                PacketEnum.Precipitation =>
                    (packetEnumKey, (udpPacketAsJsonDocument.Deserialize<PrecipitationDto>(_jsonOptions) ?? throw exception)),
                _ => throw new NotSupportedException($"Packet type '{packetEnumKey}' not supported.")
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error deserializing packet of type '{packetEnumKey}': {ex.Message}", ex);
        }
    }
}