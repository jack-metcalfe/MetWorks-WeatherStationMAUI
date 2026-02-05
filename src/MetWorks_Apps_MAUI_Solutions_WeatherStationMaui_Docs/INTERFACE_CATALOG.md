# Interface catalog (inventory + usage)

This repo uses interfaces heavily for two primary reasons:

1. **Dependency injection boundaries** (DDI + MAUI DI)
2. **Event-driven message contracts** (`IEventRelayBasic`)

This document inventories the interfaces currently defined in the solution and summarizes where they are used. It is intentionally descriptive (not prescriptive) so we can make instrumentation and refactor decisions grounded in reality.

> Scope: interfaces defined under `src/MetWorks_Interfaces` **plus** the small set of MAUI UI-selection interfaces under `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection`.

Note: the ingestion/parsing layer (`src/MetWorks_IoT_UDP_Tempest`) also defines several DTO/factory interfaces. Those are included below because they directly add to interface count and are central to the data pipeline.

---

## Messaging

### `IEventRelayBasic`
- Defined: `src/MetWorks_Interfaces/IEventRelayBasic.cs`
- Implemented by: `src/MetWorks_EventRelay/EventRelayBasic.cs`
- Used by:
  - UDP input: `src/MetWorks_Networking_Udp_Transformer/TempestPacketTransformer.cs` (publishes `IRawPacketRecordTyped`)
  - Transformer: `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs` (subscribes to `IRawPacketRecordTyped`, publishes typed readings)
  - UI: `src/MetWorks_Apps_MAUI_WeatherStationMaui/ViewModels/WeatherViewModel.cs` (subscribes to `IObservationReading`, `IWindReading`)
  - Postgres sinks:
    - `src/MetWorks_Ingest_Postgres/RawPacketIngestor.cs` (subscribes to `IRawPacketRecordTyped`)
    - `src/MetWorks_Ingest_Postgres/StationMetadataIngestor.cs` (subscribes to `StationMetadata`)
  - Station metadata:
    - `src/MetWorks_Common/StationMetadataProvider.cs` (publishes `StationMetadata`)

### `IEventRelayPath`
- Defined: `src/MetWorks_Interfaces/IEventRelayPath.cs`
- Implemented by: `src/MetWorks_EventRelay/EventRelayPath.cs`
- Purpose: settings-change notifications by prefix match on `ISettingValue.Path`
- Used by:
  - Publisher: `src/MetWorks_Common_Settings/SettingRepository.cs` (`ApplyOverrides` publishes `ISettingValue`)
  - Subscriber (current): `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs`
    - `IEventRelayPath.Register(unitGroupPrefix, OnUnitSettingChanged)`

---

## Settings

### `ISettingRepository`
- Defined: `src/MetWorks_Interfaces/ISettingRepository.cs`
- Implemented by: `src/MetWorks_Common_Settings/SettingRepository.cs`
- Used by: most services (via `ServiceBase.InitializeBase(...)`), plus direct reads in:
  - `src/MetWorks_Common/TempestRestClient.cs` (Tempest REST settings)
  - `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs` (unit preferences)
  - `src/MetWorks_Ingest_Postgres/*` (connection strings, buffering)

### `ISettingProvider`
- Defined: `src/MetWorks_Interfaces/ISettingProvider.cs`
- Implemented by: `src/MetWorks_Common_Settings/SettingProvider.cs`
- Used by:
  - `SettingRepository.InitializeAsync(...)` (takes `ISettingProvider`)

### `ISettingDefinition`
- Defined: `src/MetWorks_Interfaces/ISettingDefinition.cs`
- Used by:
  - `SettingProvider` and `SettingRepository` internal dictionaries

### `ISettingValue`
- Defined: `src/MetWorks_Interfaces/ISettingValue.cs`
- Used by:
  - `SettingRepository.ApplyOverrides(...)`
  - `EventRelayPath` routing
  - `SensorReadingTransformer` unit-change handler

---

## Logging

### `ILoggerStub`
- Defined: `src/MetWorks_Interfaces/ILoggerStub.cs`
- Implemented by: `src/MetWorks_Common_Logging/LoggerStub.cs`
- Used by:
  - `SettingProvider.InitializeAsync(...)`
  - `SettingRepository.InitializeAsync(...)`
  - `InstanceIdentifier.InitializeAsync(...)`
  - `LoggerFile/LoggerPostgreSQL/LoggerResilient` initialization (as part of the logger stack)

### `ILoggerFile`
- Defined: `src/MetWorks_Interfaces/ILoggerFile.cs`
- Implemented by: `src/MetWorks_Common_Logging/LoggerFile.cs`

### `ILoggerPostgreSQL`
- Defined: `src/MetWorks_Interfaces/ILoggerPostgreSQL.cs`
- Implemented by: `src/MetWorks_Common_Logging/LoggerPostgreSQL.cs`

### `ILoggerResilient`
- Defined: `src/MetWorks_Interfaces/ILoggerResilient.cs`
- Implemented by: `src/MetWorks_Common_Logging/LoggerResilient.cs`
- Used by: most long-running services and startup initialization

### `ILogger`
- Defined: `src/MetWorks_Interfaces/ILogger.cs`
- Used by:
  - internal logging in service base patterns
  - component-level logging where a resilient logger is not required

---

## Readings (EventRelayBasic message contracts)

### `IReading`
- Defined: `src/MetWorks_Interfaces/IReading.cs`
- Used as a common base for reading messages.

### `IWeatherReading`
- Defined: `src/MetWorks_Interfaces/IWeatherReading.cs`
- Used by: `IObservationReading`, `IWindReading`, `IPrecipitationReading`, `ILightningReading`

### `IObservationReading`
- Defined: `src/MetWorks_Interfaces/IObservationReading.cs`
- Produced by: `SensorReadingTransformer` (`ObservationReading` model)
- Consumed by: `WeatherViewModel`

### `IWindReading`
- Defined: `src/MetWorks_Interfaces/IWindReading.cs`
- Produced by: `SensorReadingTransformer` (`WindReading` model)
- Consumed by: `WeatherViewModel`

### `IPrecipitationReading`
- Defined: `src/MetWorks_Interfaces/IPrecipitationReading.cs`
- Produced by: `SensorReadingTransformer` (`PrecipitationReading` model)
- Consumed by: (none currently)

### `ILightningReading`
- Defined: `src/MetWorks_Interfaces/ILightningReading.cs`
- Produced by: `SensorReadingTransformer` (`LightningReading` model)
- Consumed by: (none currently)

### `IRawPacketRecordTyped`
- Defined: `src/MetWorks_Interfaces/IRawPacketRecordTyped.cs`
- Produced by:
  - `src/MetWorks_IoT_UDP_Tempest/RawPacketRecordTypedFactory.cs`
  - published by `src/MetWorks_Networking_Udp_Transformer/TempestPacketTransformer.cs`
- Consumed by:
  - `src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs`
  - `src/MetWorks_Ingest_Postgres/RawPacketIngestor.cs`

### `IReadingProvenance`
- Defined: `src/MetWorks_Interfaces/IReadingProvenance.cs`
- Used as a payload on readings to capture timing/lineage.

---

## Station metadata

### `IStationMetadataProvider`
- Defined: `src/MetWorks_Interfaces/IStationMetadataProvider.cs`
- Implemented by: `src/MetWorks_Common/StationMetadataProvider.cs`
- Used by:
  - `SensorReadingTransformer` (for station elevation)

### `IStationMetadataPersister`
- Defined: `src/MetWorks_Interfaces/IStationMetadataPersister.cs`
- Implemented by: `src/MetWorks_Ingest_Postgres/StationMetadataIngestor.cs`

### `ITempestRestClient`
- Defined: `src/MetWorks_Interfaces/ITempestRestClient.cs`
- Implemented by: `src/MetWorks_Common/TempestRestClient.cs`
- Used by:
  - `StationMetadataProvider`

### `IPlatformPaths`
- Defined: `src/MetWorks_Interfaces/IPlatformPaths.cs`
- Implemented by: `src/MetWorks_Common/DefaultPlatformPaths.cs`
- Used by:
  - `StationMetadataProvider` (snapshot persistence)

---

## Service lifecycle

### `IServiceReady`
- Defined: `src/MetWorks_Interfaces/IServiceReady.cs`
- Implemented by:
  - `SettingRepository`
  - (other services may implement indirectly via `ServiceBase` patterns)
- Used by:
  - components that need an explicit readiness awaitable

---

## MAUI device selection (UI composition)

These interfaces are UI-selection specific and are not part of the UDP/data pipeline.

### `IContentViewFactory`
- Defined: `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/IContentViewFactory.cs`
- Implemented by: `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/ContentViewFactory.cs`
- Consumed by: `src/MetWorks_Apps_MAUI_WeatherStationMaui/Pages/HostPages/MainSwipeHostPage.xaml.cs`

### `IContentVariantCatalog`
- Defined: `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/IContentVariantCatalog.cs`
- Implemented by: `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/ContentVariantCatalog.cs`
- Consumed by: `ContentViewFactory`

### `IHostCompositionCatalog`
- Defined: `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/IHostCompositionCatalog.cs`
- Implemented by: (see host catalog implementation in `DeviceSelection`)
- Consumed by: `MainSwipeHostPage`

### `IDeviceOverrideSource`
- Defined: `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/Overrides/IDeviceOverrideSource.cs`
- Implemented by: `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/Overrides/YamlDeviceOverrideSource.cs`
- Consumed by: `ContentViewFactory`

---

## Notes on “too many interfaces”

Common causes of interface proliferation in this codebase:

- DDI-style two-phase initialization (`InitializeAsync`) encourages interface-first registration.
- Message contracts for the event relay are all interfaces.
- Some services use multiple interfaces in a stack (logger stack, settings stack).

A pragmatic next step is to identify which interfaces are:

- **Message contracts** (good candidates to keep as interfaces)
- **External dependencies** (good candidates to keep as interfaces)
- **Single-implementation internal services** (possible candidates to collapse to concretes)

### Potential interface-reduction candidates (summary)

This table is intentionally conservative. It lists interfaces that *appear* to have a single implementation and are used in a narrow area of the codebase.

Before removing any interface, confirm:

- It is not referenced by generated DDI wiring that assumes an interface.
- It is not used across multiple projects as a stable contract.
- You are not relying on dynamic replacement (platform-specific, dev/test toggles).

| Interface | Likely implementations | Primary usage | Candidate action | Notes |
|---|---:|---|---|---|
| `ILoggerFile` | 1 | Logging stack | Consider folding into `ILoggerResilient` config | Only if nothing consumes this directly as a DI boundary |
| `ILoggerPostgreSQL` | 1 | Logging stack | Consider folding into `ILoggerResilient` config | Same caution as above |
| `ILoggerStub` | 1 | Bootstrapping, settings init | Keep (for now) | Often used at startup; low complexity |
| `IPlatformPaths` | 1 | Station snapshot persistence | Consider concrete dependency | If never substituted per-platform, could become `DefaultPlatformPaths` |
| `IStationMetadataPersister` | 1 | Postgres persistence | Consider removing only if no alternate persisters planned | Useful seam if you add other sinks later |
| `ITempestRestClient` | 1 | REST client | Keep | External dependency; good interface boundary |
| `IServiceReady` | 1+ | Readiness awaitable | Keep | Cross-cutting pattern |
| `IContentVariantCatalog` | 1 | MAUI device selection | Consider making concrete | Only if it stays purely internal to MAUI project |
| `IHostCompositionCatalog` | 1 (expected) | MAUI device selection | Consider making concrete | Verify actual implementation file before action |
| `IContentViewFactory` | 1 | MAUI host pages | Consider making concrete | This is basically a local factory |
| `IDeviceOverrideSource` | 1 | MAUI device overrides | Consider making concrete | If you don’t plan alternate sources (remote, JSON, etc.) |

The message interfaces (`IObservationReading`, `IWindReading`, etc.) are **not** listed as candidates because they are message contracts used by the event relay.

---

## Tempest UDP DTO/parsing (MetWorks_IoT_UDP_Tempest)

These interfaces are local to the Tempest UDP parsing layer and are primarily used to describe JSON DTO shape.

### `IPacketDtoBase`
- Defined: `src/MetWorks_IoT_UDP_Tempest/IPacketDtoBase.cs`
- Implemented by: `PacketDtoBase` (base class) and individual DTOs
- Used by:
  - `PacketFactory` return shape (`(PacketEnum, IPacketDtoBase)`)

### `IWindDto`
- Defined: `src/MetWorks_IoT_UDP_Tempest/IWindDto.cs`
- Implemented by: `src/MetWorks_IoT_UDP_Tempest/WindDto.cs`
- Used by:
  - DTO contract for wind packet fields (epoch, speed, direction)

### `ILightningDto`
- Defined: `src/MetWorks_IoT_UDP_Tempest/ILightningDto.cs`

### `IPrecipitationDto`
- Defined: `src/MetWorks_IoT_UDP_Tempest/IPrecipitationDto.cs`

### `IObservationDto`
- Defined: `src/MetWorks_IoT_UDP_Tempest/IObservationDto.cs`

### `IObservationEntryDto`
- Defined: `src/MetWorks_IoT_UDP_Tempest/IObservationEntryDto.cs`

### `IPacketFactory`
- Defined: `src/MetWorks_IoT_UDP_Tempest/IPacketFactory.cs`
- Implemented by: `src/MetWorks_IoT_UDP_Tempest/PacketFactory.cs` (internal)
- Notes:
  - This is a "static interface" pattern that forwards to `PacketFactory` static members.
  - Consider whether this indirection is still buying anything vs using `PacketFactory` directly.

### `IRawPacketRecordTypedFactory`
- Defined: `src/MetWorks_IoT_UDP_Tempest/IRawPacketRecordTypedFactory.cs`
- Implemented by: `src/MetWorks_IoT_UDP_Tempest/RawPacketRecordTypedFactory.cs` (internal static)
- Notes:
  - Same static-forwarding pattern as `IPacketFactory`.

If you want, the next revision of this doc can add a section per interface with:
- “number of implementations”
- “is it used as an event message?”
- “is it used across project boundaries?”
