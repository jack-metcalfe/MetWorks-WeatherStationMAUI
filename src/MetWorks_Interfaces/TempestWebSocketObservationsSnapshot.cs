namespace MetWorks.Interfaces;

/// <summary>
/// Raw observation/event snapshot received from the Tempest hosted WebSocket stream.
/// This message is intended for publication via <see cref="IEventRelayBasic"/> and preserves the full JSON payload.
/// </summary>
public sealed record TempestWebSocketObservationsSnapshot(
    long DeviceId,
    DateTimeOffset ReceivedUtc,
    string MessageType,
    string RawJson
);
