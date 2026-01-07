namespace UdpPackets;
public interface IRawPacketRecord
{
    Guid Id { get; }
    long ReceivedUtcUnixEpochSecondsAsLong { get; }
    string RawJson { get; }
}
