namespace UdpPackets;
internal class RawPacketRecordFactory
{
    internal static RawPacketRecord Create(ReadOnlyMemory<char> rawPacketJsonAsReadOnlyMemoryOfChar)
    {
        return new RawPacketRecord(
            IdGenerator.CreateCombGuid(),
            DateTime.UtcNow.ToUnixEpochSeconds(),
            rawPacketJsonAsReadOnlyMemoryOfChar.ToString()
        );
    }
    internal static RawPacketRecord Create(Span<char> rawPacketJsonAsSpanOfChar)
    {
        return new RawPacketRecord(
            IdGenerator.CreateCombGuid(),
            DateTime.UtcNow.ToUnixEpochSeconds(),
            rawPacketJsonAsSpanOfChar.ToString()
        );
    }
}