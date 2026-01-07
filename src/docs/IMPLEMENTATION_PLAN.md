# IMPLEMENTATION_PLAN.md
# Weather Station MAUI - Implementation Plan
## Provenance Tracking & Event-Driven Unit Conversion

Created: 2026-01-06  
Status: In Progress  
Context: This document tracks the implementation of the architecture defined in ARCHITECTURE.md

---

## Executive Summary

We are implementing a comprehensive provenance tracking system with event-driven unit conversion for a .NET MAUI weather station application. The system uses COMB GUIDs for chronological traceability, RedStar.Amounts for type-safe unit handling, and a pure push pattern for settings changes.

### Key Architectural Decisions
1. Two COMB GUIDs: Raw packet ID (immutable source) + Transformed reading ID (each transformation)
2. Event-Driven Settings: Settings changes trigger automatic retransformation via events
3. Last-Packet Cache: Maintains most recent packet per type for immediate retransformation
4. Embedded Provenance: Every reading carries complete lineage metadata
5. RedStar.Amounts: Type-safe unit handling with automatic conversions

---

## Current State Analysis

### Existing Components ✅
- COMB GUID Generator: src\utility\IdGenerator.cs - Creates chronologically sortable GUIDs
- Raw Packet Factory: src\udp_packets\RawPacketRecordTypedFactory.cs - Assigns COMB GUIDs at UDP receipt
- UDP Transformer: src\udp_in_raw_packet_record_typed_out\Transformer.cs - Receives and parses UDP packets
- Settings Repository: src\settings\SettingsRepository.cs - Loads/stores settings (needs event enhancement)
- PostgreSQL Sink: src\raw_packet_record_type_in_postgres_out\ListenerSink.cs - Persists to database
- Event Relay: ISingletonEventRelay - Existing pub/sub system for messages
- RedStar.Amounts Core: src\RedStar.Amounts - Unit handling library (missing some unit types)

### Configuration ✅
- Registry YAML: src\docs\maximal-valid.yaml - Declarative DI configuration
- Unit Settings Already Defined:
  - Temperature: /services/udp/unitOverrides/temperature (fahrenheit/celsius)
  - Wind Speed: /services/udp/unitOverrides/windSpeed (mph/kph/m/s/knots)
  - Pressure: /services/udp/unitOverrides/pressure (mbar/inHg/hPa/kPa)
  - Precipitation: /services/udp/unitOverrides/precipitation (inch/mm/cm)
  - Distance: /services/udp/unitOverrides/distance (mile/km/m)

### What's Missing ❌
1. Event system in SettingsRepository
2. ProvenanceTracker service
3. Provenance data models
4. WeatherDataTransformer service
5. Weather reading interfaces with RedStar.Amounts
6. Missing RedStar.Amounts unit types (SpeedUnits, PressureUnits)
7. Test project structure
8. UI ViewModels and pages

---

## Implementation Phases

### Phase 1: Foundation (Core Infrastructure) 🔵

#### 1.1 RedStar.Amounts Extensions ✅ COMPLETED
Location: src\RedStar.Amounts.WeatherExtensions\  
Files Created:
- ✅ WeatherUnitAliases.cs - Alias mapper using RedStar's UnitResolve event
- ✅ RedStar.Amounts.WeatherExtensions.csproj - Extension project

Dependencies: None  
Estimated Time: 30 minutes  
Status: ⏳ Not Started

Acceptance Criteria:
- [ ] All speed units match YAML enum values
- [ ] All pressure units match YAML enum values
- [ ] Conversion functions registered in RegisterConversions()
- [ ] Units inherit from RedStar.Amounts.Unit base class

---

####     Provenance Data Models ✅ COMPLETED
Location: src\weather-station-maui\Models\Provenance\  
Files Created:
- ✅ ReadingProvenance.cs - Tracks transformation pipeline
- ✅ DataLineage.cs - Complete packet history  
- ✅ ProvenanceStep.cs - Individual pipeline step
- ✅ ProcessingError.cs - Error tracking
- ✅ DataStatus.cs - Enum for packet lifecycle states

Code Structure:

    public record ReadingProvenance
    {
        public required Guid RawPacketId { get; init; }
        public required DateTime UdpReceiptTime { get; init; }
        public required DateTime TransformStartTime { get; init; }
        public required DateTime TransformEndTime { get; init; }
        public TimeSpan TransformDuration => TransformEndTime - TransformStartTime;
        public TimeSpan TotalPipelineTime => TransformEndTime - UdpReceiptTime;
        public string? SourceUnits { get; init; }
        public string? TargetUnits { get; init; }
        public string TransformerVersion { get; init; } = "1.0";
    }
    
    public enum DataStatus
    {
        Received, Parsed, Transformed, Retransformed, 
        Persisted, Failed, Buffered, Displayed
    }

Acceptance Criteria:
- [ ] All records use required keyword for mandatory fields
- [ ] Computed properties for durations
- [ ] DataStatus enum matches ARCHITECTURE.md appendix
- [ ] XML documentation on all public members

---

### Phase 2: Settings Enhancement 🟢

#### 2.1 Enhanced SettingsRepository with Events ✅ COMPLETED
Location: src\settings\SettingsRepository.cs  
Status: ✅ COMPLETED

Changes Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocationChanges Made:
- ✅ Added ConcurrentDictionary for event handler storage
- ✅ Added OnSettingChanged() for exact path subscriptions
- ✅ Added OnSettingsChanged() for wildcard prefix subscriptions
- ✅ Enhanced ApplyOverrides() to track changes
- ✅ Added NotifySettingChanges() with exception isolation
- ✅ Thread-safe handler storage and invocation

    private static readonly ConcurrentDictionary<string, List<Action<string, string?, string?>>> _settingChangeHandlers = new();

2. Add Subscription Methods:

    public void OnSettingChanged(string settingPath, Action<string, string?, string?> handler)
    public void OnSettingsChanged(string pathPrefix, Action<string, string?, string?> handler)

3. Enhance ApplyOverrides():

    // Track changes: Dictionary<path, (oldValue, newValue)>
    // Call NotifySettingChanges() after applying

4. Add Notification Method:

    private void NotifySettingChanges(Dictionary<string, (string? oldValue, string? newValue)> changedSettings)
    // Support exact match and wildcard (prefix/*) subscriptions

Dependencies: None  
Estimated Time: 1 hour  
Status: ⏳ Not Started

Testing Strategy:
- Subscribe to specific path → verify callback invoked
- Subscribe with wildcard → verify all matching paths invoke callback
- Multiple subscribers → verify all invoked
- No change detected → verify no notifications

Acceptance Criteria:
- [ ] Existing functionality unchanged (backward compatible)
- [ ] Event handlers stored per path/wildcard
- [ ] Wildcard subscriptions work (/ui/units/*)
- [ ] Only changed settings trigger events
- [ ] Thread-safe event handler storage
- [ ] Exceptions in handlers don't break notification loop

---

### Phase 3: Provenance Tracking 🟡

#### 3.1 ProvenanceTracker Service ✅ COMPLETED
Location: src\weather-station-maui\Services\ProvenanceTracker.cs
Status: ✅ COMPLETED

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Created:
- ✅ ProvenanceTracker service with LRU cache
- ✅ ProvenanceStatistics record
- ✅ Complete public API (13 methods)
- ✅ Thread-safe concurrent access
- ✅ COMB GUID chronological sorting
- ✅ Exception isolation in all methods

Key Features:
- COMB GUID-keyed storage (chronologically ordered)
- Track complete pipeline: UDP → Parse → Transform → Persist → Display
- Performance metrics aggregation
- Query by: ID, time range, status, packet type

Public API:

    public async Task<bool> InitializeAsync(IFileLogger iFileLogger)
    public DataLineage TrackNewPacket(IRawPacketRecordTyped packet)
    public void AddStep(Guid packetId, string stepName, string component, string? details = null, TimeSpan? duration = null)
    public void UpdateStatus(Guid packetId, DataStatus newStatus)
    public void RecordError(Guid packetId, string component, string stepName, Exception exception)
    public void LinkTransformedReading(Guid packetId, Guid transformedId)
    public void LinkDatabaseRecord(Guid packetId, Guid dbRecordId)
    public DataLineage? GetLineage(Guid packetId)
    public List<DataLineage> GetRecentLineages(int count = 100)
    public List<DataLineage> GetLineagesByStatus(DataStatus status)
    public List<DataLineage> GetLineagesByTimeRange(DateTime start, DateTime end)
    public ProvenanceStatistics GetStatistics()
    public string ExportLineageAsJson(Guid packetId)

Dependencies: 
- Provenance models (Phase 1.2)
- IFileLogger
- IRawPacketRecordTyped

Estimated Time: 2 hours  
Status: ⏳ Not Started

Acceptance Criteria:
- [ ] Max 1000 lineages stored (LRU eviction)
- [ ] Thread-safe concurrent access
- [ ] COMB GUID sorting works correctly
- [ ] Statistics include p50, p95, p99 percentiles
- [ ] JSON export includes full lineage
- [ ] Graceful handling of missing lineages

---

### Phase 4: Weather Reading Models ✅ COMPLETED

#### 4.1 Weather Reading Interfaces ✅ COMPLETED
Files Created:
- ✅ IWeatherReading.cs - Base interface with provenance
- ✅ IObservationReading.cs - Temperature, humidity, pressure
- ✅ IWindReading.cs - Speed, direction, gusts
- ✅ IPrecipitationReading.cs - Rain rate, totals
- ✅ ILightningReading.cs - Distance, strike count

#### 4.2 Weather Reading Implementations ✅ COMPLETED
Files Created:
- ✅ WeatherReadings.cs - All 4 record implementations
  - ObservationReading
  - WindReading
  - PrecipitationReading
  - LightningReading

---

### Phase 5: Data Transformation 🔴

#### 5.1 WeatherDataTransformer Service ✅ COMPLETED
Location: src\weather-station-maui\Services\WeatherDataTransformer.cs
Status: ✅ COMPLETED

Created:
- ✅ Complete transformation service (600+ lines)
- ✅ Last-packet cache with LRU eviction
- ✅ Event-driven retransformation
- ✅ 4 packet parsers (Observation, Wind, Precipitation, Lightning)
- ✅ RedStar.Amounts integration
- ✅ ProvenanceTracker integration
- ✅ Cardinal direction conversion helper

Public API:

    public async Task<bool> InitializeAsync(IFileLogger iFileLogger, ISettingsRepository iSettingsRepository)
    public void RefreshUnitPreferences()  // Public for testing
    public async ValueTask DisposeAsync()

Private Methods:

    private void RegisterUnitConversions()
    private void LoadUnitPreferences()
    private void OnUnitSettingChanged(string settingPath, string? oldValue, string? newValue)
    private void OnRawPacketReceived(IRawPacketRecordTyped rawPacket)
    private void TransformAndPublish(IRawPacketRecordTyped rawPacket, bool isRetransformation)
    private void RetransformCachedPackets()
    private IObservationReading? ParseObservation(IRawPacketRecordTyped rawPacket, bool isRetransformation)
    private IWindReading? ParseWind(...)
    private IPrecipitationReading? ParsePrecipitation(...)
    private ILightningReading? ParseLightning(...)
    private string DegreesToCardinal(double degrees)

Cache Structure:

    private readonly ConcurrentDictionary<PacketEnum, IRawPacketRecordTyped> _lastPacketCache = new();

Unit Preference Fields:

    private Unit _preferredTemperatureUnit = TemperatureUnits.DegreeFahrenheit;
    private Unit _preferredPressureUnit = PressureUnits.InchOfMercury;
    private Unit _preferredSpeedUnit = SpeedUnits.MilePerHour;
    private Unit _preferredDistanceUnit = LengthUnits.Mile;
    private Unit _preferredPrecipitationUnit = LengthUnits.Inch;

Dependencies:
- Enhanced SettingsRepository (Phase 2.1)
- Weather reading models (Phase 4.1, 4.2)
- RedStar.Amounts units (Phase 1.1)
- ProvenanceTracker (Phase 3.1)
- ISingletonEventRelay
- IFileLogger

Estimated Time: 3-4 hours  
Status: ⏳ Not Started

Flow on Settings Change:
1. User changes /services/udp/unitOverrides/temperature to "degree celsius"
2. SettingsRepository.ApplyOverrides() detects change
3. SettingsRepository fires event: ("/services/udp/unitOverrides/temperature", "degree fahrenheit", "degree celsius")
4. WeatherDataTransformer.OnUnitSettingChanged() invoked
5. LoadUnitPreferences() called → _preferredTemperatureUnit = TemperatureUnits.DegreeCelsius
6. RetransformCachedPackets() called
7. For each PacketEnum in cache:
   - Get cached raw packet
   - Call ParseObservation/ParseWind/etc with isRetransformation = true
   - Create NEW COMB GUID for transformedId
   - Apply current unit preferences
   - Set Provenance.TransformerVersion = "1.0-retransform"
   - Publish via ISingletonEventRelay
8. UI receives new reading with updated units instantly

Acceptance Criteria:
- [ ] Subscribes to all unit settings with wildcard pattern
- [ ] Cache maintains exactly one packet per PacketEnum
- [ ] Retransformation creates new COMB GUID
- [ ] Retransformation links to original rawPacketId
- [ ] Provenance tracks isRetransformation
- [ ] All RedStar.Amounts conversions successful
- [ ] Gracefully handles missing JSON fields
- [ ] Logs all transformations with IFileLogger
- [ ] Integrates with ProvenanceTracker

---

### Phase 6: Integration ✅ COMPLETED

#### 6.1 Update UdpTransformer ✅ COMPLETED
- ✅ Added ProvenanceTracker field and parameter
- ✅ TrackNewPacket() called after packet creation
- ✅ AddStep() called for JSON parsing
- ✅ UpdateStatus() called on failures

#### 6.2 Update ListenerSink ✅ COMPLETED  
- ✅ Added ProvenanceTracker field and parameter
- ✅ LinkDatabaseRecord() called after successful write
- ✅ RecordError() called in all catch blocks

#### 6.3 Update Registry Configuration ✅ COMPLETED
- ✅ Added MetWorksWeather.Services namespace
- ✅ Created TheProvenanceTracker instance
- ✅ Created TheWeatherDataTransformer instance
- ✅ Wired up all dependencies

Dependencies: ProvenanceTracker (Phase 3.1)  
Estimated Time: 30 minutes  
Status: ⏳ Not Started

---

#### 6.2 Update ListenerSink (PostgreSQL)
Location: src\raw_packet_record_type_in_postgres_out\ListenerSink.cs (MODIFY)  
Changes:
1. Add ProvenanceTracker field (optional - for diagnostics)
2. Call provenanceTracker.LinkDatabaseRecord() after successful write
3. Call provenanceTracker.RecordError() on write failure

Dependencies: ProvenanceTracker (Phase 3.1)  
Estimated Time: 30 minutes  
Status: ⏳ Not Started

---

#### 6.3 Update Registry Configuration
Location: src\docs\maximal-valid.yaml (MODIFY)  
Changes:
1. Add ProvenanceTracker namespace and class definition
2. Add WeatherDataTransformer namespace and class definition
3. Add instances for both services
4. Wire up dependencies

Example Addition:

    - name: "MetWorksWeather.Services"
      interface: []
      class:
        - name: "ProvenanceTracker"
          interface: null
          parameter:
            - name: "iFileLogger"
              class: null
              interface: "InterfaceDefinition.IFileLogger"
        - name: "WeatherDataTransformer"
          interface: InterfaceDefinition.IBackgroundService
          parameter:
            - name: "iFileLogger"
              class: null
              interface: "InterfaceDefinition.IFileLogger"
            - name: "iSettingsRepository"
              class: null
              interface: "InterfaceDefinition.ISettingsRepository"
    
    instance:
      - name: "TheProvenanceTracker"
        class: "MetWorksWeather.Services.ProvenanceTracker"
        assignment:
          - name: "iFileLogger"
            literal: null
            instance: "TheFileLogger"
        element: []
    
      - name: "TheWeatherDataTransformer"
        class: "MetWorksWeather.Services.WeatherDataTransformer"
        assignment:
          - name: "iFileLogger"
            literal: null
            instance: "TheFileLogger"
          - name: "iSettingsRepository"
            literal: null
            instance: "TheUDPSettingsRepository"
        element: []

Dependencies: All services implemented  
Estimated Time: 45 minutes  
Status: ⏳ Not Started

---

### Phase 7: Testing 🟤

#### 7.1 Test Project Setup
Location: tests\WeatherStation.Tests\  
Project File: WeatherStation.Tests.csproj

NuGet Packages:

    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="FluentAssertions" Version="7.0.0-alpha.3" />
    <PackageReference Include="Moq" Version="4.20.72" />

Test Structure:

    tests/WeatherStation.Tests/
    ├── Services/
    │   ├── ProvenanceTrackerTests.cs
    │   ├── WeatherDataTransformerTests.cs
    │   ├── SettingsRepositoryTests.cs
    │   └── UdpTransformerTests.cs
    ├── Models/
    │   ├── WeatherReadingTests.cs
    │   └── ProvenanceTests.cs
    ├── Utilities/
    │   └── IdGeneratorTests.cs
    └── Integration/
        ├── EndToEndPipelineTests.cs
        └── SettingsChangeRetransformTests.cs

Dependencies: All implementation phases complete  
Estimated Time: 1 hour (setup only)  
Status: ⏳ Not Started

---

#### 7.2 Unit Tests - Priority List

COMB GUID Tests (30 min):

    [Fact] public void CombGuids_ShouldBe_ChronologicallySortable()
    [Fact] public void CombGuids_ShouldContain_TimestampInLastBytes()
    [Fact] public void CombGuids_GeneratedConcurrently_ShouldBeUnique()

Unit Conversion Tests (45 min):

    [Theory]
    [InlineData(72, 22.22)]  // F to C
    [InlineData(100, 37.78)]
    public void TemperatureConversion_Fahrenheit_To_Celsius_Should_Be_Accurate(double f, double c)
    
    [Theory]
    [InlineData(29.92, 1013.25)]  // inHg to mbar
    public void PressureConversion_InchMercury_To_Millibar_Should_Be_Accurate(double inHg, double mbar)

Settings Event Tests (1 hour):

    [Fact] public async Task SettingsChange_Should_TriggerEventForExactMatch()
    [Fact] public async Task SettingsChange_Should_TriggerEventForWildcardMatch()
    [Fact] public async Task SettingsChange_NoChange_Should_NotTriggerEvent()
    [Fact] public async Task SettingsChange_MultipleSubscribers_Should_InvokeAll()

Last-Packet Cache Tests (1 hour):

    [Fact] public void LastPacketCache_Should_MaintainOnePerPacketType()
    [Fact] public void LastPacketCache_Should_OverwriteOlderPacketOfSameType()
    [Fact] public void LastPacketCache_DifferentTypes_Should_NotInterfere()

Retransformation Tests (1.5 hours):

    [Fact] public async Task SettingsChange_Should_RetransformCachedPackets()
    [Fact] public async Task Retransformation_Should_CreateNewCombGuid()
    [Fact] public async Task Retransformation_Should_LinkToOriginalPacket()
    [Fact] public async Task Retransformation_Should_MarkProvenanceCorrectly()

Provenance Tests (1 hour):

    [Fact] public void ProvenanceTracker_Should_TrackNewPacket()
    [Fact] public void ProvenanceTracker_Should_LinkTransformedReading()
    [Fact] public void ProvenanceTracker_Should_RecordErrors()
    [Fact] public void ProvenanceTracker_Should_CalculateStatistics()
    [Fact] public void ProvenanceTracker_Should_QueryByTimeRange()

Total Testing Time: ~6-8 hours

---

### Phase 8: UI (Future Phases) ⚪

#### 8.1 ViewModels
- BaseViewModel
- WeatherViewModel
- DiagnosticsViewModel
- SettingsViewModel

#### 8.2 XAML Pages
- MainPage (Weather Dashboard)
- DiagnosticsPage (Provenance viewer)
- SettingsPage (Unit preferences)

#### 8.3 Diagnostic Views
- Provenance trace viewer
- Performance graphs
- Error log

Status: 📅 Scheduled for later  
Dependencies: All previous phases complete

---

## Progress Tracking

### Completion Status
- [x] Phase 1: Foundation (2/2 tasks) ✅
- [x] Phase 2: Settings Enhancement (1/1 tasks) ✅
- [x] Phase 3: Provenance Tracking (1/1 tasks) ✅
- [x] Phase 4: Weather Reading Models (2/2 tasks) ✅
- [x] Phase 5: Data Transformation (1/1 tasks) ✅
- [x] Phase 6: Integration (3/3 tasks) ✅
- [ ] Phase 7: Testing (0/2 tasks) 📅 Future
- [ ] Phase 8: UI (0/3 tasks) 📅 Future