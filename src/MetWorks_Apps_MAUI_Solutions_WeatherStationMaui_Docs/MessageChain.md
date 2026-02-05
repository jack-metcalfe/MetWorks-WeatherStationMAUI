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

- Component: `SensorReadingTransformer` (transforms into typed readings)
  - Location: `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs`
  - Registration: `IEventRelayBasic.Register<IRawPacketRecordTyped>(this, OnRawPacketReceived);`

WHAT RECEIVER DOES:

`RawPacketIngestor`:
1. Buffers messages if DB is unavailable and buffering is enabled
2. Writes raw JSON into Postgres tables (`observation`, `wind`, `lightning`, `precipitation`)

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
- Component: `WeatherViewModel`
- Location: `src/MetWorks_Apps_MAUI_WeatherStationMaui/ViewModels/WeatherViewModel.cs`
- Registration: `_iEventRelayBasic.Register<IObservationReading>(this, OnObservationReceived);`

WHAT RECEIVER DOES:
1. Marshals to main thread (MainThread.BeginInvokeOnMainThread)
2. Updates CurrentObservation property
3. Triggers PropertyChanged for: CurrentObservation, TemperatureDisplay, PressureDisplay, HumidityDisplay
4. UI automatically updates via data binding

FREQUENCY: ~Every 60 seconds (from weather station), Immediately on unit preference change (retransformation)

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
- Component: `WeatherViewModel`
- Location: `src/MetWorks_Apps_MAUI_WeatherStationMaui/ViewModels/WeatherViewModel.cs`
- Registration: `_iEventRelayBasic.Register<IWindReading>(this, OnWindReceived);`

WHAT RECEIVER DOES:
1. Marshals to main thread
2. Updates CurrentWind property
3. Triggers PropertyChanged for: CurrentWind, WindSpeedDisplay, WindDirectionDisplay, WindGustDisplay
4. UI automatically updates via data binding

FREQUENCY: ~Every 3 seconds (rapid_wind packets from weather station), Immediately on unit preference change (retransformation)

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

INTENDED EFFECT:
- When unit-of-measure settings change under that prefix, the transformer reloads preferences and retransforms cached packets so the UI updates in the new units.

MESSAGE CONTAINS:
- Id - New COMB GUID for this reading
- SourcePacketId - Links to original IRawPacketRecordTyped.Id
- Timestamp - Weather station timestamp
- ReceivedUtc - System receipt time
- StrikeDistance - Amount (value + unit) in user preferred units
- StrikeCount - int (1 per event)
- Provenance - Complete lineage

RECEIVED BY:
- Status: No listeners currently registered
- Future: Will be added to WeatherViewModel when lightning UI is implemented

FREQUENCY: Only during lightning strikes

---

## COMPLETE FLOW DIAGRAM

Weather Station (UDP Broadcast)
    ↓ Raw UDP packet (JSON)
1. Transformer (UDP Listener)
    - Receives UDP packet
    - Assigns COMB GUID
    - Calls ProvenanceTracker.TrackNewPacket()
    - SENDS: IRawPacketRecordTyped
    ↓ ISingletonEventRelay.Send(IRawPacketRecordTyped)
2. WeatherDataTransformer
    RECEIVES: IRawPacketRecordTyped
    - Caches packet
    - Parses JSON via TempestPacketParser
    - Converts units (metric to user preference)
    - Creates typed reading with provenance
    - Calls ProvenanceTracker.LinkTransformedReading()
    - SENDS: IObservationReading OR IWindReading OR IPrecipitationReading OR ILightningReading
    ↓ ISingletonEventRelay.Send(specific type)
3a. WeatherViewModel
    RECEIVES: IObservationReading
    - Updates CurrentObservation property
    - Triggers PropertyChanged
    - UI updates: Temperature, Pressure, Humidity

3b. WeatherViewModel
    RECEIVES: IWindReading
    - Updates CurrentWind property
    - Triggers PropertyChanged
    - UI updates: Wind Speed, Direction, Gusts

3c. (Future) WeatherViewModel
    RECEIVES: IPrecipitationReading
    - Will update precipitation UI

3d. (Future) WeatherViewModel
    RECEIVES: ILightningReading
    - Will update lightning UI

---

## MESSAGE STATISTICS

MESSAGE TYPE | SENDER | RECEIVER(S) | FREQUENCY | ACTIVE LISTENERS
IRawPacketRecordTyped | UDP Transformer | WeatherDataTransformer | Every UDP packet | 1
IObservationReading | WeatherDataTransformer | WeatherViewModel | ~60 seconds | 1
IWindReading | WeatherDataTransformer | WeatherViewModel | ~3 seconds | 1
IPrecipitationReading | WeatherDataTransformer | (none) | Rain events | 0
ILightningReading | WeatherDataTransformer | (none) | Lightning strikes | 0

---

## SPECIAL CASES

RETRANSFORMATION FLOW:

When user changes unit preferences:

1. User changes setting → SettingsRepository.ApplyOverrides()
2. SettingsRepository fires event → OnUnitSettingChanged()
3. WeatherDataTransformer receives event
4. LoadUnitPreferences() → updates _preferredXXXUnit fields
5. RetransformCachedPackets() → processes _lastPacketCache
6. For each cached packet:
   - TransformAndPublish(packet, isRetransformation: true)
   - NEW COMB GUID assigned (different from original)
   - SAME SourcePacketId (links back to original)
   - Provenance.TransformerVersion = "1.0-retransform"
   - ISingletonEventRelay.Send(reading) → UI updates immediately

MOCK SERVICE FLOW (Development Only):

When MockWeatherReadingService is running (#if DEBUG):

1. Timer fires every 2 seconds
2. MockWeatherReadingService.CreateMockObservationReading()
3. ISingletonEventRelay.Send(mockObservation)
4. WeatherViewModel.OnObservationReceived() → UI updates
5. MockWeatherReadingService.CreateMockWindReading()
6. ISingletonEventRelay.Send(mockWind)
7. WeatherViewModel.OnWindReceived() → UI updates

Note: Mock and real services can coexist—both publish to same relay.

---

## KEY DESIGN PRINCIPLES

1. Exact Type Matching: Event relay requires exact type match (not base/derived)
2. Single Responsibility: Each component does one thing:
   - UDP Transformer: Receive + COMB GUID
   - WeatherDataTransformer: Parse + Convert + Publish
   - WeatherViewModel: Update UI
3. Provenance Throughout: Every reading carries complete lineage
4. Thread Safety: UI updates marshaled to main thread
5. Retransformation: Cached packets allow immediate UI feedback on settings changes

---

## LISTENER REGISTRATION SUMMARY

ACTIVE REGISTRATIONS:

In WeatherDataTransformer.InitializeAsync():
ISingletonEventRelay.Register<IRawPacketRecordTyped>(this, OnRawPacketReceived);

In WeatherViewModel constructor:
ISingletonEventRelay.Register<IObservationReading>(this, OnObservationReceived);
ISingletonEventRelay.Register<IWindReading>(this, OnWindReceived);

FUTURE REGISTRATIONS (Planned):

In WeatherViewModel (when precipitation UI is added):
ISingletonEventRelay.Register<IPrecipitationReading>(this, OnPrecipitationReceived);

In WeatherViewModel (when lightning UI is added):
ISingletonEventRelay.Register<ILightningReading>(this, OnLightningReceived);

---

Last Updated: January 7, 2026
Status: Complete and working
Version: 1.0