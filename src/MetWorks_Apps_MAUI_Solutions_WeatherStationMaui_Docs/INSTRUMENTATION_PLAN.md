# Instrumentation plan (draft)

## Objectives

### Objective 1: Processing vs idle over time

Provide a practical estimate (over a configurable time window) of how much time the app is:

- **Processing (direct CPU time):** time spent executing this app’s managed code (handlers, transforms, viewmodel updates, etc.).
- **Processing (indirect CPU time):** time spent in work triggered by this app (e.g., JSON parse, encryption, DB client work, socket handling), even if not “our” code.
- **Idle / waiting:** time spent blocked waiting for work (sleeping, awaiting timed callbacks, waiting on I/O completion, waiting on locks, etc.).

This is meant to answer: “If we add another sink after UI-ready prep, are we likely to choke the host?”

### Objective 2: Identify hot message paths and handler costs

For the event-driven portions of the system, identify:

- Which message types have the highest fan-out
- Which recipients/handlers dominate CPU time
- Whether the system is mostly idle between packets or continuously busy

### Objective 3: Establish a safe baseline before new sinks

Before adding a new database sink (or any additional workload), establish a baseline report so we can compare “before” and “after”.

---

## Constraints

- Keep overhead low; instrumentation must not materially change throughput.
- Avoid adding new interface layers just for instrumentation.
- Prefer existing correlation and timing fields (`Id`, `SourcePacketId`, `Provenance`) over new message base types.
- Prefer aggregated metrics over per-event logging (avoid flooding file/console and avoid DB writes on hot code paths).

---

## Metrics naming & tagging baseline (System.Diagnostics.Metrics)

To keep metrics usable over time, treat metric names and tags as a stable public API. This section defines the initial naming/tagging baseline so we can avoid revisiting these decisions repeatedly later.

### Metric naming

- Metric names are **dot-separated**, lowercase, and hierarchical.
- Metric names include **units** where applicable.
- Metric names avoid embedding variable values (use tags for dimensions).

Prefix:

- All metrics start with: `metworks.weatherstation`

Examples:

- `metworks.weatherstation.udp.packets_received_total`
- `metworks.weatherstation.udp.errors_total`
- `metworks.weatherstation.udp.receive_timeouts_total`
- `metworks.weatherstation.udp.process_packet_duration_seconds`
- `metworks.weatherstation.transform.duration_seconds`
- `metworks.weatherstation.relay.handler_duration_seconds`
- `metworks.weatherstation.sink.postgres.write_duration_seconds`

Suffix conventions:

- `_total` = monotonic totals (`Counter<T>`)
- `_current` = sampled current value (`ObservableGauge<T>`)
- `_seconds` / `_ms` / `_bytes` = unit-bearing values (typically histograms)

### Meter naming

Use one `Meter` per subsystem:

- `MetWorks.WeatherStation.Udp`
- `MetWorks.WeatherStation.Transform`
- `MetWorks.WeatherStation.EventRelay`
- `MetWorks.WeatherStation.Sink.Postgres`

### Tags (dimensions)

Keep tag cardinality low.

Recommended tags:

- UDP:
  - `packet_type`: `wind|observation|precipitation|lightning|not_implemented`
  - `error_kind`: `socket|exception|timeout`
  - `socket_error`: `NetworkDown|ConnectionReset|...` (only when `error_kind=socket`)
  - `exception_type`: use sparingly (only when `error_kind=exception`)

- Relay:
  - `message_type`: short stable type name
  - `recipient_type`: short stable type name

- Postgres sink:
  - `table`: `wind|observation|precipitation|lightning|station_metadata`
  - `sql_state`: use only if it remains low-cardinality

---

## Measurement strategy (multi-layer)

### Layer A: Coarse host load (what the machine actually experiences)

Goal: answer “how busy is this process?”

Recommended signals (Windows):

- **Process CPU usage** (% of one core or normalized to all cores)
- **Thread count**
- **GC metrics** (alloc rate, GC pause time)

Implementation candidates:

- `System.Diagnostics.Process` CPU time deltas (cheap, coarse)
- `EventCounters` / `System.Diagnostics.Metrics` for runtime GC

Outcome: a periodic snapshot (e.g., every 5s) producing a “CPU busy vs idle” estimate for the process.

Notes:

- This measures *direct + indirect* CPU time consumed by the process.
- It does not attribute time to specific message types.

### Layer B: Relay-level attribution (what our code spends time doing)

Goal: attribute time to message handlers and message types.

Signals:

- Handler execution time: `handlerEnd - handlerStart` (per recipient per message type)
- Send→handler start latency (best-effort): `handlerStart - sendTimestamp`
- Fan-out count: number of handlers invoked per message instance

Implementation candidates:

- Wrap `Register<TMessage>` handlers in `EventRelayBasic`
- Use `Stopwatch.GetTimestamp()` for cheap timing
- Keep a bounded in-memory aggregator (e.g., `ConcurrentDictionary<(MessageType, RecipientType), Stats>`)

Outcome: periodic aggregated report for top-N hot message/handler pairs.

Notes:

- This is “direct” time in our handler code.
- It does not count time spent inside the runtime or native I/O beyond the measured handler wall-clock.

### Layer C: Pipeline correlation (end-to-end for readings)

Goal: correlate raw packet reception → transformed reading → UI update.

Signals already present:

- `IRawPacketRecordTyped.Id`
- `I*Reading.SourcePacketId`
- `IReadingProvenance.UdpReceiptTime`
- `IReadingProvenance.TransformStartTime/TransformEndTime`

Outcome:

- End-to-end durations from provenance, independent from relay instrumentation.

---

## Plan to implement (phased)

## Implemented (Phases 12)

The repository currently implements Phase 1 and Phase 2 as a **log-emitted JSON summary** on a periodic sampling interval (default 10 seconds). Persistence to PostgreSQL is not wired yet (see comment in `MetricsSamplerService`).

The repository also implements Phase 5 metrics persistence as a **best-effort PostgreSQL insert** of the per-interval JSON summary (one row per sampling interval). Failed inserts are logged and dropped.

### Where the code lives

- Phase 1 (process sampler + summary emitter): `src/MetWorks_Common/Metrics/MetricsSamplerService.cs`
- Phase 2 (EventRelay handler timing + aggregation):
  - Instrumentation wrapper: `src/MetWorks_EventRelay/EventRelayBasic.cs`
  - Aggregator: `src/MetWorks_EventRelay/RelayMetricsAggregator.cs`
- Phase 5 (PostgreSQL metrics summary persistence):
  - Ingestor: `src/MetWorks_Common/Metrics/MetricsSummaryIngestor.cs`

Startup wiring:

- The service registry initializes the sampler via generated registry init: `src/MetWorks_DdiRegistry/Registry.g.cs`  `TheMetricsSampler_Initializer.g.cs`.

DDI wiring notes (WeatherStationMaui.yaml):

- In the DDI `instance:` section, named instances must be defined before any other instance references them (no forward references).
- `namespace:` class entries must include all initializer parameters that are assigned in `instance:`.
- Dotted instance property access (e.g., `RootCancellationTokenSource.Token`, `TheInstanceIdentifier.InstallationId`) requires the property be declared under that class’s `property:` list in `namespace:`.

### Enable/disable toggles (settings)

All collection is **off by default**.

Definitions are in `src/MetWorks_Resource_Store/data/settings.yaml` under `/services/metrics/*`.

- `/services/metrics/enabled` (bool)
  - Master switch for `MetricsSamplerService`.
- `/services/metrics/connectionString` (string)
  - PostgreSQL connection string for metrics summary persistence.
- `/services/metrics/tableName` (string)
  - Table name for metrics summary persistence (default `metrics_summary`).
- `/services/metrics/autoCreateTable` (bool)
  - When true, the metrics summary table is created if missing.
- `/services/metrics/captureIntervalSeconds` (int)
  - Sampling interval. Defaults to 10s if unset/invalid.
- `/services/metrics/relayEnabled` (bool)
  - Enables Phase 2 handler timing wrapper in `EventRelayBasic`.
- `/services/metrics/relayTopN` (int)
  - Controls the number of top (message_type, recipient_type) pairs included per interval.
- `/services/metrics/pipelineEnabled` (bool)
  - Enables Phase 3 provenance-based pipeline timing summaries for `IWeatherReading` messages.
- `/services/metrics/pipelineTopN` (int)
  - Controls the number of top reading types included per interval.

### Output format (current)

`MetricsSamplerService` emits one log line per interval:

- Prefix: `METRICS `
- Payload: JSON serialized via `System.Text.Json`

The JSON shape (schema_version=1):

- `schema_version` (int)
- `captured_utc` (UTC timestamp)
- `interval_seconds` (int, rounded wall time)
- `process`
  - `cpu_seconds_delta` (double)
  - `cpu_utilization_ratio` (double)
    - Computed as `cpuDeltaSeconds / (wallDeltaSeconds * Environment.ProcessorCount)`
  - `processor_count` (int)
  - `threads` (int)
  - `gc`
    - `gen0_delta` (int)
    - `gen1_delta` (int)
    - `gen2_delta` (int)
    - `managed_memory_bytes` (long)
- `relay` (object | null)
  - Present only when `/services/metrics/relayEnabled=true`
  - `top_handlers` (array)
    - Each entry:
      - `message_type` (string, short type name)
      - `recipient_type` (string, short type name)
      - `count` (long)
      - `total_ms` (double)
      - `avg_ms` (double)
      - `max_ms` (double)
  - `top_fanout` (array)
    - Top message types by handler invocations during the interval.
    - Each entry:
      - `message_type` (string, short type name)
      - `handler_invocations` (long)

- `pipeline` (object | null)
  - Present only when `/services/metrics/pipelineEnabled=true`
  - Source: `IWeatherReading.Provenance` timestamps (no transformer modifications required)
  - `top_readings` (array)
    - Top reading types by total UDP→transform-end time during the interval
    - Each entry:
      - `reading_type` (string, short type name)
      - `count` (long)
      - `retransforms` (long)
      - `udp_to_transform_start_avg_ms` (double)
      - `udp_to_transform_start_max_ms` (double)
      - `transform_avg_ms` (double)
      - `transform_max_ms` (double)
      - `udp_to_transform_end_avg_ms` (double)
      - `udp_to_transform_end_max_ms` (double)

Notes:

- Phase 2 aggregation is interval-scoped: the sampler calls `EventRelayBasic.GetTopRelayHotspotsSnapshot(topN)` which snapshots **and resets** the aggregator each interval.
- Handler timings use `Stopwatch.GetTimestamp()` around the handler invocation and record elapsed ticks.
- Fan-out is measured as **handler invocations per message type** (incremented once per handler call). It is also snapshot-and-reset per interval.
- Pipeline timings are recorded when relay metrics are enabled and the message is an `IWeatherReading`; durations are computed from provenance:
  - UDP→transform start = `TransformStartTime - UdpReceiptTime`
  - transform duration = `TransformEndTime - TransformStartTime`
  - UDP→transform end = `TransformEndTime - UdpReceiptTime`
  - retransforms are counted via `Provenance.IsRetransformation`

- Pipeline summaries may also include entries for `IRawPacketRecordTyped`.
  - These represent an estimate of UDP receipt → relay handler start latency using `IRawPacketRecordTyped.ReceivedTime` and `DateTime.UtcNow` at handler invocation.
  - For these entries, the reported `transform_*` fields will be 0.

### Output format details (field-by-field)

This section describes what each JSON node represents, how it is collected/calculated, and why it is useful.

#### Top-level envelope

- `schema_version` (int)
  - What: Version for the emitted JSON contract.
  - How: Constant in `MetricsSamplerService`.
  - Value: Allows forward evolution without breaking consumers.

- `captured_utc` (string timestamp)
  - What: UTC timestamp for when the interval snapshot was captured.
  - How: `DateTime.UtcNow` in `MetricsSamplerService` at snapshot time.
  - Value: Use as the row’s time axis when charting/aggregating.

- `interval_seconds` (int)
  - What: Approximate wall-clock duration covered by the snapshot.
  - How: Computed from `nowWall - lastWall`, rounded to whole seconds.
  - Value: Makes rates comparable if the timer drifted.

#### `process`

Goal: coarse “how busy is the process overall?” signal. This captures *direct + indirect* CPU time in the process.

- `cpu_seconds_delta` (double)
  - What: Amount of CPU time consumed by the process during the interval (in seconds).
  - How: `Process.GetCurrentProcess().TotalProcessorTime` is sampled each interval and a delta is computed.
  - Value: Primary indicator of real CPU work (includes runtime/native work done by the process, not just app code).

- `cpu_utilization_ratio` (double)
  - What: CPU usage normalized to all logical processors.
  - How: `cpu_seconds_delta / (interval_seconds * processor_count)`.
    - Example: `0.50` means the process used ~50% of *all* logical CPU capacity over the interval.
  - Value: Easy “busy vs idle” number that’s comparable across machines.

- `processor_count` (int)
  - What: Logical processor count on the host.
  - How: `Environment.ProcessorCount`.
  - Value: Makes `cpu_utilization_ratio` self-explanatory from one row.

- `threads` (int)
  - What: Current OS thread count for the process.
  - How: `Process.GetCurrentProcess().Threads.Count`.
  - Value: Detect excessive thread growth, thread leaks, or unexpected concurrency.

- `gc` (object)
  - `gen0_delta` / `gen1_delta` / `gen2_delta` (int)
    - What: delta count of collections per generation within the interval.
    - How: `GC.CollectionCount(gen)` sampled each interval and differenced.
    - Value: A quick signal of allocation pressure and memory churn.
  - `managed_memory_bytes` (long)
    - What: Approximate managed heap size at snapshot time.
    - How: `GC.GetTotalMemory(forceFullCollection: false)`.
    - Value: Detect managed memory growth and correlate with GC counts.

#### `relay` (object | null)

Present only when `/services/metrics/relayEnabled=true`.

Goal: identify which message handlers and message types dominate execution time.

- `top_handlers` (array)
  - What: Top N `(message_type, recipient_type)` pairs ranked by total handler wall time in the interval.
  - How:
    - `EventRelayBasic.Register<TMessage>` wraps each handler.
    - Timing is taken with `Stopwatch.GetTimestamp()` around the handler invocation.
    - Aggregation is maintained in-memory in `RelayMetricsAggregator` and reset each interval.
  - Fields:
    - `message_type`: short stable message type name.
    - `recipient_type`: short stable recipient (handler owner) type name.
    - `count`: number of handler invocations recorded.
    - `total_ms`: sum of elapsed handler durations.
    - `avg_ms`: `total_ms / count`.
    - `max_ms`: maximum single observed handler duration.
  - Value:
    - Locates CPU-heavy handlers.
    - `max_ms` helps identify occasional stalls (GC, UI thread contention, locks).

- `top_fanout` (array)
  - What: Top message types by handler invocation count.
  - How: incremented in the same `EventRelayBasic` wrapper once per handler invocation.
  - Fields:
    - `message_type`: short stable type name.
    - `handler_invocations`: count of handler calls.
  - Value:
    - Highlights high-broadcast message types and potential amplification.

#### `pipeline` (object | null)

Present only when `/services/metrics/pipelineEnabled=true`.

Goal: summarize end-to-end timing from packet receipt to transform boundaries using provenance timestamps.

- `top_readings` (array)
  - What: Top reading types by total UDP→transform-end time during the interval.
  - How:
    - For `IWeatherReading` messages, `EventRelayBasic` reads `IWeatherReading.Provenance`.
    - Durations are computed from provenance timestamps and aggregated by reading type.
    - Aggregation is maintained in-memory in `PipelineMetricsAggregator` and reset each interval.
  - Fields:
    - `reading_type`: short stable reading interface/type name.
    - `count`: number of readings observed in interval.
    - `retransforms`: count of readings where `Provenance.IsRetransformation` was true.
    - `udp_to_transform_start_avg_ms` / `_max_ms`:
      - Computed as `(TransformStartTime - UdpReceiptTime)`.
      - Interprets “how long did the reading wait between UDP receipt and transform begin?”
    - `transform_avg_ms` / `_max_ms`:
      - Computed as `(TransformEndTime - TransformStartTime)`.
      - Interprets “how long did transform work take?” (as defined by provenance).
    - `udp_to_transform_end_avg_ms` / `_max_ms`:
      - Computed as `(TransformEndTime - UdpReceiptTime)`.
      - Interprets “overall latency from UDP receipt to transform completion.”
  - Value:
    - Separates queueing/dispatch delay from transform execution time.
    - Retransform counts help detect too-frequent “recompute” patterns.

- `IRawPacketRecordTyped` pipeline entries (special case)
  - What: When present, these represent an estimate of UDP receipt → relay handler start latency for raw packet messages.
  - How:
    - Uses `IRawPacketRecordTyped.ReceivedTime` as the receipt marker.
    - Uses `DateTime.UtcNow` at handler invocation.
    - Records `udp_to_transform_*` as that delta, with `transform_* = 0`.
  - Value:
    - Helps distinguish whether large “UDP→transform” time is happening before the transformer ever sees packets.

### Phase 0: Define output format

- Decide on a single output format for reports:
  - Debug log line(s)
  - Optional file under app data
  - Optional in-memory UI page later

Decisions (v1):

- Primary persistence target is **PostgreSQL on the LAN** (Android wall devices are not expected to expose local files).
- Persist metrics as **one “summary row” per sampling interval** (default 10s) rather than one row per metric.
  - Rationale: keeps write volume low and allows schema flexibility early.
- The summary payload is stored as **`jsonb`**.
  - Rationale: tolerate evolution and poor early decisions without schema churn.
- A separate metrics pipeline is used (kept independent from application log events).
- A single table is used for all metrics summaries.
- Metrics must be disableable.
  - Start with metrics disabled by default.
  - Phase in finer-grained enable/disable switches per subsystem (udp/relay/transform/sinks) as the implementation matures.
  - Intended primarily for product improvement and wall-device tuning (not necessarily enabled for “field” deployments).

Proposed table shape (v1):

- `comb_id` (uuid, PK)
- `installation_id` (uuid, not null)
- `captured_utc` (timestamptz, not null)
- `capture_interval_seconds` (int, not null)
- `application_id` (uuid, not null)
  - Stable identity for the app across renames.
- `schema_version` (int, not null)
- `json_metrics_summary` (jsonb, not null)
- Optional columns:
  - `platform` (text)
  - `app_version` (text)
  - `json_metadata` (jsonb)

Notes:

- `json_metrics_summary` may contain multiple “summary types” (udp/relay/transform/sinks) inside a single row.
- If/when query performance becomes important, Postgres can project commonly used JSON fields into:
  - generated columns,
  - indexes on expression paths, or
  - materialized views / post-processing tables.

### Phase 1: Add process-level sampler

- Periodically sample process CPU time and compute utilization over the interval.
- Capture baseline “busy vs idle” for the process.

### Phase 2: Add relay-level aggregation (EventRelayBasic)

- Add optional (off-by-default) handler wrapper timing.
- Aggregate by message type + recipient type.
- Provide a periodic “top offenders” summary.

### Phase 3: Use provenance for end-to-end reading timing

- Add a small report that summarizes:
  - transform durations
  - UDP→transform durations
  - retransformation frequency

### Phase 4: Baseline report

- Run for a fixed time window (e.g., 10 minutes) under typical load.
- Record baseline results.

### Phase 5: Add new sink

- Implement the additional sink.
- Repeat the baseline report and compare.

---

## Open questions

- Which platforms matter most for the load estimate (Windows vs Android)?
  - Android (primary concern due to lower-spec hardware on deployed devices)

- Where should reports be emitted (log/file/UI)?
  - Logs first (lowest friction)

- Do we need per-installation persistence of metrics, or is ad-hoc debugging enough initially?
  - Per-installation persistence so we can distinguish deployed "wall" devices from dev/debug machines

- What sampling interval is acceptable (1s vs 5s vs 10s)?
  - Default 10s (ensures multiple wind readings per interval)
  - Prefer parameter-driven/configurable later
