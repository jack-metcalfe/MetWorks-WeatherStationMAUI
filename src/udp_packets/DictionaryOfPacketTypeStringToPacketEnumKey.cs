namespace UdpPackets;
internal class DictionaryOfPacketTypeStringToPacketEnumKey
{
    internal const string LightningPacketKeyString = "evt_strke";
    internal const string ObservationPacketKeyString = "obs_st";
    internal const string PrecipitationPacketKeyString = "evt_precip";
    internal const string WindPacketKeyString = "rapid_wind";

    static readonly Dictionary<string, PacketEnum> _dictionaryOfPacketTypeStringToPacketEnum = new(StringComparer.OrdinalIgnoreCase)
    {
        [LightningPacketKeyString] = PacketEnum.Lightning,
        [ObservationPacketKeyString] = PacketEnum.Observation,
        [PrecipitationPacketKeyString] = PacketEnum.Precipitation,
        [WindPacketKeyString] = PacketEnum.Wind,
    };
    public static bool TryGet(string udpPacketTypeKeyString, out PacketEnum udpPacketTypeKey)
    {
        return _dictionaryOfPacketTypeStringToPacketEnum
            .TryGetValue(udpPacketTypeKeyString.Trim(), out udpPacketTypeKey);
    }
    public static PacketEnum Get(string udpPacketTypeKeyString)
    {
        return TryGet(udpPacketTypeKeyString, out var packetType)
            ? packetType
            : throw new NotSupportedException($"Unknown or unsupported packet type: '{udpPacketTypeKeyString}'");
    }
}