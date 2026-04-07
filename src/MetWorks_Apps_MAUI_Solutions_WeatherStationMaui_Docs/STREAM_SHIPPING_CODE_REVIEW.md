# Stream Shipping — Code & Documentation Review

> **Status**: Complete  
> **Scope**: All shipper implementations + `StreamShippingUploadMetrics` + three StreamShipping docs

---

## 1. Findings Summary

| # | Area | Finding | Severity | Action |
|---|------|---------|----------|--------|
| 1 | All shippers + `StreamShippingUploadMetrics` | `source` concept was redundant with `table` — proposed fix superseded by eliminating `source` entirely; `shipper_state` column renamed `[table]` | N/A | **Resolved** — `source` eliminated; `StreamShippingUploadMetrics.Record` takes `table` only |
| 2 | `LOCAL_FIRST_STREAM_SHIPPING_IMPLEMENTATION_PLAN.md` | Stale "Not implemented yet" bullet for DDI wiring (already done; status note contradicts it) | Minor — misleading to readers | Remove the bullet; keep the status note |
| 3 | `LOCAL_FIRST_STREAM_SHIPPING_IMPLEMENTATION_PLAN.md` | Step 1 DDI block for `TheStationMetadataStreamShipper` is missing `stationMetadataDatabaseReadiness -> TheStationMetadataDatabaseReadiness` | Moderate — would break DDI wiring if followed | Add the missing assignment |
| 4 | `LOCAL_FIRST_STREAM_SHIPPING_IMPLEMENTATION_PLAN.md` | `StreamShippingUploadMetrics` is entirely absent from the docs | Moderate — unintelligible for new developers | Add a section describing the class and its integration |
| 5 | `07_RECEIVER_MIGRATION_PLAN.md` | `PrecipitationStreamShipper` (`table=precipitation`) is missing from the architecture diagram and wire format table | Moderate — diagram does not match code | Add `Precipitation` row |
| 6 | `07_RECEIVER_MIGRATION_PLAN.md` | Section 10 cost estimate table has headers but no data | Minor — incomplete | **Resolved** — §10 already filled in (~$11–15/month); no change needed |

---

## 2. Finding #1 — `source` concept eliminated

### Original finding

`ObservationStreamShipper.UploadNdjsonAsync` passed `table` as both the `source` and `table`
arguments to `StreamShippingUploadMetrics.Record`. The proposed fix was to add a dedicated
`source` parameter to make shipper identity explicit.

### Resolution

During implementation it became clear that `source` and `table` were always the same value
for every shipper — there was no scenario where they would meaningfully differ. The `source`
concept was **eliminated entirely** rather than formalised:

- `StreamShippingUploadMetrics.Record(...)` simplified to `(string table, ...)` — no `source` parameter
- `StreamShippingUploadHotspot` and `MetricsShippingUploadHotspot` retain `Table` only
- All `Source` constants removed from shippers
- `shipper_state` schema: column renamed from `source` to `[table]` (square brackets required — `table` is a SQL reserved word); SQL params updated from `$source` to `$table`
- `LoggerSQLiteStreamShipper` NDJSON output: `source` field removed from emitted wire format

The current `Record` signature is:

```csharp
// StreamShippingUploadMetrics.Record — current
public static void Record(
    string table,
    int rows,
    long gzipBytes,
    long elapsedTicks,
    bool success)
```

---

## 3. Finding #2 — Stale "Not implemented yet" bullet

In `LOCAL_FIRST_STREAM_SHIPPING_IMPLEMENTATION_PLAN.md`, under **Not implemented yet**:

> _Confirm/complete `instance:` wiring for all shippers in `WeatherStationMaui.yaml`_

This is immediately followed by an inline status update confirming the wiring IS complete.
The bullet should be removed to avoid confusion. The status note can remain or be folded
into the **Implemented** list.

---

## 4. Finding #3 — Missing DDI assignment for `StationMetadataStreamShipper`

The Step 1 DDI wiring block in the implementation plan lists common shipper assignments
but omits the extra dependency specific to `StationMetadataStreamShipper`. The actual
`InitializeAsync` signature is:

```csharp
public async Task<bool> InitializeAsync(
    ILogger iLogger,
    ISettingRepository iSettingRepository,
    IEventRelayBasic iEventRelayBasic,
    IInstanceIdentifier iInstanceIdentifier,
    IStationMetadataDatabaseReadiness stationMetadataDatabaseReadiness,  // ← not in doc
    IStreamShippingDatabaseReadiness streamShippingDatabaseReadiness,
    IStreamShippingRepository streamShippingRepository,
    HttpClient httpClient,
    CancellationToken externalCancellation,
    ProvenanceTracker provenanceTracker)
```

The doc's `TheStationMetadataStreamShipper` assignment block needs:

```yaml
- stationMetadataDatabaseReadiness -> TheStationMetadataDatabaseReadiness
```

---

## 5. Finding #4 — `StreamShippingUploadMetrics` undocumented

`MetWorks.Common.Metrics.StreamShippingUploadMetrics` is a static lock-free aggregator that
records per-`table` upload attempt metrics. It is consumed by
`MetricsSamplerService`, which calls `SnapshotTopNAndReset(shippingTopN)` each sample
interval and emits the results as `shipping.top_uploads` in the metrics payload.

The class is not mentioned in any of the three StreamShipping documents. It should be
documented in `LOCAL_FIRST_STREAM_SHIPPING_IMPLEMENTATION_PLAN.md` under
**Implemented**, noting:

- Location: `MetWorks.Common.Metrics.StreamShippingUploadMetrics`
- API: `Record(string table, int rows, long gzipBytes, long elapsedTicks, bool success)` — keyed by `table` only
- How it is populated: called from the `finally` block of each `UploadNdjsonAsync` method
- How it is consumed: `MetricsSamplerService.SnapshotTopNAndReset(shippingTopN)` →
  `shipping.top_uploads` in the sampled metrics JSON

---

## 6. Finding #5 — `Precipitation` absent from receiver migration plan

`PrecipitationStreamShipper` (`table=precipitation`) is a fully
implemented shipper. However, the architecture diagram in
`07_RECEIVER_MIGRATION_PLAN.md` §2 only lists:

```
│ Observation   │
│ Wind          │
│ Lightning     │
│ StationMeta   │
│ Logs          │
```

`Precipitation` is missing. It should be added as a sixth row in the diagram and in the
wire format section.

---

## 7. Finding #6 — Empty cost estimate (§10)

The cost estimate table in `07_RECEIVER_MIGRATION_PLAN.md` §10 was found to already have
complete cost data (~$11–15/month) when the review was actioned. No change needed.
