# Tempest REST Observations + UDP/REST Mux (Implementation Plan)

This document is a best-effort breakdown of the work needed to implement the mini-spec in `mini-spec.md`.

## Phase 0: Confirm REST contract details
1. Confirm the exact endpoint, auth mechanism, and response payload shape for station observations.
   - Identify how the API expects the token (query param vs bearer header).
   - Capture a representative JSON payload (real or sample) to drive parsing.
   - Use `curl` (or Postman) to validate auth and payload shape.
     - Forecast example (known working pattern; token as query param):
       - `curl -X GET --header 'Accept: application/json' 'https://swd.weatherflow.com/swd/rest/better_forecast?station_id=<station_id>&token=<api_key>'`
     - Observation endpoint example (verify exact path in swagger):
       - `curl -X GET --header 'Accept: application/json' 'https://swd.weatherflow.com/swd/rest/observations/station/<station_id>?token=<api_key>'`
     - Do not commit real station IDs or API keys to the repo.
   - Sample payload (redacted): `GetStationObservationResultSample.json`
2. Confirm the timestamp fields used for observation time.
   - Identify whether the payload provides station-local time or UTC epoch seconds.
   - Current sample indicates `obs[0].timestamp` is epoch seconds.

## Phase 1: Add settings surface area
1. Add setting constants.
   - Update `src/MetWorks_Constants/SettingConstants.cs`.
2. Add setting group definitions.
   - Update `src/MetWorks_Constants/LookupDictionaries.cs`.
3. Add settings definitions.
   - Update `src/MetWorks_Resource_Store/data/settings.yaml`.
   - Keep `definitions` sorted lexicographically by `path`.

Suggested new setting paths (sorted):
- `/services/tempestObservations/refreshIntervalMinutes` (default `15`, min `5` enforced in code)
- `/services/weatherIngest/restStaleMinutes` (default `20`)
- `/services/weatherIngest/sourceMode` (default `Auto`, allowable `Auto|RestOnly|UdpOnly`)
- `/services/weatherIngest/udpStaleSeconds` (default `90`)

## Phase 2: REST client support
1. Extend `ITempestRestClient`.
   - Add `GetStationObservationsAsync(...)` returning a snapshot that preserves raw JSON.
2. Implement the REST observations call in `TempestRestClient`.
   - Use resilient HTTP patterns already present in the repo:
     - `SendAsync(..., ResponseHeadersRead)`
     - `ReadAsStreamAsync`
     - `JsonDocument.ParseAsync`
   - Capture:
     - `StationId`
     - `RetrievedUtc = DateTimeOffset.UtcNow`
     - `RawJson = doc.RootElement.GetRawText()`
   - Ensure timeouts + cancellation are honored.

## Phase 3: Add REST observation superset message
1. Add a new record message type (location TBD; prefer an interfaces/contract project).
   - `TempestRestObservationsSnapshot(StationId, RetrievedUtc, RawJson)`
   - Note: keep this distinct from the REST client return type (`TempestStationObservationsSnapshot`) so the event-relay message remains stable even if the client surface evolves.
2. Optionally add small helper types for parsed obs arrays later.

## Phase 4: Implement REST polling provider
1. Create `TempestRestObservationsProvider : ServiceBase`.
   - Parameterless constructor.
   - `InitializeAsync(ILogger, ISettingRepository, IEventRelayBasic, ITempestRestClient, CancellationToken, ...)`.
2. Implement the refresh loop.
   - Default interval 15 minutes.
   - Enforce minimum 5 minutes.
   - Best-effort behavior:
     - log failures
     - keep running
     - do not crash the app.
3. Add on-demand refresh entry point.
   - Add `RequestRefreshAsync(...)` or similar.
   - On-demand refresh must not shift cadence.

## Phase 5: Define mux status + selection model
1. Add a `WeatherIngestSource` enum.
   - Values: `None`, `Rest`, `Udp`.
2. Add `WeatherIngestStatus` record.
   - Include active source + availability + freshness + last times.
   - Keep the message stable and UI-friendly.

## Phase 6: Implement REST-to-UI reading mapping
1. Decide the provenance strategy for REST-derived readings.
   - Initial recommendation: synthesize provenance values (e.g., `RawPacketId = Guid.Empty`) and set `TransformerVersion` to indicate REST origin.
2. Implement a REST parsing/transform step that creates:
   - `IObservationReading` (using `ObservationReading`)
   - `IWindReading` (using `WindReading`)
3. Keep unit conversions consistent.
   - Prefer reusing the existing preferred-unit settings.
   - Reuse derived calculators where possible (`DerivedObservationCalculator`).
4. Keep time zone behavior consistent.
   - Use station `timezone_offset_minutes` when available (similar to forecast handling).
   - Fall back to device local time if missing.

## Phase 7: Implement the UDP/REST mux
1. Create `WeatherReadingMux`.
   - Subscribe to UDP-derived `IObservationReading` and `IWindReading`.
   - Subscribe to `TempestRestObservationsSnapshot`.
2. Track “last seen” timestamps per source.
   - UDP: last received UTC (or `ReceivedUtc` from readings).
   - REST: last successful fetch UTC.
3. Apply selection policy.
   - In `Auto` mode:
     - prefer UDP when fresh
     - else REST when fresh
     - else `None`.
   - Settings-backed thresholds:
     - `udpStaleSeconds`
     - `restStaleMinutes`.
4. Publish canonical UI stream.
   - Publish only from the active source.
   - Avoid thrash when both sources are available.
5. Publish `WeatherIngestStatus`.
   - Update on message arrivals and on a periodic tick (so stale transitions occur even without new messages).

## Phase 8: UI indicator
1. Update `WeatherViewModel`.
   - Subscribe to `WeatherIngestStatus`.
   - Expose bindable properties for:
     - active source
     - UDP availability/freshness
     - REST availability/freshness.
2. Add a small UI element to display the indicator.
   - Keep it subtle and readable in dark mode.
   - Prefer a single place first (e.g., `LiveWind*.xaml`) then propagate if useful.

## Phase 9: Persistence (raw REST JSON)
1. Add best-effort local persistence for REST snapshots.
   - Option A (fastest): file persistence in app data (pattern used by forecast snapshot).
   - Option B (more extensible): SQLite table for REST snapshots.
2. Keep parsed-column persistence as a follow-on.

ToDo to retain:
- Persist UDP-derived values alongside REST snapshots for future comparison workflows.

## Phase 10: Wire into DDI startup
1. Update the DDI input YAML.
   - File: `src/MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs/WeatherStationMaui.yaml`.
   - Add namespace models for new services/messages as needed.
   - Add instances in define-before-use order.
   - Expose appropriate services to MAUI DI if needed.
2. Regenerate DDI output.
   - Do not hand-edit `*.g.cs`.
   - Ensure generated registry compiles.

## Phase 11: Testing + validation
1. Add tests at the seam points.
   - REST parsing: given sample JSON, verify computed `IObservationReading` and `IWindReading` fields.
   - Mux selection: verify active source selection at threshold boundaries.
2. Manual validation scenarios.
   - UDP only.
   - REST only (no local UDP).
   - Both available (ensure no thrash).
   - Neither available (ensure UI shows `None` and app remains stable).

## Notes / open questions
- The REST observation schema needs to be confirmed from a real payload to ensure robust parsing.
- Provenance for REST-derived readings should be consistent and should not break existing persistence/metrics assumptions.
