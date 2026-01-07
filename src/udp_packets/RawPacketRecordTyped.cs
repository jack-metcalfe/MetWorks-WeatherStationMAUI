namespace UdpPackets;

internal sealed record RawPacketRecordTyped(
    Guid Id,                                    // COMB GUID
    long ReceivedUtcUnixEpochSecondsAsLong,    // UTC epoch timestamp
    string RawPacketJson,                       // Original JSON payload
    PacketEnum PacketEnum
) : IRawPacketRecordTyped
{
    /// <summary>
    /// Converts the Unix epoch timestamp to a DateTime for convenience.
    /// </summary>
    public DateTime ReceivedTime => DateTimeOffset.FromUnixTimeSeconds(ReceivedUtcUnixEpochSecondsAsLong).UtcDateTime;
}