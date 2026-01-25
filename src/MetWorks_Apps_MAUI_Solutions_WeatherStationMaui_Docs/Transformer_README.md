# Transformer (UDP listener)

Purpose
- Listens for UDP packets, parses them into typed records and forwards to `IEventRelayBasic`.
- Handles socket bind failures and network interface changes and can recover automatically.

Key settings
- `UdpListener_preferredPort` — preferred UDP port to bind.
- `UdpListener_enableOutputBuffering` — enable bounded in-memory buffering of outbound messages.

Behavior summary
- Binds a `UdpClient` and runs a receive loop using `ReceiveAsync`.
- Subscribes to `NetworkChange` events and runs periodic rebind attempts when the socket or network goes away.
- Uses a `SemaphoreSlim` to safely replace/dispose the `UdpClient` without races.
- Optionally buffers outbound messages in a bounded in-memory queue when forwarding to `IEventRelayBasic` fails; buffered messages are flushed automatically after recovery.

Operational notes
- In-memory buffer is transient — consider SQLite if durability is required for long outages.
- Tune constants: retry attempts, retry delay, rebind interval, and buffer size to match deployment.
- Test by toggling network interfaces and simulating port-in-use scenarios.

Files & locations
- Implementation: `src/udp_in_raw_packet_record_typed_out/Transformer.cs`