namespace MetWorks.IoT.UDP.Tempest;

// ============================================================================
// RawPacketRecordTypedFactory
// ----------------------------------------------------------------------------
// Builds IRawPacketRecordTyped instances from raw Tempest UDP JSON payloads
// that arrive on the LAN from a Tempest weather station hub.
//
// Each produced record captures:
//   * A COMB GUID identity (sortable-by-time GUID) for dedup and ordering.
//   * The application-received UTC timestamp (Unix epoch seconds).
//   * The original JSON text (preserved verbatim for lossless persistence
//     and downstream shipping).
//   * The decoded PacketEnum that classifies the payload (obs_st, rapid_wind,
//     evt_strike, hub_status, etc.) so consumers can route without re-parsing.
//
// This class is intentionally `internal static`: it is a pure stateless
// construction helper owned by the UDP ingest pipeline and is not part of
// any public surface.
// ============================================================================
internal static class RawPacketRecordTypedFactory
{
    // ------------------------------------------------------------------------
    // Create(ReadOnlyMemory<char>)
    // ------------------------------------------------------------------------
    // Overload used when callers already hold the UDP payload as a
    // ReadOnlyMemory<char> slice (e.g. from a pooled char buffer after
    // UTF-8 decode). Materializes the memory to a string exactly once so
    // the same instance can be persisted and inspected for the 'type' field.
    // ------------------------------------------------------------------------
    internal static IRawPacketRecordTyped Create(ReadOnlyMemory<char> rawPacketJsonAsReadOnlyMemoryOfChar)
    {
        // Materialize the payload once; ToString() on ReadOnlyMemory<char>
        // allocates a single string covering the full slice.
        var rawPacketJsonAsString = rawPacketJsonAsReadOnlyMemoryOfChar.ToString();

        return new RawPacketRecordTyped(
            // COMB GUID: a GUID whose high bits encode the current time so
            // primary-key inserts remain roughly monotonic in SQLite.
            IdGenerator.CreateCombGuid(),
            // Application-received timestamp captured at ingest, NOT the
            // station's reported timestamp. Stored as Unix epoch seconds
            // (integer) to keep the schema compact and timezone-neutral.
            DateTime.UtcNow.ToUnixEpochSeconds(),
            // Preserve the original JSON verbatim so no information is lost
            // on the way to persistence / stream shipping.
            rawPacketJsonAsString,
            // Classify the packet up-front so downstream consumers can route
            // by enum without having to re-parse the JSON.
            ExtractPacketEnumKey(rawPacketJsonAsString)
        );
    }

    // ------------------------------------------------------------------------
    // Create(Span<char>)
    // ------------------------------------------------------------------------
    // Overload used when callers hold the payload on the stack / in a
    // writable char buffer. Span<char> cannot be captured across awaits or
    // stored on the heap, so we copy it to a string immediately.
    //
    // Behavior is otherwise identical to the ReadOnlyMemory<char> overload.
    // ------------------------------------------------------------------------
    internal static IRawPacketRecordTyped Create(Span<char> rawPacketJsonAsSpanOfChar)
    {
        // Span<char>.ToString() allocates a single string containing the
        // span's characters; this is the safe bridge from stack memory to
        // the heap-allocated record.
        var rawPacketJsonAsString = rawPacketJsonAsSpanOfChar.ToString();

        return new RawPacketRecordTyped(
            IdGenerator.CreateCombGuid(),
            DateTime.UtcNow.ToUnixEpochSeconds(),
            rawPacketJsonAsString,
            ExtractPacketEnumKey(rawPacketJsonAsString)
        );
    }

    // ------------------------------------------------------------------------
    // ExtractPacketEnumKey
    // ------------------------------------------------------------------------
    // Parses the UDP JSON just far enough to read the top-level "type" field
    // and maps that string to the PacketEnum used throughout the pipeline.
    //
    // Tempest UDP packets always carry a discriminator at the root, e.g.:
    //     { "serial_number":"ST-...", "type":"obs_st", "obs":[[...]] }
    //     { "serial_number":"HB-...", "type":"rapid_wind", "ob":[...] }
    //
    // Unknown / unsupported values map to PacketEnum.NotImplemented so the
    // ingest layer can still persist the raw bytes without dropping data.
    // ------------------------------------------------------------------------
    static PacketEnum ExtractPacketEnumKey(string udpPacketAsString)
    {
        try
        {
            // Full JsonDocument parse is acceptable here: packets are small
            // (well under 1 KB) and only the root object is inspected.
            var udpPacketAsJsonDocument = JsonDocument.Parse(udpPacketAsString);

            // Read the "type" property. Two failure modes are treated as
            // malformed input and surfaced as InvalidOperationException:
            //   1) the property is missing entirely
            //   2) the property exists but the value is JSON null
            var packetEnumKeyAsString = udpPacketAsJsonDocument.RootElement.TryGetProperty("type", out var typeProp)
                        ? typeProp.GetString()
                            ?? throw new InvalidOperationException("'type' field is null.")
                        : throw new InvalidOperationException("Missing 'type' field in JSON document.");

            // Look the string up in the shared string -> enum dictionary.
            // Supported types (obs_st, rapid_wind, evt_strike, hub_status, ...)
            // return true; anything else falls through to NotImplemented so
            // the caller can still ingest and persist the packet.
            var isSupportedType = DictionaryOfPacketTypeStringToPacketEnumKey.TryGet(
                packetEnumKeyAsString, out var packetEnumKey);
            return isSupportedType ? packetEnumKey : PacketEnum.NotImplemented;
        }
        catch (Exception ex)
        {
            // Wrap any parse / lookup failure in a single, specific
            // exception type so upstream error handling has a stable
            // contract. The original exception is preserved as InnerException
            // for diagnostics.
            throw new InvalidOperationException("Failed to extract PacketEnum from UDP packet JSON.", ex);
        }
    }
}