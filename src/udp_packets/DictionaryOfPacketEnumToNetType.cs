namespace UdpPackets;
internal class DictionaryOfPacketEnumToNetType : IReadOnlyDictionarySimple<PacketEnum, Type>
{
    private static readonly DictionaryOfPacketEnumToNetType _instance = new DictionaryOfPacketEnumToNetType();
    private DictionaryOfPacketEnumToNetType() { }
    public static DictionaryOfPacketEnumToNetType Instance => _instance;
    static readonly Dictionary<PacketEnum, Type> _dictionaryOfPacketEnumToNetType = new()
    {
        [PacketEnum.Lightning] = typeof(LightningDto),
        [PacketEnum.Observation] = typeof(ObservationDto),
        [PacketEnum.Precipitation] = typeof(PrecipitationDto),
        [PacketEnum.Wind] = typeof(WindDto),
    };
    public bool TryGetValue(PacketEnum packetEnumKey, out Type netType)
    {
        return _dictionaryOfPacketEnumToNetType
            .TryGetValue(packetEnumKey, out netType);
    }
    public static bool TryGetValueSingleton(PacketEnum packetEnumKey, out Type netType)
    {
        return Instance.TryGetValue(packetEnumKey, out netType);
    }
    public Type GetValue(PacketEnum packetEnumKey)
    {
        return TryGetValue(packetEnumKey, out var netType)
            ? netType
            : throw new NotSupportedException($"Unknown or unsupported packet enum: '{packetEnumKey}'");
    }
    public static Type GetValueSingleton(PacketEnum packetEnumKey)
    {
        return Instance.GetValue(packetEnumKey);
    }
}