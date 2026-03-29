# Session restore - 2026-03-17

## Touched areas
- Metrics settings
  - Added `/services/metrics/shippingEnabled` and `/services/metrics/shippingTopN` definitions.
  - Added `SettingConstants.Metrics_shippingEnabled` / `SettingConstants.Metrics_shippingTopN`.
  - Extended `LookupDictionaries.MetricsGroupSettingsDefinition` to include the full metrics key set (enabled/interval/table/autoCreate + relay/pipeline/storage + shipping).

- Metrics sampler payload
  - `MetricsSamplerService` now emits a `shipping` node (upload hotspots + per-source shipper_state) gated by metrics settings.
  - Aligned persisted `schemaVersion` with emitted `schema_version`.

- Stream shipping persistence
  - `StreamShippingRepository` now rethrows operation failures as `InvalidOperationException` (instead of base `Exception`).

- Declarative DI
  - Updated `WeatherStationMaui.yaml` `namespace:` signature + `instance:` assignments for `TheMetricsSamplerService` to accept `IStreamShippingDatabaseReadiness` + `IStreamShippingRepository`.

- Structured metrics parsing
  - Extended `MetricsStructuredSnapshot` + `MetricsStructuredSnapshotParser` to parse `storage` and `shipping` nodes.
