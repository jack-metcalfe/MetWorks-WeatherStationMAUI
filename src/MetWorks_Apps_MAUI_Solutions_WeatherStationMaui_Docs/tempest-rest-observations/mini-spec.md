# Tempest REST Observations + UDP/REST Mux (Mini-spec)

## Overview
Add periodic fetching of Tempest REST observations **in addition to** existing UDP ingestion.

The system will:
- Poll REST observations on a configurable interval.
- Publish a **superset** snapshot message (raw JSON preserved).
- Publish **UI-compatible** readings (`IObservationReading`, `IWindReading`) via a mux so the UI continues to bind to the same message contracts.
- Publish a status message so the UI can show which source is active and whether each source is available/fresh.

## Goals
1. Keep the app usable when UDP monitoring is not an option (REST provides degraded-but-usable updates).
2. Enable comparing station/device observations to REST observations using the backend database.
3. Avoid message “thrash” when both UDP and REST are available by selecting a single active source.

## Non-goals (initial)
- Persisting parsed REST columns (planned later).
- Persisting UDP-derived values alongside REST for comparison (planned later).

## Components

### `TempestRestObservationsProvider` (new, long-running service)
- Base: `ServiceBase`.
- Polls the Tempest REST observations endpoint.
- Publishes `TempestRestObservationsSnapshot`.
- Stores:
  - last success time
  - last error (optional)

#### Refresh cadence
- Default: **15 minutes**.
- Minimum: **5 minutes**.
- Future: on-demand refresh via a provider method.

#### On-demand refresh behavior
On-demand refresh should **not** shift the cadence (an immediate refresh should not delay the next scheduled refresh).

### `WeatherReadingMux` (new, long-running service)
- Subscribes to:
  - UDP-derived `IObservationReading`
  - UDP-derived `IWindReading`
  - `TempestRestObservationsSnapshot`
- Publishes:
  - canonical `IObservationReading`
  - canonical `IWindReading`
  - `WeatherIngestStatus`

The mux is the *only* component that publishes the canonical UI stream, preventing UDP and REST from racing each other.

## Messages

### `TempestRestObservationsSnapshot` (new)
Purpose: preserve all REST fields (superset) and support persistence.

Notes:
- This is intended to be the event-relay published message type.
- The REST client method returns `TempestStationObservationsSnapshot`; a provider can map from that into this message.

### `WeatherIngestStatus` (new)
Purpose: surface active source + availability/freshness.

Suggested shape:
- `ActiveSource: Udp | Rest | None`
- `UdpAvailable: bool`
- `UdpLastReceivedUtc: DateTimeOffset?`
- `UdpIsFresh: bool`
- `RestAvailable: bool`
- `RestLastRetrievedUtc: DateTimeOffset?`
- `RestIsFresh: bool`
- `RestLastError: string?` (optional)

## Selection policy
Default mode: `Auto`.

- If UDP is fresh: active source = UDP.
- Else if REST is fresh: active source = REST.
- Else: active source = None.

### Freshness thresholds (settings-backed)
Defaults (tunable via settings):
- UDP stale cutoff: **90 seconds**.
- REST stale cutoff: **20 minutes**.

## Time zone consistency
- UDP reading timestamps are currently derived from epoch seconds and converted to device local time during transformation.
- REST observation timestamps should be converted consistently using the station `timezone_offset_minutes` approach already used for forecast; fall back to device local time if station offset is unavailable.

## Settings
Add new settings definitions (paths TBD, but grouped for discoverability):

### REST polling
- `/services/tempestObservations/refreshIntervalMinutes`
  - defaultValue: `15`
  - min: `5`

### Mux behavior
- `/services/weatherIngest/sourceMode`
  - defaultValue: `Auto`
  - allowableValues: `Auto`, `UdpOnly`, `RestOnly`
- `/services/weatherIngest/udpStaleSeconds`
  - defaultValue: `90`
- `/services/weatherIngest/restStaleMinutes`
  - defaultValue: `20`

## Persistence
- Persist raw REST JSON snapshots (initial).
- Add parsed columns later.

## UI indicator
- Add a small UI indicator bound to `WeatherIngestStatus`:
  - Active source (UDP/REST/NONE)
  - UDP availability/freshness
  - REST availability/freshness

## ToDo
- Decide UI/behavior when neither UDP nor REST is available (stale last-known display vs explicit offline state).
- Add persistence of UDP-derived values alongside REST snapshots to support comparison workflows.
- Add an on-demand refresh UI action that calls the REST provider refresh method.
