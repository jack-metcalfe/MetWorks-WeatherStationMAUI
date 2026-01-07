namespace UdpPackets;
internal sealed class LightningDto : PacketDtoBase, ILightningDto
{
    [JsonIgnore] public long DeviceReceivedUtcTimestampEpoch { get; init; }
    [JsonIgnore] public double LightningStrikeDistanceKm { get; init; }
    [JsonIgnore] public double RelativeEnergyContent { get; init; }
    [JsonPropertyName("evt")] public JsonElement MeasurementPayload { get; init; }
    [JsonConstructor]
    public LightningDto(string serial_number, string type, string hub_sn, JsonElement evt)
    {
        SerialNumber = serial_number;
        Type = type;
        HubSerialNumber = hub_sn;
        MeasurementPayload = evt;

        var array = evt.EnumerateArray().ToArray();
        DeviceReceivedUtcTimestampEpoch = array[0].GetInt64();
        LightningStrikeDistanceKm = array[1].GetDouble();
        RelativeEnergyContent = array[2].GetDouble();
    }
}
