namespace UdpPackets;
internal sealed class ObservationDto : PacketDtoBase, IObservationDto
{
    [JsonPropertyName("firmware_revision")] public int FirmwareRevision { get; init; }
    [JsonIgnore] public IObservationReadingDto[] Observations { get; init; }
    [JsonPropertyName("obs")] public JsonElement Measurements { get; init; }
    [JsonConstructor]
    public ObservationDto(string serial_number, string type, string hub_sn, int firmware_revision, JsonElement obs)
    {
        SerialNumber = serial_number;
        Type = type;
        HubSerialNumber = hub_sn;
        FirmwareRevision = firmware_revision;
        Measurements = obs;

        Observations = obs.EnumerateArray()
            .Select(inner => inner.EnumerateArray().ToArray())
            .Select(array => new ObservationReadingDto
            {
                EpochTimestampUtc = array[0].GetInt64(),
                WindLull = array[1].GetDouble(),
                WindAverage = array[2].GetDouble(),
                WindGust = array[3].GetDouble(),
                WindDirection = array[4].GetInt32(),
                WindSampleInterval = array[5].GetInt32(),
                StationPressure = array[6].GetDouble(),
                AirTemperature = array[7].GetDouble(),
                RelativeHumidity = array[8].GetDouble(),
                Illuminance = array[9].GetInt32(),
                UvIndex = array[10].GetDouble(),
                SolarRadiation = array[11].GetDouble(),
                RainAccumulation = array[12].GetDouble(),
                PrecipitationType = array[13].GetInt32(),
                LightningStrikeAvgDistance = array[14].GetInt32(),
                LightningStrikeCount = array[15].GetInt32(),
                BatteryVoltage = array[16].GetDouble(),
                ReportingInterval = array[17].GetInt32()
            })
            .ToArray();
    }
}
