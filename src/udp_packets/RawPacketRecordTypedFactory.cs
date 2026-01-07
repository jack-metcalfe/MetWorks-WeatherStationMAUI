using System.Diagnostics;

namespace UdpPackets;
internal static class RawPacketRecordTypedFactory
{
    internal static IRawPacketRecordTyped Create(ReadOnlyMemory<char> rawPacketJsonAsReadOnlyMemoryOfChar)
    {
        var rawPacketJsonAsString = rawPacketJsonAsReadOnlyMemoryOfChar.ToString();
        return new RawPacketRecordTyped(
            IdGenerator.CreateCombGuid(),
            DateTime.UtcNow.ToUnixEpochSeconds(),
            rawPacketJsonAsString,
            ExtractPacketEnumKey(rawPacketJsonAsString)
        );
    }
    internal static IRawPacketRecordTyped Create(Span<char> rawPacketJsonAsSpanOfChar)
    {
        var rawPacketJsonAsString = rawPacketJsonAsSpanOfChar.ToString();
        return new RawPacketRecordTyped(
            IdGenerator.CreateCombGuid(),
            DateTime.UtcNow.ToUnixEpochSeconds(),
            rawPacketJsonAsString,
            ExtractPacketEnumKey(rawPacketJsonAsString)
        );
    }
    static PacketEnum ExtractPacketEnumKey(string udpPacketAsString)
    {
        try
        {
            var udpPacketAsJsonDocument = JsonDocument.Parse(udpPacketAsString);
            var packetEnumKeyAsString = udpPacketAsJsonDocument.RootElement.TryGetProperty("type", out var typeProp)
                        ? typeProp.GetString()
                            ?? throw new InvalidOperationException("'type' field is null.")
                        : throw new InvalidOperationException("Missing 'type' field in JSON document.");

            var isSupportedType = DictionaryOfPacketTypeStringToPacketEnumKey.TryGet(
                packetEnumKeyAsString, out var packetEnumKey);
            return isSupportedType ? packetEnumKey : PacketEnum.NotImplemented;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to extract PacketEnum from UDP packet JSON.", ex);
        }
    }
}