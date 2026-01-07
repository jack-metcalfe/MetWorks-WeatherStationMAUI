namespace UdpPackets;
public interface IRawPacketRecordTyped
{
    Guid Id { get; }
    long ReceivedUtcUnixEpochSecondsAsLong { get; }
    string RawPacketJson { get; }
    DateTime ReceivedTime { get; }
    PacketEnum PacketEnum { get; }
}
