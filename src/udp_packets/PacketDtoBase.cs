namespace UdpPackets;
internal class PacketDtoBase : IPacketDtoBase
{
    [JsonPropertyName("serial_number")] public required string SerialNumber { get; init; }
    [JsonPropertyName("type")] public required string Type { get; init; }
    [JsonPropertyName("hub_sn")] public required string HubSerialNumber { get; init; }
}
