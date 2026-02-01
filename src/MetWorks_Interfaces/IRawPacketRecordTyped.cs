namespace MetWorks.Interfaces;
public interface IRawPacketRecordTyped
{
    Guid Id { get; }
    PacketEnum PacketEnum { get; }
    string RawPacketJson { get; }
    long ReceivedUtcUnixEpochSecondsAsLong { get; }
    DateTime ReceivedTime { get; }
}
