MetWorks Weather Station - Implementation Plan
Overview
This document outlines the complete implementation plan for the MetWorks Weather Station MAUI application. The app will display real-time weather data from Tempest weather stations on Android tablets/phones and Windows devices, with device-specific optimized layouts.
---
Architecture Summary
Core Philosophy
•	Android-first design targeting tablets (primary) and phones (secondary)
•	Device-specific layouts with pixel-perfect optimization for each resolution
•	Orientation-aware with automatic page swapping on rotation
•	Tap-only navigation to prevent accidental menu activation
•	Event-driven data flow using BasicEventRelay pub/sub pattern
Technology Stack
•	.NET 10 / MAUI
•	System.Text.Json for packet deserialization
•	RedStar.Amounts for type-safe unit conversions
•	PostgreSQL for historical data (optional)
•	Tempest UDP broadcasts for real-time data
---
Component Breakdown
1. Navigation Structure: Shell with Locked Flyout
Purpose: Provide app-wide navigation without accidental swipe-to-open behavior
Key Features:
•	Hamburger menu accessible via tap only (no swipe gesture)
•	Full-screen weather display as default view
•	Purple/Gold Vikings color scheme
•	Menu sections: Live Weather, History, Forecast, Radar, Settings, About
Behavior:
•	FlyoutBehavior set to "Locked" prevents swipe-from-left gesture
•	Hamburger icon (☰) always visible in top-left corner
•	Tapping icon toggles menu open/closed
•	Automatic menu close after navigation selection
User Experience:
•	Solves accidental activation problem (hair, clothing touches)
•	Clean, professional kiosk-style appearance
•	Easy access when needed via deliberate tap
---
2. Device Selection System
Purpose: Automatically detect device characteristics and load the optimal page layout
Components:
A. DeviceProfile (Record)
Properties:
•	DeviceName (friendly identifier)
•	WidthPixels, HeightPixels (native resolution)
•	Density (PPI/DPI)
•	Platform (Android, Windows, iOS)
•	DeviceModel, Manufacturer (optional hardware identifiers)
•	ViewTypeName (which page to load)
•	PreferredOrientation (Portrait, Landscape, or Auto)
•	Calculated properties: DiagonalInches, LogicalWidth, LogicalHeight
Purpose: Represents a specific device configuration with its optimal view
B. DeviceViewRegistry (Static Class)
Purpose: Central registry of all known device profiles
Contains:
•	List of pre-configured device profiles
•	Common devices: Pixel 7, Galaxy S23, Surface Pro, iPad Pro
•	Desktop displays: 1920x1200, 4K displays
•	Specialty displays: ultra-wide, portrait kiosks
Matching Logic:
1.	Try exact match (resolution + density + platform)
2.	Try device model/manufacturer match
3.	Find closest match by aspect ratio and resolution
4.	Fall back to default responsive page
Extensibility:
•	New devices can be registered at runtime
•	No code changes needed to support new hardware
C. DeviceViewSelector (Static Class)
Purpose: Query current device and return appropriate ContentPage
Process:
1.	Read DeviceDisplay.Current.MainDisplayInfo
2.	Extract width, height, density, orientation
3.	Read DeviceInfo.Current for model/manufacturer
4.	Query DeviceViewRegistry for best match
5.	Instantiate and return matched ContentPage
6.	Log device detection results for debugging
Handles Rotation:
•	Subscribes to MainDisplayInfoChanged event
•	Detects orientation changes
•	Swaps to landscape/portrait variant of current page
•	Preserves ViewModel/BindingContext during swap
---
3. Page Structure
Device-Specific Weather Pages (ContentPage)
Purpose: Pixel-perfect layouts optimized for specific resolutions
Pattern: Separate pages for each resolution and orientation
•	Portrait: 1080x2400, 1440x2304, 1812x2176, 970x2485
•	Landscape: 2400x1080, 2304x1440, 2176x1812, 2485x970, 1920x1200
Current State:
•	Existing files are ContentView (incorrect)
•	Need conversion to ContentPage
•	Need namespace/ViewModel binding updates
Layout Characteristics:
•	Grid-based with star sizing for proportional scaling
•	Large fonts for distance viewing (328pt for time, 265pt for values)
•	Purple background with gold accents
•	Minimal chrome, maximum data visibility
Data Displayed:
•	Current time (day of week, date, HH:MM)
•	Temperature with unit symbol
•	Wind speed and direction (cardinal + degrees)
•	Humidity percentage
•	UV index
Feature Pages (ContentPage)
Settings Page:
•	Unit preferences (temperature, wind, pressure, distance)
•	Station configuration (serial number, location)
•	Database settings (PostgreSQL connection string)
•	Network settings (UDP port)
•	Display preferences
•	Test/validation buttons
History Page:
•	Date range selector
•	Chart/graph visualization (temperature trends, rainfall totals)
•	Data table view
•	Export functionality
•	Pagination for large datasets
•	Query PostgreSQL for historical readings
Forecast Page:
•	Integration with Tempest forecast API
•	Multi-day forecast cards
•	Hourly predictions
•	Weather alerts
•	Icons and condition descriptions
Radar Page:
•	Real-time radar imagery from Tempest API
•	Animation/loop controls
•	Zoom and pan functionality
•	Timestamp overlay
•	Refresh controls
About Page:
•	App version and build info
•	Station status indicators
•	Service health checks
•	Debug information
•	Credits and licenses
---
4. ViewModel Architecture
LargeFormatWeatherViewModel
Purpose: Provide data binding for weather display pages
Responsibilities:
•	Subscribe to BasicEventRelay for IObservationReading and IWindReading events
•	Update observable properties when new data arrives
•	Provide separated value/unit properties for flexible display
•	Manage time display with minute-aligned updates
•	Calculate derived values (wind cardinal direction)
Properties:
•	CurrentObservation, CurrentWind (domain models)
•	TemperatureValue, TemperatureUnit (separated for layout)
•	WindSpeedValue, WindSpeedUnit, WindDirectionCardinal, WindDirectionDegrees
•	HumidityValue, UvIndexValue
•	TimeDayOfWeek, TimeDateDisplay, TimeDisplay (3-line time)
•	Boolean flags: HasObservationData, HasWindData
Clock Timer Logic:
•	Initial display shows current time immediately
•	Calculate delay until next minute boundary
•	First timer tick at top of next minute
•	Reconfigure timer for 60-second interval
•	Continue ticking every minute
•	Ensures minute value updates exactly at :00 seconds
Thread Safety:
•	Event handlers use MainThread.BeginInvokeOnMainThread
•	Ensures UI updates happen on correct thread
•	Prevents cross-thread exceptions
Disposal:
•	Unsubscribes from event relay
•	Stops and disposes clock timer
•	Implements IDisposable pattern
---
5. Data Flow Pipeline
Stage 1: UDP Reception
•	Tempest weather station broadcasts UDP packets
•	UdpInRawPacketRecordTypedOut listens on port 50222
•	Wraps JSON in IRawPacketRecordTyped with PacketEnum classification
•	Publishes to BasicEventRelay
Stage 2: Packet Deserialization
•	PacketFactory receives IRawPacketRecordTyped
•	Determines packet type (Observation, Wind, Lightning, Precipitation)
•	Deserializes JSON to strongly-typed DTO using System.Text.Json
•	DTOs use required properties with JsonPropertyName attributes
•	No parameterized constructors (learned from ObservationDto fix)
Stage 3: Domain Transformation
•	WeatherDataTransformer subscribes to raw packet events
•	Converts DTOs to domain models (IObservationReading, IWindReading)
•	Applies unit conversions via RedStar.Amounts
•	Adds provenance tracking (source packet ID, timestamps)
•	Publishes domain models to BasicEventRelay
Stage 4: UI Update
•	LargeFormatWeatherViewModel subscribes to domain model events
•	Updates observable properties
•	UI bindings trigger automatic refresh
•	No manual UpdateSource needed
Stage 5: Optional Persistence
•	RawPacketRecordTypedInPostgresOut subscribes to events
•	Stores readings in PostgreSQL
•	App continues without database if unavailable
•	Auto-reconnection on database availability
---
6. Color Resources
WeatherColors.xaml:
•	PurpleVikings: #4F2683 (primary background)
•	GoldVikings: #FFC62F (accent color for headers, labels)
Usage:
•	Background: PurpleVikings
•	Headers and unit labels: GoldVikings
•	Large values: White
•	Contrast ratio optimized for distance viewing
Merged Into App Resources:
•	Separate file to avoid modifying standard Colors.xaml
•	Loaded via MergedDictionaries in App.xaml
---
7. Android-Specific Configuration
AndroidManifest.xml:
•	Location: Platforms/Android/AndroidManifest.xml
•	Permissions required:
•	INTERNET (UDP listening, API calls)
•	ACCESS_NETWORK_STATE (connection monitoring)
•	ACCESS_WIFI_STATE (WiFi network info)
Target SDK: Android API 21+ (Android 5.0 Lollipop)
Orientation Handling:
•	App responds to orientation changes
•	DeviceViewSelector swaps pages automatically
•	No need to lock orientation
---
Implementation Phases
Phase 1: Foundation
1.	Create DeviceProfile, DeviceViewRegistry, DeviceViewSelector
2.	Create WeatherColors.xaml resource file
3.	Update App.xaml to merge WeatherColors
4.	Create AppShell with locked flyout navigation
5.	Update App.xaml.cs to use AppShell
Phase 2: Page Conversion
1.	Convert existing MainView files from ContentView to ContentPage
2.	Update namespaces from TempestMonitor to MetWorksWeather
3.	Update ViewModel bindings to LargeFormatWeatherViewModel
4.	Create code-behind files with ViewModel initialization
5.	Test each page individually
Phase 3: Device Detection Integration
1.	Wire DeviceViewSelector into AppShell
2.	Implement orientation change handler
3.	Register all device profiles in registry
4.	Test on multiple emulators/devices
5.	Fine-tune matching algorithm
Phase 4: Feature Pages
1.	Create SettingsPage with forms
2.	Create HistoryPage with chart placeholders
3.	Create ForecastPage with API integration
4.	Create RadarPage with image display
5.	Create AboutPage with diagnostics
Phase 5: Android Deployment
1.	Create AndroidManifest.xml with permissions
2.	Test on Android emulator (Pixel 7, Galaxy tablet)
3.	Test on physical Android device
4.	Verify UDP reception on Android
5.	Verify orientation handling
Phase 6: Polish
1.	Add icons for flyout menu items
2.	Implement loading indicators
3.	Add error handling and retry logic
4.	Create mock data service for testing
5.	Performance optimization
---
Testing Strategy
Device Testing Matrix
Emulators:
•	Pixel 7 (1080x2400) - Portrait
•	Pixel 7 (2400x1080) - Landscape
•	Generic 10" Tablet (1920x1200) - Landscape
•	Surface Pro simulator (2176x1812) - Both orientations
Physical Devices:
•	Developer's Android tablet
•	Wall-mounted display (current production device)
•	Test phone for on-the-go monitoring
Test Scenarios:
1.	Cold start - verify correct page loads
2.	Rotation - verify page swaps correctly
3.	Menu navigation - verify all pages load
4.	Live data - verify UDP reception and display updates
5.	Long-running - verify no memory leaks, clock accuracy
6.	Network loss - verify graceful handling
7.	Database unavailable - verify app continues
---
Known Issues and Solutions
Issue 1: Accidental Menu Activation
Problem: Hair, clothing touching screen edge opens menu Solution: FlyoutBehavior = Locked (tap-only access)
Issue 2: ObservationDto Deserialization Failure
Problem: Constructor parameters didn't match JSON property names Solution: Use required properties instead of constructors, let JsonPropertyName attributes handle mapping
Issue 3: Minute Updates Not Aligned
Problem: Clock shows 11:30 but updates at 11:30:37 Solution: Calculate delay to next minute boundary, use one-shot timer for first tick, then 60-second repeating timer
Issue 4: ContentView vs ContentPage Confusion
Problem: Existing MainView files are ContentView (can't navigate) Solution: Convert all to ContentPage, treat device-specific layouts as full pages
---
Future Enhancements
Potential Features
•	Multiple weather stations (multiple devices, comparative display)
•	Weather alerts and notifications
•	Customizable dashboards (user-selected widgets)
•	Dark/light theme toggle
•	Screen saver mode (dim display after inactivity)
•	Voice announcements (weather updates via TTS)
•	Widget for Android home screen
•	Export reports as PDF
•	Integration with smart home systems
Performance Optimizations
•	Lazy loading of historical data
•	Image caching for radar
•	Background service for data collection
•	Incremental chart rendering
---
Documentation Updates Needed
Current State
•	Architecture documentation created (this document)
•	ObservationDto deserialization fix documented
To Be Created
•	API reference for domain models
•	ViewModel patterns documentation
•	UI component library guide
•	Deployment guide (Android APK signing)
•	User manual
•	Troubleshooting guide
---
Success Criteria
Must Have (MVP)
•	✅ App runs on Android tablets and phones
•	✅ Device-specific layouts load automatically
•	✅ Real-time weather data displays correctly
•	✅ Navigation works without accidental activation
•	✅ Orientation changes handled smoothly
•	✅ Unit conversions work (F/C, mph/km/h)
Should Have (V1.0)
•	✅ Settings page functional
•	✅ Historical data charting
•	✅ Forecast integration
•	✅ Radar imagery
•	✅ Stable on long-running wall display
Nice to Have (V1.1+)
•	Multiple station support
•	Customizable layouts
•	Themes and appearance options
•	Export functionality
•	Advanced analytics
---
End of Implementation Plan
This document serves as the blueprint for the complete MetWorks Weather Station implementation. All code generation will follow this plan.
