namespace UdpPackets;

/// <summary>
/// Public API for parsing Tempest weather station UDP packets.
/// Keeps internal DTOs encapsulated while providing access to parsed data.
/// </summary>
public static class TempestPacketParser
{
    /// <summary>
    /// Parses an observation packet and returns the first reading.
    /// Returns null if parsing fails or packet contains no observations.
    /// </summary>
    public static IObservationReadingDto? ParseObservation(IRawPacketRecordTyped rawPacket)
    {
        try
        {
            var dto = PacketEnumToConcreteDto.PacketHandlers[PacketEnum.Observation](
                rawPacket.RawPacketJson.AsMemory()
            ) as ObservationDto;
            
            return dto?.Observations.Length > 0 ? dto.Observations[0] : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a rapid wind packet.
    /// Returns null if parsing fails.
    /// </summary>
    public static IWindDto? ParseWind(IRawPacketRecordTyped rawPacket)
    {
        try
        {
            return PacketEnumToConcreteDto.PacketHandlers[PacketEnum.Wind](
                rawPacket.RawPacketJson.AsMemory()) as IWindDto;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a precipitation event packet.
    /// Returns null if parsing fails.
    /// </summary>
    public static IPrecipitationDto? ParsePrecipitation(IRawPacketRecordTyped rawPacket)
    {
        try
        {
            return PacketEnumToConcreteDto.PacketHandlers[PacketEnum.Precipitation](
                rawPacket.RawPacketJson.AsMemory()) as IPrecipitationDto;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a lightning strike event packet.
    /// Returns null if parsing fails.
    /// </summary>
    public static ILightningDto? ParseLightning(IRawPacketRecordTyped rawPacket)
    {
        try
        {
            return PacketEnumToConcreteDto.PacketHandlers[PacketEnum.Lightning](
                rawPacket.RawPacketJson.AsMemory()) as ILightningDto;
        }
        catch
        {
            return null;
        }
    }
}