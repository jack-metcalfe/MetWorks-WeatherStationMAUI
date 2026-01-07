namespace UdpPackets;
internal sealed class PrecipitationDto : PacketDtoBase, IPrecipitationDto
{
    [JsonPropertyName("firmware_revision")] public int FirmwareRevision { get; init; }
    [JsonIgnore] public long DeviceReceivedUtcTimestampEpoch { get; init; }
    [JsonPropertyName("evt")] public JsonElement Measurements { get; init; }
    [JsonConstructor]
    public PrecipitationDto(string serial_number, string type, string hub_sn, JsonElement evt)
    {
        SerialNumber = serial_number;
        Type = type;
        HubSerialNumber = hub_sn;
        Measurements = evt;

        DeviceReceivedUtcTimestampEpoch = evt[0].GetInt64();
    }
}
