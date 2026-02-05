namespace MetWorks.Common.Metrics;

using System.Globalization;
using System.Text.Json;

public static class MetricsStructuredSnapshotParser
{
    public static bool TryParse(string json, out MetricsStructuredSnapshot snapshot)
    {
        snapshot = default!;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var schemaVersion = TryGetInt32(root, "schema_version") ?? 0;
            var capturedUtc = TryGetDateTimeUtc(root, "captured_utc") ?? DateTime.MinValue;
            var intervalSeconds = TryGetInt32(root, "interval_seconds") ?? 0;

            var process = TryParseProcess(root);
            var relay = TryParseRelay(root);
            var pipeline = TryParsePipeline(root);

            snapshot = new MetricsStructuredSnapshot(
                SchemaVersion: schemaVersion,
                CapturedUtc: capturedUtc,
                IntervalSeconds: intervalSeconds,
                Process: process,
                Relay: relay,
                Pipeline: pipeline);

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static MetricsProcessSnapshot? TryParseProcess(JsonElement root)
    {
        if (!root.TryGetProperty("process", out var p) || p.ValueKind != JsonValueKind.Object)
            return null;

        var cpuSecondsDelta = TryGetDouble(p, "cpu_seconds_delta") ?? 0;
        var cpuUtilizationRatio = TryGetDouble(p, "cpu_utilization_ratio") ?? 0;
        var processorCount = TryGetInt32(p, "processor_count") ?? 0;
        var threads = TryGetInt32(p, "threads") ?? 0;

        var gc = new MetricsGcSnapshot(
            Gen0Delta: 0,
            Gen1Delta: 0,
            Gen2Delta: 0,
            ManagedMemoryBytes: 0);

        if (p.TryGetProperty("gc", out var g) && g.ValueKind == JsonValueKind.Object)
        {
            gc = new MetricsGcSnapshot(
                Gen0Delta: TryGetInt32(g, "gen0_delta") ?? 0,
                Gen1Delta: TryGetInt32(g, "gen1_delta") ?? 0,
                Gen2Delta: TryGetInt32(g, "gen2_delta") ?? 0,
                ManagedMemoryBytes: TryGetInt64(g, "managed_memory_bytes") ?? 0);
        }

        return new MetricsProcessSnapshot(
            CpuSecondsDelta: cpuSecondsDelta,
            CpuUtilizationRatio: cpuUtilizationRatio,
            ProcessorCount: processorCount,
            Threads: threads,
            Gc: gc);
    }

    static MetricsRelaySnapshot? TryParseRelay(JsonElement root)
    {
        if (!root.TryGetProperty("relay", out var r) || r.ValueKind != JsonValueKind.Object)
            return null;

        var topHandlers = new List<MetricsRelayHandlerHotspot>();
        if (r.TryGetProperty("top_handlers", out var handlers) && handlers.ValueKind == JsonValueKind.Array)
        {
            foreach (var h in handlers.EnumerateArray())
            {
                if (h.ValueKind != JsonValueKind.Object)
                    continue;

                var messageType = TryGetString(h, "message_type") ?? "";
                var recipientType = TryGetString(h, "recipient_type") ?? "";

                topHandlers.Add(new MetricsRelayHandlerHotspot(
                    MessageType: messageType,
                    RecipientType: recipientType,
                    Count: TryGetInt64(h, "count") ?? 0,
                    TotalMs: TryGetDouble(h, "total_ms") ?? 0,
                    AvgMs: TryGetDouble(h, "avg_ms") ?? 0,
                    MaxMs: TryGetDouble(h, "max_ms") ?? 0));
            }
        }

        var topFanout = new List<MetricsRelayFanoutHotspot>();
        if (r.TryGetProperty("top_fanout", out var fanout) && fanout.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in fanout.EnumerateArray())
            {
                if (f.ValueKind != JsonValueKind.Object)
                    continue;

                topFanout.Add(new MetricsRelayFanoutHotspot(
                    MessageType: TryGetString(f, "message_type") ?? "",
                    HandlerInvocations: TryGetInt64(f, "handler_invocations") ?? 0));
            }
        }

        return new MetricsRelaySnapshot(
            TopHandlers: topHandlers,
            TopFanout: topFanout);
    }

    static MetricsPipelineSnapshot? TryParsePipeline(JsonElement root)
    {
        if (!root.TryGetProperty("pipeline", out var p) || p.ValueKind != JsonValueKind.Object)
            return null;

        var topReadings = new List<MetricsPipelineReadingHotspot>();
        if (p.TryGetProperty("top_readings", out var readings) && readings.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in readings.EnumerateArray())
            {
                if (e.ValueKind != JsonValueKind.Object)
                    continue;

                topReadings.Add(new MetricsPipelineReadingHotspot(
                    ReadingType: TryGetString(e, "reading_type") ?? "",
                    Count: TryGetInt64(e, "count") ?? 0,
                    Retransforms: TryGetInt64(e, "retransforms") ?? 0,
                    UdpToTransformStartAvgMs: TryGetDouble(e, "udp_to_transform_start_avg_ms") ?? 0,
                    UdpToTransformStartMaxMs: TryGetDouble(e, "udp_to_transform_start_max_ms") ?? 0,
                    TransformAvgMs: TryGetDouble(e, "transform_avg_ms") ?? 0,
                    TransformMaxMs: TryGetDouble(e, "transform_max_ms") ?? 0,
                    UdpToTransformEndAvgMs: TryGetDouble(e, "udp_to_transform_end_avg_ms") ?? 0,
                    UdpToTransformEndMaxMs: TryGetDouble(e, "udp_to_transform_end_max_ms") ?? 0));
            }
        }

        return new MetricsPipelineSnapshot(TopReadings: topReadings);
    }

    static int? TryGetInt32(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el))
            return null;

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i))
            return i;

        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
            return s;

        return null;
    }

    static long? TryGetInt64(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el))
            return null;

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var i))
            return i;

        if (el.ValueKind == JsonValueKind.String && long.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
            return s;

        return null;
    }

    static double? TryGetDouble(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el))
            return null;

        if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var d))
            return d;

        if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s))
            return s;

        return null;
    }

    static string? TryGetString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el))
            return null;

        if (el.ValueKind == JsonValueKind.String)
            return el.GetString();

        return el.ToString();
    }

    static DateTime? TryGetDateTimeUtc(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var el))
            return null;

        if (el.ValueKind != JsonValueKind.String)
            return null;

        var s = el.GetString();
        if (string.IsNullOrWhiteSpace(s))
            return null;

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        return null;
    }
}
