# Session restore / running log — 2026-02-21

## Goal
Implement Tempest REST observations ingest and UDP/REST mux, then surface source status in the UI.

## Functional areas touched

### Tempest REST observations mapping (Phase 6)
- Updated `TempestRestReadingsMapper` to support the real `GetStationObservationResultSample.json` payload shape (`obs[0]` as an object).
- Interprets source units via `station_units` and converts to preferred units.

### UDP/REST mux (Phase 7)
- Added `WeatherReadingMux` to select a canonical UI stream from UDP vs REST based on freshness thresholds and mode.
- `WeatherViewModel` now consumes mux-published concrete `ObservationReading`/`WindReading` messages.

### UI ingest-source indicator (Phase 8)
- `WeatherViewModel` subscribes to `WeatherIngestStatus` and exposes bindable properties (`ActiveIngestSource`, freshness + last-seen timestamps).
- `LiveWind1920x1200.xaml` shows a subtle indicator (colored dot + label) for `ActiveIngestSource`.

### REST observations snapshot persistence (Phase 9)
- Added best-effort local file persistence for Tempest REST observations snapshots in `TempestRestObservationsProvider`.
  - Raw JSON: `tempest.observations.snapshot.json`
  - Sidecar metadata (station id + retrieved UTC): `tempest.observations.snapshot.meta.json`
- TODO captured: persist forecast + observations snapshots to SQLite with an old-data purge soon after (forecast payload is large).

### DDI startup wiring (Phase 10)
- Updated `WeatherStationMaui.yaml` to add DDI wiring for:
  - `TempestRestObservationsProvider`
  - `WeatherReadingMux`
- Regenerated `MetWorks_DdiRegistry` (`*.g.cs`) so both services are created/initialized at startup.

## Build/test status
- `dotnet build` succeeded.
