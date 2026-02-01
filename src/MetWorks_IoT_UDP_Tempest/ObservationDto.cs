namespace MetWorks.IoT.UDP.Tempest;
internal sealed class ObservationDto : PacketDtoBase, IObservationDto
{
    [JsonPropertyName("firmware_revision")] public required int FirmwareRevision { get; init; }
    [JsonPropertyName("obs")] public required JsonElement Measurements { get; init; }
    
    [JsonIgnore] 
    public IObservationEntryDto[] Observations => 
        Measurements.EnumerateArray()
            .Select(inner => inner.EnumerateArray().ToArray())
            .Select(
                array => new ObservationEntryDto {
                    HubSerialNumber = HubSerialNumber,
                    SerialNumber = SerialNumber,
                    Type = Type,

                    AirTemperature = array[7].GetDouble(),
                    BatteryLevel = array[16].GetDouble(),
                    EpochTimestampUtc = array[0].GetInt64(),
                    Illuminance = array[9].GetInt32(),
                    LightningStrikeAverageDistance = array[14].GetInt32(),
                    LightningStrikeCount = array[15].GetInt32(),
                    PrecipitationType = array[13].GetInt32(),
                    RainAccumulation = array[12].GetDouble(),
                    RelativeHumidity = array[8].GetDouble(),
                    ReportingInterval = array[17].GetInt32(),
                    SolarRadiation = array[11].GetDouble(),
                    StationPressure = array[6].GetDouble(),
                    UvIndex = array[10].GetDouble(),
                    WindAverage = array[2].GetDouble(),
                    WindDirection = array[4].GetInt32(),
                    WindGust = array[3].GetDouble(),
                    WindLull = array[1].GetDouble(),
                    WindSampleInterval = array[5].GetInt32(),
                }
             )
            .ToArray();
}
