namespace MetWorks.IoT.UDP.Tempest;
internal class PacketDtoBase : IPacketDtoBase
{
    [JsonPropertyName("hub_sn")] public required string HubSerialNumber { get; init; }
    [JsonPropertyName("serial_number")] public required string SerialNumber { get; init; }
    [JsonPropertyName("type")] public required string Type { get; init; }
}