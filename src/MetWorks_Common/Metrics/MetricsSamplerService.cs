namespace MetWorks.Common.Metrics;
public sealed class MetricsSamplerService : ServiceBase
{
    const int DefaultCaptureIntervalSeconds = 10;

    public MetricsSamplerService()
    {
    }

    public async Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        MetricsSummaryIngestor? metricsSummaryIngestor = null,
        IMetricsLatestSnapshot? metricsLatestSnapshotStore = null,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null)
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);

        InitializeBase(
            iLoggerResilient.ForContext(this.GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker);

        await iLoggerResilient.Ready.ConfigureAwait(false);

        var enabled = ISettingRepository.GetValueOrDefault<bool>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_enabled));

        if (!enabled)
        {
            ILogger.Information("MetricsSamplerService is disabled via settings");
            try { MarkReady(); } catch { }
            return true;
        }

        var intervalSeconds = ISettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_captureIntervalSeconds));

        if (intervalSeconds <= 0)
            intervalSeconds = DefaultCaptureIntervalSeconds;

        var relayEnabled = ISettingRepository.GetValueOrDefault<bool>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_relayEnabled));

        EventRelayBasic.RelayMetricsEnabled = relayEnabled;

        var relayTopN = ISettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_relayTopN));

        if (relayTopN <= 0)
            relayTopN = 10;

        var pipelineEnabled = ISettingRepository.GetValueOrDefault<bool>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_pipelineEnabled));

        EventRelayBasic.PipelineMetricsEnabled = pipelineEnabled;

        var pipelineTopN = ISettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.MetricsGroupSettingsDefinition.BuildSettingPath(SettingConstants.Metrics_pipelineTopN));

        if (pipelineTopN <= 0)
            pipelineTopN = 10;

        StartBackground(ct => SamplerLoopAsync(TimeSpan.FromSeconds(intervalSeconds), relayEnabled, relayTopN, pipelineEnabled, pipelineTopN, metricsSummaryIngestor, metricsLatestSnapshotStore, ct));

        try { MarkReady(); } catch { }
        ILogger.Information($"MetricsSamplerService started (interval={intervalSeconds}s, relayEnabled={relayEnabled}, relayTopN={relayTopN}, pipelineEnabled={pipelineEnabled}, pipelineTopN={pipelineTopN})");
        return true;
    }

    async Task SamplerLoopAsync(TimeSpan interval, bool relayEnabled, int relayTopN, bool pipelineEnabled, int pipelineTopN, MetricsSummaryIngestor? metricsSummaryIngestor, IMetricsLatestSnapshot? metricsLatestSnapshotStore, CancellationToken token)
    {
        var proc = Process.GetCurrentProcess();
        var lastWall = DateTime.UtcNow;
        var lastCpu = proc.TotalProcessorTime;
        var lastGen0 = GC.CollectionCount(0);
        var lastGen1 = GC.CollectionCount(1);
        var lastGen2 = GC.CollectionCount(2);

        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, token).ConfigureAwait(false);

                proc.Refresh();
                var nowWall = DateTime.UtcNow;
                var nowCpu = proc.TotalProcessorTime;

                var wallDelta = nowWall - lastWall;
                var cpuDelta = nowCpu - lastCpu;

                var gen0 = GC.CollectionCount(0);
                var gen1 = GC.CollectionCount(1);
                var gen2 = GC.CollectionCount(2);

                var gen0Delta = Math.Max(0, gen0 - lastGen0);
                var gen1Delta = Math.Max(0, gen1 - lastGen1);
                var gen2Delta = Math.Max(0, gen2 - lastGen2);

                var cpuUtil = 0d;
                if (wallDelta.TotalSeconds > 0)
                {
                    cpuUtil = cpuDelta.TotalSeconds / (wallDelta.TotalSeconds * Math.Max(1, Environment.ProcessorCount));
                }

                object? relay = null;
                if (relayEnabled)
                {
                    var top = EventRelayBasic.GetTopRelayHotspotsSnapshot(relayTopN)
                        .Select(h => new
                        {
                            message_type = h.MessageType,
                            recipient_type = h.RecipientType,
                            count = h.Count,
                            total_ms = h.TotalMilliseconds,
                            avg_ms = h.AverageMilliseconds,
                            max_ms = h.MaxMilliseconds
                        })
                        .ToArray();

                    var fanOut = EventRelayBasic.GetTopFanOutSnapshot(relayTopN)
                        .Select(h => new
                        {
                            message_type = h.MessageType,
                            handler_invocations = h.HandlerInvocations
                        })
                        .ToArray();

                    relay = new { top_handlers = top, top_fanout = fanOut };
                }

                object? pipeline = null;
                if (pipelineEnabled)
                {
                    var top = EventRelayBasic.GetTopPipelineSnapshot(pipelineTopN)
                        .Select(h => new
                        {
                            reading_type = h.ReadingType,
                            count = h.Count,
                            retransforms = h.RetransformCount,
                            udp_to_transform_start_avg_ms = h.UdpToTransformStartAvgMs,
                            udp_to_transform_start_max_ms = h.UdpToTransformStartMaxMs,
                            transform_avg_ms = h.TransformAvgMs,
                            transform_max_ms = h.TransformMaxMs,
                            udp_to_transform_end_avg_ms = h.UdpToTransformEndAvgMs,
                            udp_to_transform_end_max_ms = h.UdpToTransformEndMaxMs
                        })
                        .ToArray();

                    pipeline = new { top_readings = top };
                }

                var payload = new
                {
                    schema_version = 1,
                    captured_utc = nowWall,
                    interval_seconds = (int)Math.Round(wallDelta.TotalSeconds),
                    process = new
                    {
                        cpu_seconds_delta = cpuDelta.TotalSeconds,
                        cpu_utilization_ratio = cpuUtil,
                        processor_count = Environment.ProcessorCount,
                        threads = proc.Threads.Count,
                        gc = new
                        {
                            gen0_delta = gen0Delta,
                            gen1_delta = gen1Delta,
                            gen2_delta = gen2Delta,
                            managed_memory_bytes = GC.GetTotalMemory(false)
                        }
                    },
                    relay,
                    pipeline
                };

                // Phase 1: log-only for now. DB persistence wiring happens next.
                var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
                ILogger.Information($"METRICS {payloadJson}");

                if (metricsLatestSnapshotStore is MetricsLatestSnapshotStore concrete)
                {
                    concrete.RecordCaptured(
                        capturedUtc: nowWall,
                        intervalSeconds: (int)Math.Round(wallDelta.TotalSeconds),
                        jsonPayload: payloadJson);
                }

                if (metricsSummaryIngestor is not null)
                {
                    try
                    {
                        await metricsSummaryIngestor.PersistAsync(
                            capturedUtc: nowWall,
                            captureIntervalSeconds: (int)Math.Round(wallDelta.TotalSeconds),
                            schemaVersion: 1,
                            jsonMetricsSummary: payloadJson,
                            cancellationToken: token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (InvalidOperationException ex)
                    {
                        ILogger.Warning($"Metrics persistence failure: {ex.Message}");
                    }
                }

                lastWall = nowWall;
                lastCpu = nowCpu;
                lastGen0 = gen0;
                lastGen1 = gen1;
                lastGen2 = gen2;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                ILogger.Warning($"MetricsSamplerService loop failure: {ex.Message}");
                // brief backoff to avoid tight loop
                try { await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false); } catch { }
            }
        }
    }
}
