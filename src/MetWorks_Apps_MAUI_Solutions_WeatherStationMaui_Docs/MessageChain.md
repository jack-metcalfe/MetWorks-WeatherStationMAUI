# Complete Message Chain Documentation

## Overview
This document details the key message flows in the WeatherStation MAUI app.

There are two distinct relays:

- `IEventRelayBasic` (typed messages) implemented by `MetWorks.EventRelay.EventRelayBasic`
  - API: `Register<TMessage>(recipient, handler)` / `Send<TMessage>(message)`
  - Implementation uses `CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger`
- `IEventRelayPath` (settings-change routing) implemented by `MetWorks.EventRelay.EventRelayPath`
  - API: `Register(pathPrefix, handler)` / `Send(ISettingValue settingValue)`
  - Routes by prefix match on `ISettingValue.Path`

---

## Correlation & identity fields (present today)

This system already contains the information needed to correlate messages and measure end-to-end timing without adding a new “message base interface”.

### The two key IDs

- **Message/record ID (`Id`)**
  - Used on `IRawPacketRecordTyped` and on each derived reading (`IObservationReading`, `IWindReading`, etc.).
  - Generated as a COMB GUID (chronologically sortable).

- **Upstream correlation (`SourcePacketId`)**
  - Used on derived readings to correlate them back to the originating UDP packet:
    - `IObservationReading.SourcePacketId`
    - `IWindReading.SourcePacketId`
    - `IPrecipitationReading.SourcePacketId`
    - `ILightningReading.SourcePacketId`
  - The value is the upstream `IRawPacketRecordTyped.Id`.

### Provenance (timing + lineage)

Derived readings also carry `Provenance` (`IReadingProvenance`) which includes:

- `RawPacketId` (same value as `SourcePacketId`)
- `UdpReceiptTime`
- `TransformStartTime`
- `TransformEndTime`
- `TransformerVersion` (e.g., `"1.0"` vs `"1.0-retransform"`)

### How to use this for instrumentation

- **Correlation across the pipeline**: use `SourcePacketId` / `Provenance.RawPacketId` to join derived readings back to the raw packet id.
- **Processing time (transform)**: `TransformEndTime - TransformStartTime`.
- **End-to-end (UDP → UI-ready)**: `TransformEndTime - UdpReceiptTime` (or `Timestamp` vs `UdpReceiptTime` depending on what “end-to-end” means).

Notes:

- `StationMetadata` is a separate flow and currently does not carry a `SourcePacketId` because it originates from REST/cache rather than UDP.

---

## MESSAGE FLOW CHAIN

### 1. `IRawPacketRecordTyped`

SENT BY:
- Component: `TempestPacketTransformer` (UDP Listener)
- Location: `src/MetWorks_Networking_Udp_Transformer/TempestPacketTransformer.cs`
- Method: `ProcessPacketAsync(...)`
- Code: `IEventRelayBasic.Send(iRawPacketRecordTyped);`

MESSAGE CONTAINS:
- Id - COMB GUID (chronologically sortable)
- PacketEnum - Type of packet (Observation, Wind, Precipitation, Lightning)
- JsonAsReadOnlyMemoryOfChar - Raw JSON from weather station
- ReceivedTime - UTC timestamp when packet arrived
- ReceivedUtcUnixEpochSecondsAsLong - Unix epoch seconds

RECEIVED BY:
- Component: `RawPacketIngestor` (PostgreSQL sink)
  - Location: `src/MetWorks_Ingest_Postgres/RawPacketIngestor.cs`
  - Registration: `IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);`

- Component: `RawPacketIngestor` (SQLite sink)
  - Location: `src/MetWorks_Ingest_SQLite/RawPacketIngestor.cs`
  - Registration: `IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);`

- Component: `SensorReadingTransformer` (transforms into typed readings)
  - Location: `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs`
  - Registration: `IEventRelayBasic.Register<IRawPacketRecordTyped>(this, OnRawPacketReceived);`

WHAT RECEIVER DOES:

`RawPacketIngestor` (PostgreSQL):
1. Buffers messages if DB is unavailable and buffering is enabled
2. Writes raw JSON into PostgreSQL tables (`observation`, `wind`, `lightning`, `precipitation`)

`RawPacketIngestor` (SQLite):
1. Skips writes when DB is unavailable
2. Writes raw JSON into SQLite tables based on `PacketEnum` (e.g., `observation`, `wind`, `lightning`, `precipitation`)

`SensorReadingTransformer`:
1. Caches the last packet per `PacketEnum` in `_lastPacketCache` for retransformation
2. Parses JSON via `TempestPacketParser`
3. Converts from Tempest metric units to user preferred units
4. Computes derived values (dew point, wind chill, heat index, feels-like; sea-level pressure when elevation is available)
5. Publishes typed readings via `IEventRelayBasic.Send(...)` (see below)

FREQUENCY: Every UDP packet received (~3 seconds for wind, ~60 seconds for observation)

---

### 2. `IObservationReading`

SENT BY:
- Component: `SensorReadingTransformer`
- Location: `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs`
- Method: `TransformAndPublish(...)`
- Code: `IEventRelayBasic.Send(iObservationReading);`

MESSAGE CONTAINS:
- Id - New COMB GUID for this reading
- SourcePacketId - Links to original IRawPacketRecordTyped.Id
- Timestamp - Weather station timestamp
- ReceivedUtc - System receipt time
- Temperature - Amount (value + unit) in user preferred units
- HumidityPercent - double (percentage)
- Pressure - Amount (value + unit) in user preferred units
- DewPoint - Amount? (optional)
- UvIndex - double
- SolarRadiation - double
- Provenance - Complete lineage:
  - RawPacketId - Original packet ID
  - UdpReceiptTime - When UDP arrived
  - TransformStartTime - When transformation started
  - TransformEndTime - When transformation ended
  - SourceUnits - "degree celsius, millibar"
  - TargetUnits - User preferred units
  - TransformerVersion - "1.0" or "1.0-retransform"

RECEIVED BY:
- Component: `WeatherReadingMux`
- Location: `src/MetWorks_Ingest_Transformer/WeatherReadingMux.cs`
- Registration: `IEventRelayBasic.Register<IObservationReading>(this, ...)`

WHAT RECEIVER DOES:
1. Caches latest UDP-derived observation
2. Selects the canonical active ingest source (UDP vs REST) based on freshness + settings
3. Publishes canonical UI readings as concrete `ObservationReading` / `WindReading`

FREQUENCY: ~Every 60 seconds (UDP observation packets), immediately on unit preference change (retransformation)

---

### 3. `IWindReading`

SENT BY:
- Component: `SensorReadingTransformer`
- Location: `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs`
- Method: `TransformAndPublish(...)`
- Code: `IEventRelayBasic.Send(iWindReading);`

MESSAGE CONTAINS:
- Id - New COMB GUID for this reading
- SourcePacketId - Links to original IRawPacketRecordTyped.Id
- Timestamp - Weather station timestamp
- ReceivedUtc - System receipt time
- Speed - Amount (value + unit) in user preferred units
- DirectionDegrees - double (0-360)
- DirectionCardinal - string ("N", "NNE", "NE", etc.)
- GustSpeed - Amount? (optional, not in rapid_wind packets)
- AverageSpeed - Amount? (optional)
- LullSpeed - Amount? (optional)
- Provenance - Complete lineage (same structure as Observation)

RECEIVED BY:
- Component: `WeatherReadingMux`
- Location: `src/MetWorks_Ingest_Transformer/WeatherReadingMux.cs`
- Registration: `IEventRelayBasic.Register<IWindReading>(this, ...)`

WHAT RECEIVER DOES:
1. Caches latest UDP-derived wind
2. Selects the canonical active ingest source (UDP vs REST) based on freshness + settings
3. Publishes canonical UI readings as concrete `ObservationReading` / `WindReading`

FREQUENCY: ~Every 3 seconds (UDP rapid_wind packets), immediately on unit preference change (retransformation)

---

### 4. `IPrecipitationReading`

SENT BY:
- Component: `SensorReadingTransformer`
- Location: `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs`
- Method: `TransformAndPublish(...)`
- Code: `IEventRelayBasic.Send(iPrecipitationReading);`

MESSAGE CONTAINS:
- Id - New COMB GUID for this reading
- SourcePacketId - Links to original IRawPacketRecordTyped.Id
- Timestamp - Weather station timestamp
- ReceivedUtc - System receipt time
- RainRate - Amount (currently 0 - event notification only)
- DailyAccumulation - Amount? (optional, get from observation)
- Provenance - Complete lineage

RECEIVED BY:
- Status: No listeners currently registered
- Future: Will be added to WeatherViewModel when precipitation UI is implemented

FREQUENCY: Only during rain events

---

### 5. `ILightningReading`

SENT BY:
- Component: `SensorReadingTransformer`
- Location: `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs`
- Method: `TransformAndPublish(...)`
- Code: `IEventRelayBasic.Send(iLightningReading);`

### 6. `StationMetadata` (station snapshot-derived)

SENT BY:
- Component: `StationMetadataProvider`
- Location: `src/MetWorks_Common/StationMetadataProvider.cs`
- Method: `GetStationMetadataAsync(...)`
- Code: `IEventRelayBasic.Send(_metadata);`

RECEIVED BY:
- Component: `StationMetadataIngestor` (PostgreSQL sink)
  - Location: `src/MetWorks_Ingest_Postgres/StationMetadataIngestor.cs`
  - Registration: `IEventRelayBasic.Register<StationMetadata>(this, md => StartBackground(ct => PersistAsync(md, ct)));`

- Component: `StationMetadataIngestor` (SQLite sink)
  - Location: `src/MetWorks_Ingest_SQLite/StationMetadataIngestor.cs`
  - Registration: `IEventRelayBasic.Register<StationMetadata>(this, md => StartBackground(ct => PersistAsync(md, ct)));`

---

### 7. `TempestForecast`

SENT BY:
- Component: `TempestForecastProvider`
- Location: `src/MetWorks_Common/TempestForecastProvider.cs`
- Code: `IEventRelayBasic.Send(_forecast);`

RECEIVED BY (CURRENT):
- Component: `ForecastHoursViewModel`
- Location: `src/MetWorks_Apps_MAUI_WeatherStationMaui/ViewModels/ForecastHoursViewModel.cs`
- Registration: `_iEventRelayBasic.Register<TempestForecast>(this, OnForecastReceived);`

Notes:
- `ForecastHoursViewModel` also performs an initial pull from `ITempestForecastProvider` to populate UI immediately.

---

## REST OBSERVATIONS + UDP/REST MUX (IMPLEMENTED)

This section describes the implemented message flow to support fetching observations from Tempest's REST API in addition to UDP.

Reference docs:
- `src/MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs/tempest-rest-observations/mini-spec.md`
- `src/MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs/tempest-rest-observations/implementation-plan.md`

### New REST snapshot type (client-level)

AVAILABLE VIA:
- Interface: `ITempestRestClient`
- File: `src/MetWorks_Interfaces/ITempestRestClient.cs`

METHOD:
- `GetStationObservationsAsync(CancellationToken ct = default)`

RETURNS:
- `TempestStationObservationsSnapshot`
  - `StationId`
  - `RetrievedUtc`
  - `RawJson`

Notes:
- The snapshot preserves the full JSON payload to avoid binding to a rigid external schema.
- This is a pull-based REST call; `TempestRestObservationsProvider` polls on an interval and publishes a message via `IEventRelayBasic`.

### `TempestRestObservationsSnapshot` (event relay)

MESSAGE:
- `TempestRestObservationsSnapshot`
  - File: `src/MetWorks_Interfaces/TempestRestObservationsSnapshot.cs`
  - Fields: `StationId`, `RetrievedUtc`, `RawJson`

Notes:
- This is the superset message published via `IEventRelayBasic` by `TempestRestObservationsProvider`.

SENT BY:
- Component: `TempestRestObservationsProvider`
- File: `src/MetWorks_Common/TempestRestObservationsProvider.cs`

RECEIVED BY:
- Component: `WeatherReadingMux`
- File: `src/MetWorks_Ingest_Transformer/WeatherReadingMux.cs`
- Registration: `IEventRelayBasic.Register<TempestRestObservationsSnapshot>(this, OnRestSnapshotReceived)`

### `WeatherIngestStatus`

Status message used to surface ingest health in the UI:
- Which source is currently active for UI display (UDP vs REST)
- Whether UDP and/or REST are available/fresh
- What happens when neither is available

Type:
- `WeatherIngestStatus`
  - File: `src/MetWorks_Interfaces/WeatherIngestStatus.cs`

SENT BY:
- Component: `WeatherReadingMux`
- File: `src/MetWorks_Ingest_Transformer/WeatherReadingMux.cs`

RECEIVED BY:
- Component: `WeatherViewModel`
- File: `src/MetWorks_Apps_MAUI_WeatherStationMaui/ViewModels/WeatherViewModel.cs`
- Registration: `_iEventRelayBasic.Register<WeatherIngestStatus>(this, OnWeatherIngestStatusReceived);`

### Canonical UI stream via mux

Goal: publish exactly one canonical UI stream of concrete readings:
- `ObservationReading`
- `WindReading`

...selected by a mux so UDP and REST do not race each other and cause UI thrash.

SENT BY:
- Component: `WeatherReadingMux`
- File: `src/MetWorks_Ingest_Transformer/WeatherReadingMux.cs`

RECEIVED BY (CURRENT):
- Component: `WeatherViewModel`
  - File: `src/MetWorks_Apps_MAUI_WeatherStationMaui/ViewModels/WeatherViewModel.cs`
  - Registrations:
    - `_iEventRelayBasic.Register<ObservationReading>(this, OnObservationReceived);`
    - `_iEventRelayBasic.Register<WindReading>(this, OnWindReceived);`

- Component: `HistoricalObservationsViewModel`
  - File: `src/MetWorks_Apps_MAUI_WeatherStationMaui/ViewModels/HistoricalObservationsViewModel.cs`
  - Registration: `_iEventRelayBasic.Register<ObservationReading>(this, ...)`

### REST snapshot -> UI readings mapping (IMPLEMENTED)

IMPLEMENTED MAPPER:
- `TempestRestReadingsMapper`
  - File: `src/MetWorks_Ingest_Transformer/TempestRestReadingsMapper.cs`
  - Input: `TempestRestObservationsSnapshot`
  - Output: `ObservationReading` + `WindReading` (best-effort)

Provenance strategy (REST-derived):
- `ReadingProvenance.RawPacketId = Guid.Empty`
- `IWeatherReading.SourcePacketId = Guid.Empty`
- `ReadingProvenance.TransformerVersion = "rest-1.0"`

---

## WEBSOCKET OBSERVATIONS + UDP/REST/WS MUX (IMPLEMENTED)

This section describes the end-to-end runtime chain required to get Tempest WebSockets delivering observation/wind readings into the canonical UI stream.

Reference docs:
- `src/MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs/tempest-websocket-observations/implementation-plan.md`

### Prerequisites (configuration)

1. Tempest developer application exists and provides a `client_id`.
2. Tempest application has an OAuth redirect callback URL registered (custom scheme).
   - Example: `metworks-weatherstation://oauth2redirect`
3. App settings are configured (via `settings.yaml` + overrides) with:
   - `/services/tempest/oauth/clientId`
   - `/services/tempest/oauth/redirectUri`
   - `/services/tempest/websocket/enabled` (must be `true`)
   - `/services/tempest/websocket/deviceId` (optional; falls back to station metadata `TempestDeviceId`)

### 1) DDI startup wires and starts the long-running services

SENT BY:
- Host: MAUI app startup calls DDI `Registry.InitializeAllAsync()`.

EFFECT:
- DDI constructs and initializes:
  - `TempestOAuthTokenProvider` (implements `ITempestOAuthTokenProvider`)
  - `TempestWebSocketObservationsProvider` (implements `ITempestWebSocketObservationsProvider`)
  - `WeatherReadingMux`

Notes:
- `TempestWebSocketObservationsProvider.InitializeAsync(...)` calls `StartBackground(RunAsync)`.
- The WS provider does not block startup; it runs a background loop that waits for prerequisites.

### 2) OAuth token is acquired (interactive once, then cached)

AVAILABLE VIA:
- Interface: `ITempestOAuthTokenProvider`
- File: `src/MetWorks_Interfaces/ITempestOAuthTokenProvider.cs`

IMPLEMENTATION:
- `TempestOAuthTokenProvider`
  - File: `src/MetWorks_Maui_Services/TempestOAuthTokenProvider.cs`

CHAIN:
1. Some UI/action path calls `GetAccessTokenAsync(allowInteractive: true, ct)` at least once.
2. `TempestOAuthTokenProvider` uses MAUI `WebAuthenticator`.
3. Browser redirects back to the app via the registered callback URL.
4. The provider exchanges the auth code for an `access_token`.
5. Token material is persisted in MAUI `SecureStorage`.

Notes:
- The WS provider itself calls `GetAccessTokenAsync(allowInteractive: false, ct)` (it will wait/retry if the token is not present yet).

### 3) `TempestWebSocketObservationsProvider` connects and subscribes

SENT BY:
- Component: `TempestWebSocketObservationsProvider`
- File: `src/MetWorks_Common/TempestWebSocketObservationsProvider.cs`

CHAIN (background loop):
1. Load `/services/tempest/websocket/enabled`.
2. Retrieve OAuth `access_token` from `ITempestOAuthTokenProvider` (non-interactive).
3. Resolve `device_id`:
   - Use `/services/tempest/websocket/deviceId` if set.
   - Else use `StationMetadata.TempestDeviceId` from `IStationMetadataProvider`.
4. Connect `ClientWebSocket` to `wss://ws.weatherflow.com/swd/data?token=<access_token>`.
5. Send subscription messages:
   - `listen_start` (observation stream, e.g. `obs_st`)
   - `listen_rapid_start` (rapid wind stream, e.g. `rapid_wind`)

### 4) WebSocket messages are published as raw snapshots

MESSAGE:
- `TempestWebSocketObservationsSnapshot`
  - File: `src/MetWorks_Interfaces/TempestWebSocketObservationsSnapshot.cs`
  - Fields: `DeviceId`, `ReceivedUtc`, `MessageType`, `RawJson`

SENT BY:
- Component: `TempestWebSocketObservationsProvider`
- Code: `IEventRelayBasic.Send(snapshot);`

FREQUENCY (typical):
- `rapid_wind`: ~every 3 seconds
- `obs_st`: ~every 60 seconds

### 5) `WeatherReadingMux` maps snapshots into UI-compatible readings

RECEIVED BY:
- Component: `WeatherReadingMux`
- File: `src/MetWorks_Ingest_Transformer/WeatherReadingMux.cs`
- Registration: `IEventRelayBasic.Register<TempestWebSocketObservationsSnapshot>(this, OnWebSocketSnapshotReceived)`

MAPPING:
- Mapper: `TempestWebSocketReadingsMapper`
  - File: `src/MetWorks_Ingest_Transformer/TempestWebSocketReadingsMapper.cs`
  - Input: `TempestWebSocketObservationsSnapshot`
  - Output (best-effort): `ObservationReading` and/or `WindReading`

Provenance strategy (WS-derived):
- `ReadingProvenance.RawPacketId = Guid.Empty`
- `IWeatherReading.SourcePacketId = Guid.Empty`
- `ReadingProvenance.TransformerVersion = "ws-1.0"`

### 6) The mux selects the active source and publishes a single canonical UI stream

EFFECT:
- `WeatherReadingMux` evaluates freshness and chooses `WeatherIngestSource`:
  - Prefer UDP when fresh.
  - Else prefer WebSocket when fresh.
  - Else fall back to REST when fresh.

SENT BY:
- Component: `WeatherReadingMux`
- Messages:
  - `ObservationReading` (concrete)
  - `WindReading` (concrete)
  - `WeatherIngestStatus` (includes WS freshness fields)

RECEIVED BY (CURRENT):
- `WeatherViewModel`
- `HistoricalObservationsViewModel` (observation)

Notes:
- Status is ticked on a timer (`StatusTickInterval`) and also emitted when new UDP/WS/REST data arrives.

### `WeatherIngestWarmStartRequest`

Purpose: allow late subscribers (UI) to request that the mux re-publish cached canonical readings/status.

SENT BY:
- Component: `WeatherViewModel`
- File: `src/MetWorks_Apps_MAUI_WeatherStationMaui/ViewModels/WeatherViewModel.cs`
- Code: `_iEventRelayBasic.Send(new WeatherIngestWarmStartRequest());`

RECEIVED BY:
- Component: `WeatherReadingMux`
- File: `src/MetWorks_Ingest_Transformer/WeatherReadingMux.cs`
- Registration: `IEventRelayBasic.Register<WeatherIngestWarmStartRequest>(this, OnWarmStartRequested)`

## SETTINGS CHANGE NOTIFICATIONS (`IEventRelayPath`)

Settings changes are routed by prefix match on `ISettingValue.Path`. This is **separate** from the typed message pipeline above.

PUBLISHED BY:
- Component: `SettingRepository`
- Location: `src/MetWorks_Common_Settings/SettingRepository.cs`
- Method: `ApplyOverrides(IEnumerable<ISettingValue> overrides)`
- Code: `IEventRelayPath.Send(c);`

SUBSCRIBED BY (CURRENT):
- Component: `SensorReadingTransformer`
- Location: `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs`
- Method: `InitializeAsync(...)`
- Registration:
  - `var unitGroupPrefix = LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildGroupPath();`
  - `IEventRelayPath.Register(unitGroupPrefix, OnUnitSettingChanged);`

- Component: `WeatherReadingMux`
  - Location: `src/MetWorks_Ingest_Transformer/WeatherReadingMux.cs`
  - Registrations:
    - `IEventRelayPath.Register(LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildGroupPath(), OnWeatherIngestSettingsChanged)`
    - `IEventRelayPath.Register(LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildGroupPath(), OnUnitOfMeasureSettingsChanged)`

INTENDED EFFECT:
- When unit-of-measure settings change under that prefix, the transformer reloads preferences and retransforms cached packets so the UI updates in the new units.

- When weather ingest settings change, the mux re-evaluates which source is active.
- When unit-of-measure settings change, the mux remaps the latest REST snapshot so cached REST readings are refreshed in the new preferred units.

MESSAGE CONTAINS:
- `ISettingValue`
  - `Path`
  - `Value`

---

## COMPLETE FLOW DIAGRAM

Weather Station (UDP Broadcast)
    ↓ Raw UDP packet (JSON)
1. Transformer (UDP Listener)
    - Receives UDP packet
    - Assigns COMB GUID
    - Calls ProvenanceTracker.TrackNewPacket()
    - SENDS: IRawPacketRecordTyped
    ↓ IEventRelayBasic.Send(IRawPacketRecordTyped)
2. SensorReadingTransformer
    RECEIVES: IRawPacketRecordTyped
    - Caches packet
    - Parses JSON via TempestPacketParser
    - Converts units (metric to user preference)
    - Creates typed reading with provenance
    - Calls ProvenanceTracker.LinkTransformedReading()
    - SENDS: IObservationReading OR IWindReading OR IPrecipitationReading OR ILightningReading
    ↓ IEventRelayBasic.Send(specific type)
3. WeatherReadingMux
    RECEIVES: IObservationReading, IWindReading, TempestRestObservationsSnapshot, TempestWebSocketObservationsSnapshot
    - Chooses canonical source (UDP vs WebSocket vs REST)
    - SENDS: ObservationReading, WindReading, WeatherIngestStatus
    ↓ IEventRelayBasic.Send(concrete reading / status)
4. WeatherViewModel
    RECEIVES: ObservationReading, WindReading, WeatherIngestStatus
    - Updates bound properties

3c. (Future) WeatherViewModel
    RECEIVES: IPrecipitationReading
    - Will update precipitation UI

3d. (Future) WeatherViewModel
    RECEIVES: ILightningReading
    - Will update lightning UI

---

## MESSAGE STATISTICS

MESSAGE TYPE | SENDER | RECEIVER(S) | FREQUENCY | ACTIVE LISTENERS
IRawPacketRecordTyped | UDP Transformer | RawPacketIngestor (Postgres + SQLite), SensorReadingTransformer | Every UDP packet | 3
IObservationReading | SensorReadingTransformer | WeatherReadingMux | ~60 seconds | 1
IWindReading | SensorReadingTransformer | WeatherReadingMux | ~3 seconds | 1
TempestRestObservationsSnapshot | TempestRestObservationsProvider | WeatherReadingMux | On REST refresh | 1
TempestWebSocketObservationsSnapshot | TempestWebSocketObservationsProvider | WeatherReadingMux | rapid_wind (~3s) + obs_st (~60s) | 1
TempestForecast | TempestForecastProvider | ForecastHoursViewModel | On forecast refresh | 1
ObservationReading | WeatherReadingMux | WeatherViewModel, HistoricalObservationsViewModel | Depends on active source | 2
WindReading | WeatherReadingMux | WeatherViewModel | Depends on active source | 1
WeatherIngestStatus | WeatherReadingMux | WeatherViewModel | Tick + change-driven | 1
WeatherIngestWarmStartRequest | WeatherViewModel | WeatherReadingMux | On viewmodel init | 1
IPrecipitationReading | SensorReadingTransformer | (none) | Rain events | 0
ILightningReading | SensorReadingTransformer | (none) | Lightning strikes | 0

---

## SPECIAL CASES

RETRANSFORMATION FLOW:

When user changes unit preferences:

1. User changes setting → SettingsRepository.ApplyOverrides()
2. SettingsRepository fires event → OnUnitSettingChanged()
3. SensorReadingTransformer receives event
4. LoadUnitPreferences() → updates _preferredXXXUnit fields
5. RetransformCachedPackets() → processes _lastPacketCache
6. For each cached packet:
   - TransformAndPublish(packet, isRetransformation: true)
   - NEW COMB GUID assigned (different from original)
   - SAME SourcePacketId (links back to original)
   - Provenance.TransformerVersion = "1.0-retransform"
   - IEventRelayBasic.Send(reading) → mux receives and re-publishes canonical UI readings

MOCK SERVICE FLOW (Development Only):

When MockWeatherReadingService is running (#if DEBUG):

1. Timer fires every 2 seconds
2. MockWeatherReadingService.CreateMockObservationReading()
3. IEventRelayBasic.Send(mockObservation)
4. WeatherViewModel.OnObservationReceived() → UI updates
5. MockWeatherReadingService.CreateMockWindReading()
6. IEventRelayBasic.Send(mockWind)
7. WeatherViewModel.OnWindReceived() → UI updates

Note: Mock and real services can coexist—both publish to same relay.

---

## KEY DESIGN PRINCIPLES

1. Exact Type Matching: Event relay requires exact type match (not base/derived)
2. Single Responsibility: Each component does one thing:
   - UDP Transformer: Receive + COMB GUID
   - SensorReadingTransformer: Parse + Convert + Publish
   - WeatherReadingMux: Select canonical source + re-publish concrete readings
   - WeatherViewModel: Update UI
3. Provenance Throughout: Every reading carries complete lineage
4. Thread Safety: UI updates marshaled to main thread
5. Retransformation: Cached packets allow immediate UI feedback on settings changes

---

## LISTENER REGISTRATION SUMMARY

ACTIVE REGISTRATIONS:

In `SensorReadingTransformer.InitializeAsync()`:
- `IEventRelayBasic.Register<IRawPacketRecordTyped>(this, OnRawPacketReceived);`
- `IEventRelayPath.Register(UnitOfMeasure group prefix, OnUnitSettingChanged);`

In `WeatherReadingMux.InitializeAsync()`:
- `IEventRelayBasic.Register<IObservationReading>(this, OnUdpObservationReceived);`
- `IEventRelayBasic.Register<IWindReading>(this, OnUdpWindReceived);`
- `IEventRelayBasic.Register<TempestWebSocketObservationsSnapshot>(this, OnWebSocketSnapshotReceived);`
- `IEventRelayBasic.Register<TempestRestObservationsSnapshot>(this, OnRestSnapshotReceived);`
- `IEventRelayBasic.Register<WeatherIngestWarmStartRequest>(this, OnWarmStartRequested);`
- `IEventRelayBasic.Register<TempestRestObservationsSnapshot>(this, OnRestSnapshotReceived);`
- `IEventRelayBasic.Register<WeatherIngestWarmStartRequest>(this, OnWarmStartRequested);`
- `IEventRelayPath.Register(WeatherIngest group prefix, OnWeatherIngestSettingsChanged);`
- `IEventRelayPath.Register(UnitOfMeasure group prefix, OnUnitOfMeasureSettingsChanged);`

In `RawPacketIngestor.InitializeAsync()`:
- `IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);` (Postgres sink)
- `IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);` (SQLite sink)

In `WeatherViewModel.InitializeAsync()`:
- `_iEventRelayBasic.Register<ObservationReading>(this, OnObservationReceived);`
- `_iEventRelayBasic.Register<WindReading>(this, OnWindReceived);`
- `_iEventRelayBasic.Register<WeatherIngestStatus>(this, OnWeatherIngestStatusReceived);`
- `_iEventRelayBasic.Send(new WeatherIngestWarmStartRequest());`

In `ForecastHoursViewModel`:
- `_iEventRelayBasic.Register<TempestForecast>(this, OnForecastReceived);`

In `HistoricalObservationsViewModel`:
- `_iEventRelayBasic.Register<ObservationReading>(this, ...);`

---

Last Updated: January 7, 2026
Status: Complete and working
Version: 1.0