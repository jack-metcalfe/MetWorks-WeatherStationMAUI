namespace UdpPackets;
internal class PacketEnumToConcreteDto
{
    internal static Dictionary<PacketEnum, Func<ReadOnlyMemory<char>, PacketDtoBase>> PacketHandlers = new()
    {
        [PacketEnum.Lightning] = packet =>
        {
            var lightningPacket = PacketFactory.CreateTupleOfPacketEnumPacketDtoBaseFrom(packet).PacketDtoBase as LightningDto;
            return lightningPacket ?? throw new InvalidOperationException("Failed to cast to LightningDto.");
        },
        [PacketEnum.Observation] = packet =>
        {
            var observationPacket = PacketFactory.CreateTupleOfPacketEnumPacketDtoBaseFrom(packet).PacketDtoBase as ObservationDto;
            return observationPacket ?? throw new InvalidOperationException("Failed to cast to ObservationDto.");
        },
        [PacketEnum.Precipitation] = packet =>
        {
            var precipitationPacket = PacketFactory.CreateTupleOfPacketEnumPacketDtoBaseFrom(packet).PacketDtoBase as PrecipitationDto;
            return precipitationPacket ?? throw new InvalidOperationException("Failed to cast to PrecipitationDto.");
        },
        [PacketEnum.Wind] = packet =>
        {
            var windPacket = PacketFactory.CreateTupleOfPacketEnumPacketDtoBaseFrom(packet).PacketDtoBase as WindDto;
            return windPacket ?? throw new InvalidOperationException("Failed to cast to WindDto.");
        },
    };
}
