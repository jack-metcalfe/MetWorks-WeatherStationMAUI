# Tempest REST station metadata + derived observation metrics

This document describes the components added to support:

- Fetching Tempest/WeatherFlow **station metadata** over REST (Bearer token auth)
- Persisting a full **station snapshot** locally for offline use (RV scenario)
- Extracting key station fields (name, device name, lat/long, elevation)
- Publishing station metadata changes to the app via the **event relay**
- Computing **derived observation metrics** (dew point, wind chill, heat index, feels-like)
- Persisting station metadata updates to PostgreSQL

## Inputs

### Settings
The REST client uses settings stored in `data/settings.yaml` and accessed through `ISettingRepository`:

- `/services/tempest/apiKey`
  - Used as a **Bearer token** in the `Authorization` header
- `/services/tempest/stationId`
  - Station id for REST calls

## Key types

### Interfaces

- `ITempestRestClient` (`src/MetWorks_Interfaces/ITempestRestClient.cs`)
  - REST access point for station snapshot retrieval.
  - Method:
    - `Task<TempestStationSnapshot> GetStationSnapshotAsync(CancellationToken ct = default)`

- `IStationMetadataProvider` (`src/MetWorks_Interfaces/IStationMetadataProvider.cs`)
  - Extracts/caches station metadata (including elevation) from the station snapshot.
  - Methods:
    - `ValueTask<StationMetadata?> GetStationMetadataAsync(CancellationToken ct = default)`
    - `ValueTask<double?> GetStationElevationMetersAsync(CancellationToken ct = default)`

- `IStationMetadataPersister` (`src/MetWorks_Interfaces/IStationMetadataPersister.cs`)
  - Persists station metadata updates.
  - Method:
    - `Task PersistAsync(StationMetadata metadata, CancellationToken ct = default)`

### Records / DTOs

- `TempestStationSnapshot` (`src/MetWorks_Interfaces/ITempestRestClient.cs`)
  - `StationId` (best-effort parsed)
  - `RetrievedUtc`
  - `RawJson` (full response payload as JSON)

- `StationMetadata` (`src/MetWorks_Interfaces/IStationMetadataProvider.cs`)
  - `StationId`
  - `StationName` (`stations[0].name` or `public_name`)
  - `TempestDeviceName` (Tempest device `devices[*]` with `device_type == "ST"` → `device_meta.name`)
  - `Latitude` / `Longitude`
  - `ElevationMeters` (`stations[0].station_meta.elevation`)
  - `RetrievedUtc`

## Components

### `TempestRestClient`
- File: `src/MetWorks_Common/TempestRestClient.cs`
- Implements: `ITempestRestClient`

Responsibilities:
- Reads station id and api key from settings
- Performs REST call:
  - `GET https://swd.weatherflow.com/swd/rest/stations/{stationId}`
  - Uses `Authorization: Bearer <apiKey>`
- Returns a `TempestStationSnapshot` containing the **entire JSON** payload.

Design note:
- We intentionally avoid binding to a rigid DTO because the WeatherFlow payload is large and subject to change.

### `StationMetadataProvider`
- File: `src/MetWorks_Common/StationMetadataProvider.cs`
- Implements: `IStationMetadataProvider`

Responsibilities:
- Retrieves a station snapshot using `ITempestRestClient`
- Persists the **raw station JSON** in AppData:
  - filename: `tempest.station.snapshot.json`
- If REST fails, loads the last snapshot from disk (offline mode)
- Extracts a small strongly-typed `StationMetadata` from the snapshot
- Publishes station metadata updates via the event relay:
  - `IEventRelayBasic.Send(StationMetadata)`
  - Only when the computed `StationMetadata` differs from the previous value

Extraction paths (based on observed response shape):
- Station object: `stations[0]`
- Elevation: `stations[0].station_meta.elevation`
- Name: `stations[0].name` (fallback `public_name`)
- Coordinates: `stations[0].latitude`, `stations[0].longitude`
- Tempest device name: first `devices[*]` where `device_type == "ST"` → `device_meta.name`

### `DefaultPlatformPaths`
- File: `src/MetWorks_Common/DefaultPlatformPaths.cs`
- Implements: `IPlatformPaths`

Responsibilities:
- Provides `AppDataDirectory` for persistence.
- Uses MAUI `FileSystem.AppDataDirectory` when available; otherwise falls back to `Environment.SpecialFolder.LocalApplicationData`.

### `DerivedObservationCalculator`
- File: `src/MetWorks_Ingest_Transformer/DerivedObservationCalculator.cs`

Responsibilities:
- Pure calculation helpers for derived weather metrics:
  - dew point (Magnus formula)
  - wind chill (NOAA)
  - heat index (NOAA)
  - feels-like selection logic
  - sea-level pressure formula stub (requires station elevation)

Design note:
- This is intentionally pure and does not access services/settings.

### Derived fields on `IObservationReading`
- File: `src/MetWorks_Interfaces/IObservationReading.cs`

The derived fields already existed and are populated by the transformer:
- `Amount? DewPoint`
- `Amount? WindChill`
- `Amount? HeatIndex`
- `Amount? FeelsLike`
- `Amount? AtmosphericPressure` (sea-level; currently not populated until elevation is wired into the transformer)

### Enrichment point: `SensorReadingTransformer`
- File: `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs`

Responsibilities:
- Converts UDP readings into `ObservationReading`, applying preferred units
- Computes derived metrics during `ParseObservation(...)`:
  - dew point, wind chill, heat index, feels-like

Planned follow-up:
- Populate `AtmosphericPressure` using station elevation via an `IStationMetadataProvider` passed into `InitializeAsync`.

### `StationMetadataIngestor` (PostgreSQL)
- File: `src/MetWorks_Ingest_Postgres/StationMetadataIngestor.cs`
- Implements: `IStationMetadataPersister`

Responsibilities:
- Subscribes to `StationMetadata` messages on the event relay.
- On each update, inserts a row into Postgres.
- Auto-creates table and indexes if missing:
  - table: `public.station_metadata`

Table columns:
- `id` (text, COMB-guid)
- `application_received_utc_timestampz`
- `database_received_utc_timestampz`
- `station_id`
- `station_name`
- `tempest_device_name`
- `latitude`
- `longitude`
- `elevation_meters`
- `json_document_original` + generated jsonb
- `installation_id`

## Message flow

1. Tempest REST station snapshot
   - `TempestRestClient` fetches full JSON from WeatherFlow.

2. Station metadata extraction
   - `StationMetadataProvider` extracts `StationMetadata` from `stations[0]`.

3. Publish change
   - When metadata changes: provider sends `StationMetadata` through `IEventRelayBasic`.

4. Persist
   - `StationMetadataIngestor` receives `StationMetadata` and writes to Postgres.

5. Observation enrichment
   - `SensorReadingTransformer` enriches observation readings with derived values.

## Initialization / DI expectations

### Dependencies
- `StationMetadataProvider` depends on `ITempestRestClient`.
- `StationMetadataIngestor` depends on the event relay and settings/instance id; it does not require the provider directly.

### Ordering (DDI / InitializeAsync)
Recommended order:
1. `SettingProvider` → `SettingRepository`
2. `LoggerResilient` (+ event relay)
3. `TempestRestClient`
4. `StationMetadataProvider`
5. `StationMetadataIngestor`
6. `SensorReadingTransformer` (optionally pass metadata provider in future)

## Operational notes

- REST snapshot file location:
  - `Path.Combine(AppDataDirectory, "tempest.station.snapshot.json")`

- Offline behavior:
  - If REST fetch fails, provider reads the cached snapshot from disk.

- Security:
  - The WeatherFlow api key is treated as a secret setting.
  - Snapshot persistence currently stores the returned station payload verbatim. If WeatherFlow ever includes sensitive fields in that payload, consider redacting or encrypting the persisted snapshot.

## Suggested follow-ups

1. Wire `AtmosphericPressure` computation:
   - Pass `IStationMetadataProvider` into `SensorReadingTransformer.InitializeAsync(...)`
   - Use `DerivedObservationCalculator.TryComputeSeaLevelPressure(...)`

2. Reduce DB write frequency:
   - Add thresholding for coordinate drift
   - Or add a minimum time between persisted records

3. Persist the raw station JSON in Postgres as well (optional):
   - Either store `TempestStationSnapshot.RawJson`
   - Or store a curated subset
