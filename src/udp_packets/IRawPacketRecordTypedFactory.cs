namespace UdpPackets;
public interface IRawPacketRecordTypedFactory
{
    static IRawPacketRecordTyped Create(ReadOnlyMemory<char> rawPacketJsonAsReadOnlyMemoryOfChar)
        => RawPacketRecordTypedFactory.Create(rawPacketJsonAsReadOnlyMemoryOfChar);
    static IRawPacketRecordTyped Create(Span<char> rawPacketJsonAsSpanOfChar)
        => RawPacketRecordTypedFactory.Create(rawPacketJsonAsSpanOfChar);
}
