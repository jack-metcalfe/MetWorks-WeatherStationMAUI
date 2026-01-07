namespace UdpPackets;
internal sealed record RawPacketRecord(
    Guid Id,                       // Comb GUID
    long ReceivedUtcUnixEpochSecondsAsLong,         // UTC epoch timestamp
    string RawJson   // Original JSON payload
) : IRawPacketRecord;
