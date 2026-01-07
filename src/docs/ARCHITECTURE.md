# Weather Station MAUI - Architecture & Data Provenance

## Overview

This document describes the data flow, transformation pipeline, provenance tracking, and testing strategy for the Weather Station MAUI application.

## System Architecture

### Data Flow Pipeline

[ASCII diagram - Data flows from UDP packets through transformation to UI/Database/Analytics]
┌─────────────────┐
│   UDP Packets   │ Raw JSON from weather station
│   (COMB GUID)   │
└────────┬────────┘
         │
         ▼
┌─────────────────────────────┐
│  UdpTransformer             │
│  - Receives UDP packets     │
│  - Assigns COMB GUID        │
│  - Creates IRawPacketRecord │
│  - Tracks: UDP Receipt Time │
└────────┬────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  ProvenanceTracker          │ ⭐ NEW
│  - Records lineage          │
│  - Tracks performance       │
│  - Links transformations    │
└────────┬────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  WeatherDataTransformer     │
│  - Parses JSON              │
│  - Converts to RedStar.Amount│
│  - Applies unit preferences │
│  - Caches last packet/type  │ ⭐ NEW
│  - Tracks: Transform Time   │
└────────┬────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  Typed Weather Readings     │
│  - IObservationReading      │
│  - IWindReading             │
│  - With embedded provenance │
│  - Tracks: Total Pipeline Time│
└────────┬────────────────────┘
         │
         ├──────────┬─────────────┐
         │          │             │
         ▼          ▼             ▼
    ┌──────┐  ┌─────────┐  ┌──────────┐
    │  UI  │  │Database │  │ Analytics│
    └──────┘  └─────────┘  └──────────┘

## COMB GUID Strategy

### Purpose
- Globally unique identifiers
- Chronologically sortable (last 6 bytes = timestamp)
- Database-friendly (clustered index performance)
- Provenance tracking (maintains parent-child relationships)

### Usage Pattern
1. Raw Packet: Assigned at UDP receipt → rawPacketId
2. Transformed Reading: New COMB GUID → transformedId
3. Database Record: Uses rawPacketId as primary key
4. Linkage: transformedId references rawPacketId in provenance

### Why Two GUIDs?

The system uses separate GUIDs to distinguish between:
- Raw Packet GUID: The immutable source data (UDP packet arrival event)
- Transformed Reading GUID: Each specific transformation operation

This enables:
- Tracking multiple transformations of the same raw data
- Performance analysis per transformation
- Retransformation when settings change
- Complete audit trail of data lineage

### Implementation

Location: src\utility\IdGenerator.cs

    public static Guid CreateCombGuid()
    {
        var guidArray = Guid.NewGuid().ToByteArray();
        var timestamp = DateTime.UtcNow.Ticks;
        
        // Embed timestamp in last 6 bytes
        byte[] timeBytes = BitConverter.GetBytes(timestamp);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);
        
        Array.Copy(timeBytes, 2, guidArray, 10, 6);
        return new Guid(guidArray);
    }

Called from: src\udp_packets\RawPacketRecordTypedFactory.Create()

### Benefits
1. Chronological Ordering: Can sort by COMB GUID to get time-based order
2. Efficient Indexing: Database clustered indexes perform better than random GUIDs
3. Traceability: Time component aids in debugging and diagnostics
4. No Coordination: Each component can generate IDs independently

## Settings-Driven Unit Conversion (Event-Driven Architecture)

### Design Pattern: Push with Retransformation

The system uses a pure push pattern where settings changes propagate automatically through events:

[Flow diagram showing settings change triggering retransformation]
┌─────────────────────────────────────────────────────────┐
│ 1. User changes unit preference in UI                  │
│    (e.g., Fahrenheit → Celsius)                        │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 2. UI writes to overrides.yaml                         │
│    /ui/units/temperature: "celsius"                     │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 3. UI calls SettingsRepository.ApplyOverrides()        │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 4. SettingsRepository detects change                   │
│    Fires "SettingsChanged" event                       │
│    Event payload: (path, oldValue, newValue)           │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 5. WeatherDataTransformer.OnUnitSettingChanged()       │
│    - Reloads unit preferences from SettingsRepository  │
│    - Accesses lastPacketCache (per PacketEnum type)    │
│    - Retransforms last Observation (if exists)         │
│    - Retransforms last Wind (if exists)                │
│    - Retransforms last Precipitation (if exists)       │
│    - Retransforms last Lightning (if exists)           │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 6. Each retransformation creates NEW reading           │
│    - NEW transformedId (fresh COMB GUID)               │
│    - SAME SourcePacketId (links to original)           │
│    - NEW units applied based on current preferences    │
│    - Provenance.TransformerVersion = "1.0-retransform" │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 7. ISingletonEventRelay publishes updated readings     │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 8. UI ViewModel receives IWeatherReading events        │
│    Displays: Temperature: 22.5°C (immediately!)        │
└─────────────────────────────────────────────────────────┘

### Key Design Principles

1. Pure Push Pattern: UI doesn't know about transformation internals
2. Event-Driven: Settings changes flow through event system like packets
3. Last-Packet Cache: Transformer maintains most recent packet per type
4. Immediate Feedback: User sees updated units within milliseconds
5. No Wait Time: Even rare events (lightning, precipitation) retransform immediately
6. Full Traceability: Each retransformation gets unique COMB GUID with provenance

### Why Last-Packet Cache?

Weather data arrives at different frequencies:
- Wind: Every few seconds
- Observation: Every minute
- Precipitation: Only during rain events (could be days apart)
- Lightning: Only during storms (could be weeks apart)

Without caching, users changing from Fahrenheit to Celsius would:
- ✅ See immediate update for Wind (arrives frequently)
- ❌ Wait minutes for next Observation
- ❌ Wait days/weeks for Precipitation or Lightning

With caching: All packet types retransform immediately on settings change.

## Transformation Lifecycle Example

### Scenario: User Changes Temperature Unit Preference

[Complete flow from initial packet through settings change and retransformation]
┌─────────────────────────────────────────────────────────┐
│ Initial State: User prefers Fahrenheit                 │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ UDP Packet Received at 12:00:00                        │
│ rawPacketId: abc-123-def-456 (COMB GUID)              │
│ Timestamp: 2026-01-06 12:00:00.000                    │
│ JSON: {"type":"observation","temperature":72.5,...}    │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ ProvenanceTracker.TrackNewPacket()                     │
│ Status: Received                                        │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ Transformation #1 (User prefers Fahrenheit)            │
│ transformedId: xyz-111-aaa-222 (NEW COMB GUID)        │
│ SourcePacketId: abc-123-def-456 (LINK back)           │
│ Temperature: 72.5°F → 72.5°F (no conversion)          │
│ Provenance:                                             │
│   - RawPacketId: abc-123-def-456                       │
│   - TransformDuration: 5ms                             │
│   - SourceUnits: "°F"                                  │
│   - TargetUnits: "°F"                                  │
│   - TransformerVersion: "1.0"                          │
│                                                         │
│ ⭐ Cached in lastPacketCache[Observation]              │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ UI displays: Temperature: 72.5°F                       │
└─────────────────────────────────────────────────────────┘

                 [5 minutes pass, no new Observation packets]

┌─────────────────────────────────────────────────────────┐
│ User Changes Setting at 12:05:00                       │
│ Preference: Celsius                                     │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ SettingsRepository fires event                         │
│ Path: "/ui/units/temperature"                          │
│ OldValue: "fahrenheit", NewValue: "celsius"            │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ WeatherDataTransformer.OnUnitSettingChanged()          │
│ - Loads new preference: DegreeCelsius                  │
│ - Retrieves from cache: rawPacket abc-123-def-456     │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ Transformation #2 (SAME raw packet, NEW transformation)│
│ transformedId: xyz-222-bbb-333 (DIFFERENT COMB GUID)  │
│ SourcePacketId: abc-123-def-456 (SAME source)         │
│ Temperature: 72.5°F → 22.5°C (conversion applied!)    │
│ Provenance:                                             │
│   - RawPacketId: abc-123-def-456                       │
│   - TransformDuration: 3ms                             │
│   - SourceUnits: "°F"                                  │
│   - TargetUnits: "°C"                                  │
│   - TransformerVersion: "1.0-retransform"              │
└────────────────┬────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────┐
│ UI displays: Temperature: 22.5°C (immediately!)        │
└─────────────────────────────────────────────────────────┘

### Provenance Tracking

Both transformations are tracked separately:
- Transformation #1: xyz-111-aaa-222 → stored in database
- Transformation #2: xyz-222-bbb-333 → stored in database

Both link back to same raw packet: abc-123-def-456

Query by raw packet ID returns complete transformation history:

    var lineage = provenanceTracker.GetLineage("abc-123-def-456");
    // Returns:
    // - Transformation at 12:00:00 → °F
    // - Transformation at 12:05:00 → °C

## Data Structures with Provenance

### Base Reading Interface

All weather readings inherit provenance metadata:

    public interface IWeatherReading
    {
        // Identity
        Guid Id { get; }                    // COMB GUID of transformed reading
        Guid SourcePacketId { get; }        // COMB GUID of original UDP packet
        
        // Temporal
        DateTime Timestamp { get; }          // Weather station timestamp
        DateTime ReceivedUtc { get; }        // System receipt time
        
        // Classification
        PacketEnum PacketType { get; }
        
        // Provenance (embedded)
        ReadingProvenance Provenance { get; }
    }

### Provenance Metadata

    public record ReadingProvenance
    {
        public required Guid RawPacketId { get; init; }
        public required DateTime UdpReceiptTime { get; init; }
        public required DateTime TransformStartTime { get; init; }
        public required DateTime TransformEndTime { get; init; }
        public TimeSpan TransformDuration => TransformEndTime - TransformStartTime;
        public TimeSpan TotalPipelineTime => TransformEndTime - UdpReceiptTime;
        
        // Unit conversion tracking
        public string? SourceUnits { get; init; }
        public string? TargetUnits { get; init; }
        
        // Distinguishes initial transforms from retransforms
        public string TransformerVersion { get; init; } = "1.0";  // "1.0-retransform" for settings changes
    }

### Example: Observation Reading

    public record ObservationReading : IObservationReading
    {
        public required Guid Id { get; init; }
        public required Guid SourcePacketId { get; init; }
        public required DateTime Timestamp { get; init; }
        public required DateTime ReceivedUtc { get; init; }
        public PacketEnum PacketType => PacketEnum.Observation;
        
        // Weather data with RedStar.Amounts
        public required Amount Temperature { get; init; }
        public required double HumidityPercent { get; init; }
        public required Amount Pressure { get; init; }
        public Amount? DewPoint { get; init; }
        public Amount? HeatIndex { get; init; }
        
        // Embedded provenance
        public required ReadingProvenance Provenance { get; init; }
    }

## Performance Tracking

### Metrics Collected
1. UDP Receipt Latency: Time from packet send to receipt
2. Parse Duration: JSON deserialization time
3. Transform Duration: Unit conversion time (tracked in provenance)
4. Retransform Duration: Time to retransform on settings change
5. Database Write Duration: Persistence time
6. End-to-End Latency: UDP → UI display time

### Aggregation
- Real-time moving averages (last 100 readings)
- Per-packet-type statistics
- Percentile tracking (p50, p95, p99)
- Separate tracking for initial transforms vs retransforms
- Stored in ProvenanceTracker for diagnostics

### Example Metrics Query

    // Get average transformation time by packet type
    var stats = provenanceTracker.GetStatistics();
    var avgTransformTime = stats.AverageProcessingTime;
    
    // Get performance for specific time range
    var readings = provenanceTracker.GetLineagesByTimeRange(startTime, endTime);
    var avgDuration = readings.Average(r => r.Provenance.TransformDuration.TotalMilliseconds);
    
    // Compare initial transforms vs retransforms
    var initialTransforms = readings.Where(r => r.Provenance.TransformerVersion == "1.0");
    var retransforms = readings.Where(r => r.Provenance.TransformerVersion == "1.0-retransform");

## Provenance Tracking System

### ProvenanceTracker Service

Centralized service that maintains in-memory lineage of recent data points:

    public class ProvenanceTracker
    {
        // Stores last 1000 lineages with COMB GUID keys (chronologically ordered)
        private ConcurrentDictionary<Guid, DataLineage> _lineageStore;
        
        // Key methods
        public DataLineage TrackNewPacket(IRawPacketRecordTyped packet);
        public void AddStep(Guid packetId, string stepName, string component);
        public void LinkTransformedReading(Guid packetId, Guid transformedId);
        public void RecordError(Guid packetId, string component, Exception ex);
        public DataLineage? GetLineage(Guid packetId);
        public List<DataLineage> GetRecentLineages(int count);
    }

### DataLineage Structure

    public record DataLineage
    {
        public required Guid RawPacketId { get; init; }
        public Guid? TransformedReadingId { get; init; }
        public Guid? DatabaseRecordId { get; init; }
        public required List<ProvenanceStep> ProcessingSteps { get; init; }
        public DataStatus Status { get; init; }
        public string? OriginalJson { get; init; }
        public PacketEnum PacketType { get; init; }
        public List<ProcessingError>? Errors { get; init; }
    }
    
    public record ProvenanceStep
    {
        public required string StepName { get; init; }
        public required DateTime Timestamp { get; init; }
        public required string Component { get; init; }
        public string? Details { get; init; }
        public TimeSpan? Duration { get; init; }
        public Guid StepId { get; init; } = IdGenerator.CreateCombGuid();
    }

### Integration Points
1. UdpTransformer: Calls TrackNewPacket() on receipt
2. WeatherDataTransformer: Calls LinkTransformedReading() after conversion
3. ListenerSink: Calls LinkDatabaseRecord() after persistence
4. UI: Queries lineage for diagnostics display

## Settings Architecture

### SettingsRepository Event System

    // Subscribe to specific setting
    settingsRepository.OnSettingChanged("/ui/units/temperature", (path, oldVal, newVal) => 
    {
        Console.WriteLine($"Temperature unit changed: {oldVal} → {newVal}");
    });
    
    // Subscribe to all settings matching prefix
    settingsRepository.OnSettingsChanged("/ui/units", (path, oldVal, newVal) => 
    {
        Console.WriteLine($"Any unit setting changed: {path}");
    });
    
    // Trigger change detection and notification
    await settingsRepository.ApplyOverrides();

### Wildcard Subscription Pattern

The settings repository supports wildcard subscriptions:
- Exact match: "/ui/units/temperature" → only temperature changes
- Prefix match: "/ui/units/" → all unit changes (temperature, pressure, speed, etc.)

This allows the transformer to subscribe once to all unit-related settings:

    iSettingsRepository.OnSettingsChanged("/ui/units", OnUnitSettingChanged);

## Testing Strategy

### Test Project Structure

    tests/
    ├── WeatherStation.Tests/
    │   ├── Services/
    │   │   ├── ProvenanceTrackerTests.cs
    │   │   ├── WeatherDataTransformerTests.cs
    │   │   ├── SettingsRepositoryTests.cs          ⭐ NEW
    │   │   └── UdpTransformerTests.cs
    │   ├── Models/
    │   │   ├── WeatherReadingTests.cs
    │   │   └── ProvenanceTests.cs
    │   ├── Utilities/
    │   │   └── IdGeneratorTests.cs
    │   └── Integration/
    │       ├── EndToEndPipelineTests.cs
    │       └── SettingsChangeRetransformTests.cs    ⭐ NEW

### Key Test Scenarios

#### 1. COMB GUID Ordering

    [Fact]
    public void CombGuids_ShouldBe_ChronologicallySortable()
    {
        var guid1 = IdGenerator.CreateCombGuid();
        Thread.Sleep(10);
        var guid2 = IdGenerator.CreateCombGuid();
        
        guid1.Should().BeLessThan(guid2);
    }

#### 2. Unit Conversion Accuracy

    [Fact]
    public void TemperatureConversion_Fahrenheit_To_Celsius_Should_Be_Accurate()
    {
        var tempF = new Amount(72, TemperatureUnits.DegreeFahrenheit);
        var tempC = tempF.ConvertedTo(TemperatureUnits.DegreeCelsius);
        
        tempC.Value.Should().BeApproximately(22.22, 0.01);
    }

#### 3. Settings Change Triggers Retransformation

    [Fact]
    public async Task SettingsChange_Should_RetransformCachedPackets()
    {
        var transformer = CreateTransformer();
        var rawPacket = CreateTestPacket();
        
        // Initial transformation
        await transformer.OnRawPacketReceived(rawPacket);
        var initialReading = GetLastPublishedReading();
        initialReading.Temperature.Unit.Should().Be(TemperatureUnits.DegreeFahrenheit);
        
        // Change settings
        settingsRepository.Set("/ui/units/temperature", "celsius");
        await settingsRepository.ApplyOverrides();
        
        // Should automatically retransform
        var retransformed = GetLastPublishedReading();
        retransformed.Temperature.Unit.Should().Be(TemperatureUnits.DegreeCelsius);
        retransformed.SourcePacketId.Should().Be(rawPacket.Id);  // Same source
        retransformed.Id.Should().NotBe(initialReading.Id);      // Different transformedId
    }

#### 4. Last-Packet Cache Maintains One Per Type

    [Fact]
    public void LastPacketCache_Should_MaintainOnePerPacketType()
    {
        var transformer = CreateTransformer();
        
        var obsPacket1 = CreateObservationPacket();
        var windPacket = CreateWindPacket();
        var obsPacket2 = CreateObservationPacket();
        
        transformer.OnRawPacketReceived(obsPacket1);
        transformer.OnRawPacketReceived(windPacket);
        transformer.OnRawPacketReceived(obsPacket2);
        
        var cache = transformer.GetLastPacketCache();
        cache[PacketEnum.Observation].Id.Should().Be(obsPacket2.Id);  // Latest observation
        cache[PacketEnum.Wind].Id.Should().Be(windPacket.Id);          // Wind packet preserved
    }

#### 5. Provenance Tracks Retransformation

    [Fact]
    public void Provenance_Should_MarkRetransformations()
    {
        var transformer = CreateTransformer();
        var rawPacket = CreateTestPacket();
        
        transformer.OnRawPacketReceived(rawPacket);
        var initial = GetLastPublishedReading();
        initial.Provenance.TransformerVersion.Should().Be("1.0");
        
        // Trigger retransformation
        settingsRepository.Set("/ui/units/temperature", "celsius");
        await settingsRepository.ApplyOverrides();
        
        var retransformed = GetLastPublishedReading();
        retransformed.Provenance.TransformerVersion.Should().Be("1.0-retransform");
    }

### Testing Framework
- xUnit: Primary test framework
- FluentAssertions: Readable assertions
- Moq: Mocking dependencies
- Microsoft.NET.Test.Sdk: Test runner

## Database Schema

### Provenance Table

    CREATE TABLE packet_provenance (
        raw_packet_id UUID PRIMARY KEY,
        transformed_id UUID,
        udp_receipt_time TIMESTAMPTZ NOT NULL,
        transform_start_time TIMESTAMPTZ,
        transform_end_time TIMESTAMPTZ,
        transform_duration_ms INTEGER,
        total_pipeline_ms INTEGER,
        packet_type VARCHAR(50),
        status VARCHAR(20),
        error_message TEXT,
        source_units JSONB,
        target_units JSONB,
        transformer_version VARCHAR(20),
        created_at TIMESTAMPTZ DEFAULT NOW()
    );
    
    CREATE INDEX idx_provenance_time ON packet_provenance(udp_receipt_time DESC);
    CREATE INDEX idx_provenance_type ON packet_provenance(packet_type, status);
    CREATE INDEX idx_provenance_status ON packet_provenance(status) WHERE status = 'Failed';
    CREATE INDEX idx_provenance_retransform ON packet_provenance(transformer_version) WHERE transformer_version LIKE '%-retransform';

### Weather Data Tables

Each packet type has its own table with raw_packet_id as foreign key:

    CREATE TABLE observation (
        id UUID PRIMARY KEY,
        raw_packet_id UUID REFERENCES packet_provenance(raw_packet_id),
        timestamp TIMESTAMPTZ NOT NULL,
        temperature NUMERIC(5,2),
        temperature_unit VARCHAR(10),
        humidity NUMERIC(5,2),
        pressure NUMERIC(6,2),
        pressure_unit VARCHAR(10),
        json_document_original JSONB,
        application_received_utc_timestampz TIMESTAMPTZ,
        created_at TIMESTAMPTZ DEFAULT NOW()
    );

## API for Diagnostics

### Query by COMB GUID

    // Get full lineage for a specific packet
    var lineage = provenanceTracker.GetLineage(combGuid);
    
    // Display in UI
    Console.WriteLine($"Packet: {lineage.RawPacketId}");
    Console.WriteLine($"Status: {lineage.Status}");
    Console.WriteLine($"Steps: {lineage.ProcessingSteps.Count}");
    foreach (var step in lineage.ProcessingSteps.OrderBy(s => s.Timestamp))
    {
        Console.WriteLine($"  [{step.Timestamp:HH:mm:ss.fff}] {step.StepName} ({step.Component})");
    }

### Query by Time Range

    // Leverages COMB GUID chronological sorting
    var readings = provenanceTracker.GetLineagesByTimeRange(startTime, endTime)
        .OrderBy(r => r.RawPacketId)  // COMB GUIDs sort chronologically
        .ToList();

### Query by Status

    // Find all failed transformations for debugging
    var failedTransforms = provenanceTracker.GetLineagesByStatus(DataStatus.Failed);
    
    foreach (var lineage in failedTransforms)
    {
        Console.WriteLine($"Failed: {lineage.RawPacketId}");
        foreach (var error in lineage.Errors ?? new())
        {
            Console.WriteLine($"  Error: {error.ErrorMessage}");
            Console.WriteLine($"  Component: {error.Component}");
        }
    }

## UI Integration

### Diagnostic View Features
- Real-time provenance display: Live feed of packet processing
- Performance graphs: Chart of transform durations over time
- Error log with lineage links: Click error → see full packet journey
- "Trace packet" feature: Click any reading → see complete pipeline
- Search by COMB GUID: Direct lookup of specific packets
- Retransformation history: View all transformations of a raw packet

### Performance Dashboard
- Moving average latencies: Real-time performance metrics
- Packet loss detection: Gap detection using COMB GUID sequence
- Transform failure rates: Percentage of failed conversions
- Database connection health: PostgreSQL availability status
- Buffer status: Number of queued messages when DB unavailable
- Retransformation metrics: Track frequency and performance of settings-triggered retransforms

### Example UI Code

    // In ViewModel
    public void OnReadingClicked(IWeatherReading reading)
    {
        var lineage = provenanceTracker.GetLineage(reading.SourcePacketId);
        
        if (lineage != null)
        {
            var traceView = new ProvenanceTraceView
            {
                PacketId = lineage.RawPacketId,
                Steps = lineage.ProcessingSteps,
                Performance = new
                {
                    TransformTime = reading.Provenance.TransformDuration,
                    TotalTime = reading.Provenance.TotalPipelineTime,
                    IsRetransform = reading.Provenance.TransformerVersion.Contains("retransform")
                }
            };
            
            await Navigation.PushAsync(traceView);
        }
    }

## Documentation Standards

### Code Comments
- XML documentation on all public APIs
- Performance characteristics noted (e.g., O(n) complexity)
- Thread-safety guarantees stated explicitly
- Example usage in doc comments for complex APIs

### Example:

    /// <summary>
    /// Creates a new COMB GUID with embedded timestamp for chronological ordering.
    /// </summary>
    /// <returns>A GUID where the last 6 bytes contain the current UTC timestamp.</returns>
    /// <remarks>
    /// Thread-safe. Can be called concurrently from multiple threads.
    /// Performance: O(1), approximately 100ns per call.
    /// COMB GUIDs are sortable chronologically and database-friendly.
    /// </remarks>
    public static Guid CreateCombGuid() { ... }

### Architecture Decision Records (ADRs)

#### ADR-001: Why COMB GUIDs
- Context: Need globally unique IDs with chronological ordering
- Decision: Use COMB GUIDs (timestamp in last 6 bytes)
- Consequences: Better database performance, traceability, sortability

#### ADR-002: RedStar.Amounts for Type Safety
- Context: Need type-safe unit handling with automatic conversions
- Decision: Use RedStar.Amounts library for all measurements
- Consequences: Eliminates unit conversion bugs, cleaner API, testable

#### ADR-003: Provenance Tracking Strategy
- Context: Need complete traceability for diagnostics
- Decision: Embed provenance in all transformed readings
- Consequences: Slight memory overhead, excellent debuggability

#### ADR-004: Testing Framework Selection
- Context: Need comprehensive test coverage
- Decision: Use xUnit + FluentAssertions + Moq
- Consequences: Readable tests, standard .NET tooling, CI/CD compatible

#### ADR-005: Event-Driven Settings with Last-Packet Cache
- Context: Need immediate UI feedback when settings change, but weather data arrives infrequently
- Decision: Cache last packet per type and retransform on settings change using event-driven pattern
- Consequences: 
  - Pro: Immediate feedback for all packet types regardless of arrival frequency
  - Pro: UI decoupled from transformation logic (pure push pattern)
  - Pro: Complete provenance trail for retransformations
  - Con: Small memory overhead for packet cache (4 packets max)
  - Con: Additional complexity in transformer lifecycle

## Future Enhancements

### Planned Features
1. Persistent Provenance: Store lineage in database for long-term analysis
2. Distributed Tracing: OpenTelemetry integration for multi-service tracing
3. ML-based Anomaly Detection: Use provenance data to detect unusual patterns
4. Performance Regression Testing: Automated alerts on performance degradation
5. Export to Analytics: Stream provenance data to time-series database
6. Retransformation History UI: Visualize all transformations of a single raw packet
7. Settings Change Audit: Track who changed what setting when

### Research Areas
1. COMB GUID Collisions: Probability analysis and mitigation strategies
2. Compression: Efficient storage of large provenance datasets
3. Real-time Alerting: Push notifications on critical failures
4. Visualization: Interactive provenance graph explorer
5. Smart Caching: LRU cache with configurable size for last-packet storage

---

## Appendix: Data Status States

    public enum DataStatus
    {
        Received,           // UDP packet received, COMB GUID assigned
        Parsed,             // JSON parsed successfully, packet type identified
        Transformed,        // Converted to typed reading with RedStar.Amounts
        Retransformed,      // Transformed again due to settings change
        Persisted,          // Saved to PostgreSQL database
        Failed,             // Processing failed at some stage
        Buffered,           // Waiting in buffer due to DB unavailability
        Displayed           // Rendered in UI to user
    }

## Appendix: Key Performance Targets

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| UDP → Transform | < 10ms | TBD | 🟡 Measuring |
| Transform → DB | < 50ms | TBD | 🟡 Measuring |
| End-to-End | < 100ms | TBD | 🟡 Measuring |
| Retransform Time | < 5ms | TBD | 🟡 Measuring |
| Settings Change Latency | < 50ms | TBD | 🟡 Measuring |
| Packet Loss | < 0.1% | TBD | 🟡 Measuring |
| Transform Failures | < 0.01% | TBD | 🟡 Measuring |

---

Last Updated: 2026-01-06  
Version: 1.0  
Authors: MetWorks Development Team  
Status: Living Document - Updated as architecture evolves