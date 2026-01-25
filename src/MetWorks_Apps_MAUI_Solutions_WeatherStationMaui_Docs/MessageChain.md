# Complete Message Chain Documentation

## Overview
This document details every message sent through ISingletonEventRelay in chronological order, including message types, senders, receivers, and what each component does with the data.

---

## MESSAGE FLOW CHAIN

### 1. IRawPacketRecordTyped

SENT BY:
- Component: Transformer (UDP Listener)
- Location: src/udp_in_raw_packet_record_typed_out/Transformer.cs
- Method: ProcessPacketAsync()
- Code: ISingletonEventRelay.Send(iRawPacketRecordTyped);

MESSAGE CONTAINS:
- Id - COMB GUID (chronologically sortable)
- PacketEnum - Type of packet (Observation, Wind, Precipitation, Lightning)
- JsonAsReadOnlyMemoryOfChar - Raw JSON from weather station
- ReceivedTime - UTC timestamp when packet arrived
- ReceivedUtcUnixEpochSecondsAsLong - Unix epoch seconds

RECEIVED BY:
- Component: WeatherDataTransformer
- Location: src/metworks_services/WeatherDataTransformer.cs
- Method: OnRawPacketReceived()
- Registration: ISingletonEventRelay.Register<IRawPacketRecordTyped>(this, OnRawPacketReceived);

WHAT RECEIVER DOES:
1. Caches packet in _lastPacketCache for retransformation
2. Parses JSON based on PacketEnum type
3. Converts from metric units to user preferred units
4. Creates typed reading with provenance
5. Publishes typed reading (see below)

FREQUENCY: Every UDP packet received (~3 seconds for wind, ~60 seconds for observation)

---

### 2. IObservationReading

SENT BY:
- Component: WeatherDataTransformer
- Location: src/metworks_services/WeatherDataTransformer.cs
- Method: TransformAndPublish()
- Code: case IObservationReading obs: ISingletonEventRelay.Send(obs);

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
- Component: WeatherViewModel
- Location: src/weather-station-maui/ViewModels/WeatherViewModel.cs
- Method: OnObservationReceived()
- Registration: ISingletonEventRelay.Register<IObservationReading>(this, OnObservationReceived);

WHAT RECEIVER DOES:
1. Marshals to main thread (MainThread.BeginInvokeOnMainThread)
2. Updates CurrentObservation property
3. Triggers PropertyChanged for: CurrentObservation, TemperatureDisplay, PressureDisplay, HumidityDisplay
4. UI automatically updates via data binding

FREQUENCY: ~Every 60 seconds (from weather station), Immediately on unit preference change (retransformation)

---

### 3. IWindReading

SENT BY:
- Component: WeatherDataTransformer
- Location: src/metworks_services/WeatherDataTransformer.cs
- Method: TransformAndPublish()
- Code: case IWindReading wind: ISingletonEventRelay.Send(wind);

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
- Component: WeatherViewModel
- Location: src/weather-station-maui/ViewModels/WeatherViewModel.cs
- Method: OnWindReceived()
- Registration: ISingletonEventRelay.Register<IWindReading>(this, OnWindReceived);

WHAT RECEIVER DOES:
1. Marshals to main thread
2. Updates CurrentWind property
3. Triggers PropertyChanged for: CurrentWind, WindSpeedDisplay, WindDirectionDisplay, WindGustDisplay
4. UI automatically updates via data binding

FREQUENCY: ~Every 3 seconds (rapid_wind packets from weather station), Immediately on unit preference change (retransformation)

---

### 4. IPrecipitationReading

SENT BY:
- Component: WeatherDataTransformer
- Location: src/metworks_services/WeatherDataTransformer.cs
- Method: TransformAndPublish()
- Code: case IPrecipitationReading precip: ISingletonEventRelay.Send(precip);

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

### 5. ILightningReading

SENT BY:
- Component: WeatherDataTransformer
- Location: src/metworks_services/WeatherDataTransformer.cs
- Method: TransformAndPublish()
- Code: case ILightningReading lightning: ISingletonEventRelay.Send(lightning);

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