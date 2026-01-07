namespace UdpPackets;
internal sealed class WindDto : PacketDtoBase, IWindDto
{
    [JsonPropertyName("ob")] public required JsonElement Measurements { get; init; }

    [JsonIgnore] public long DeviceReceivedUtcTimestampEpoch => Measurements[0].GetInt64();
    [JsonIgnore] public double WindSpeed => Measurements[1].GetDouble();
    [JsonIgnore] public int WindDirection => Measurements[2].GetInt32();
}
