MetWorks Weather Station - Architecture Documentation
System Overview
MetWorks is a .NET 10 weather monitoring application that receives UDP broadcasts from Tempest weather stations, transforms the data into typed domain models with user-preferred units, optionally persists to PostgreSQL, and displays real-time weather data through a .NET MAUI cross-platform UI.
---
Data Flow Pipeline
UDP Packets → Raw Records → Typed Packets → Domain Models → UI/Database
Stage 1: UDP Reception
·	Component: UdpInRawPacketRecordTypedOut
·	Input: Raw UDP JSON broadcasts from Tempest weather station
·	Output: IRawPacketRecordTyped with PacketEnum classification
·	Key Classes: UDP listener, packet type detector
Stage 2: Packet Deserialization
·	Component: UdpPackets
·	Input: IRawPacketRecordTyped (JSON string + packet type)
·	Output: Strongly-typed DTOs (ObservationDto, WindDto, LightningDto, PrecipitationDto)
·	Key Classes:
·	PacketFactory - Deserializes JSON to appropriate DTO
·	PacketDtoBase - Base class with serial_number, type, hub_sn
·	Individual DTOs with [JsonPropertyName] mappings
Critical Design: DTOs use required properties instead of constructors to allow [JsonPropertyName] attributes to map snake_case JSON to PascalCase C# properties.
Stage 3: Domain Transformation
·	Component: MetWorksServices.WeatherDataTransformer
·	Input: IRawPacketRecordTyped via BasicEventRelay
·	Output: Domain models (IObservationReading, IWindReading, etc.)
·	Key Features:
·	Converts Tempest's metric units to user-preferred units using RedStar.Amounts
·	Adds provenance tracking (source packet ID, timestamps, transformation metadata)
·	Publishes transformed readings back to BasicEventRelay
Stage 4: Event Distribution
·	Component: BasicEventRelay
·	Pattern: Lightweight pub/sub wrapper around CommunityToolkit.Mvvm messenger
·	Subscribers:
·	MAUI UI ViewModels
·	PostgreSQL persistence service
·	Any other observers
Stage 5: Persistence (Optional)
·	Component: RawPacketRecordTypedInPostgresOut
·	Database: PostgreSQL
·	Graceful Degradation: Application continues without database if unavailable
·	Auto-reconnection: Monitors database availability and reconnects automatically
Stage 6: UI Presentation
·	Component: weather-station-maui
·	Framework: .NET MAUI
·	Binding: ViewModels subscribe to BasicEventRelay events, update observable properties
·	Display: Real-time weather data with user-configured units
---
Project Structure
Core Infrastructure
Logging - File-based structured logging with emoji indicators
interfaces - Shared interfaces across all projects
DdiRegistry - Dependency injection registry pattern, service lifecycle management
Settings - Application configuration with override system
Utility - Common utilities and helpers
enum_definition - Shared enumerations (PacketEnum: Wind, Lightning, Observation, Precipitation)
StaticDataStore - In-memory data storage
SettingsOverrideStructures + MauiSettingOverrideProviders - Platform-specific settings override mechanism
---
Data Processing
UdpInRawPacketRecordTypedOut
·	Listens on UDP port for Tempest broadcasts
·	Wraps raw JSON in IRawPacketRecordTyped with packet classification
·	Publishes to BasicEventRelay
UdpPackets
·	Internal DTOs for deserializing Tempest JSON
·	PacketFactory with System.Text.Json deserialization
·	Four packet types: Observation, Wind, Lightning, Precipitation
·	Pattern: required properties with [JsonPropertyName] for JSON mapping
BasicEventRelay
·	Singleton event bus for pub/sub messaging
·	Type-safe message passing
·	Decouples producers from consumers
MetWorksServices
·	WeatherDataTransformer - Core transformation service
·	Converts DTOs to domain models
·	Applies unit conversions via RedStar.Amounts
·	Tracks data provenance
MetWorksModels
·	Domain interfaces: IObservationReading, IWindReading, IPrecipitationReading, ILightningReading
·	Rich domain objects with typed units (not primitive doubles)
RawPacketRecordTypedInPostgresOut
·	PostgreSQL persistence
·	Optional - app degrades gracefully without it
·	Auto-reconnection support
---
Unit System
RedStar.Amounts - Unit of measurement framework with conversion support
RedStar.Amounts.StandardUnits - Temperature, pressure, speed, length, etc.
RedStar.Amounts.WeatherExtensions - Weather-specific aliases (mph, Fahrenheit, inHg)
weather-station-units - Application-specific unit configurations
Key Pattern: All measurements stored as Amount objects (value + unit), not raw doubles. Enables safe conversions and clear intent.
---
UI Layer
weather-station-maui - .NET MAUI cross-platform application
Startup Sequence (from StartupInitializer.cs):
1.	Register RedStar.Amounts unit system
2.	Register weather unit aliases
3.	Create service registry
4.	Initialize all services (UDP listener, transformers, database)
5.	Verify critical services (logger, UDP settings, UDP listener)
6.	Gracefully handle PostgreSQL unavailability
7.	Optional: Start mock weather service for development
Critical Services (must be available):
·	File logger
·	UDP settings repository
·	UDP listener
Optional Services:
·	PostgreSQL (app continues without it)
---
Key Design Patterns
1. Event-Driven Architecture
·	All components communicate via BasicEventRelay
·	Loose coupling between layers
·	Easy to add new subscribers
2. Unit-Safe Calculations
·	No raw doubles for measurements
·	All values are Amount objects with explicit units
·	Compile-time unit safety
3. Graceful Degradation
·	Application continues if database unavailable
·	Auto-reconnection for transient failures
·	Clear logging of degraded state
4. Provenance Tracking
·	Every transformed reading links back to source packet
·	Transformation timestamps recorded
·	Unit conversion history preserved
5. Service Registry Pattern
·	Centralized dependency management
·	Explicit initialization order
·	Lifecycle management (create → initialize → dispose)
---
JSON to Domain Model Example
Incoming JSON (Tempest obs_st packet):
{
  "serial_number": "ST-00168579",
  "type": "obs_st",
  "hub_sn": "HB-00172311",
  "obs": [[1767848491, 0.01, 0.28, 0.54, 61, 3, 1000.09, 9.83, 88.54, 0, 0.00, 0, 0.000000, 0, 0, 0, 2.689, 1]],
  "firmware_revision": 179
}

Stage 1: Wrapped in IRawPacketRecordTyped with PacketEnum.Observation
Stage 2: Deserialized to ObservationDto with parsed array as ObservationReadingDto
Stage 3: Transformed to IObservationReading with:
•	Temperature converted from Celsius to user preference (e.g., Fahrenheit)
•	Pressure converted from millibar to user preference (e.g., inHg)
•	Humidity as percentage
•	UV index
•	Solar radiation
•	Provenance metadata attached
Stage 4: Published to BasicEventRelay as IObservationReading
Stage 5: UI ViewModel receives event, updates observable properties, triggers UI refresh
---
Configuration & Settings
UDP Settings
•	Port: Configurable (default 50222 for Tempest)
•	Broadcast address listening
Unit Preferences
•	Temperature: Celsius, Fahrenheit, Kelvin
•	Pressure: millibar, inHg, kPa
•	Wind speed: m/s, mph, km/h, knots
•	Distance: meters, kilometers, miles, feet
Database Settings
•	PostgreSQL connection string
•	Optional - app works without database
•	Auto-reconnection interval
---
Current State & Next Steps
Working Components
•	UDP packet reception
•	JSON deserialization (fixed with ObservationDto refactor)
•	Event relay system
•	Unit conversion system
•	Service initialization with graceful degradation
Needs Implementation
•	Large-format UI for 20-foot viewing distance (3-column grid layout)
•	ViewModel for weather display
•	Color resources for UI theme
•	Mock weather service activation (currently commented out)
Documentation Status
•	This architecture document is now current
•	Need to document: API reference for domain models, ViewModel patterns, UI component library
---
Architecture Last Updated: Based on current codebase analysis during ObservationDto deserialization fix.
