# DEVELOPER GUIDE

Audience: developers who will build, debug, extend, and maintain the WeatherStation MAUI service.

Prerequisites
- .NET 10 SDK
- Visual Studio 2022/2023 with MAUI workloads (Android target is primary for runtime testing)
- Git (repo root contains weather_station_maui.sln)

Quick start
1. Checkout branch: git checkout UpdateDocToImpl (or create it locally if needed).
2. Build: dotnet build weather_station_maui.sln -c Debug
3. Run Android headless/debug target via Visual Studio or `dotnet build` + emulator.

Code locations
- UDP parsing: src/udp_packets/
- Transformation logic: src/metworks_services/
- Settings & configuration: src/settings/

Conventions
- Declarative DI components must have a public parameterless constructor and implement Task<bool> InitializeAsync(...) to receive runtime services (see DI_AND_INITIALIZEASYNC.md).
- Settings: use `LookupDictionaries.<Group>GroupSettingsDefinition.BuildSettingPath(name)` and `BuildGroupPath()` to avoid hard-coded `/services/...` strings. Register for setting-change messages with `IEventRelayPath.Register(groupPrefix, handler)` where `groupPrefix` comes from `BuildGroupPath()`.
- The per-installation GUID is exposed via `IInstanceIdentifier`. Add it to DDI input if you want the generated registry to create and initialize it. Example entry in the generator YAML:

  - name: "TheInstanceIdentifier"
    class: "MetWorks.InstanceIdentifier.InstanceIdentifier"
    exposeToMauiDi: true
    assignment:
      - name: "iLogger"
        instance: "TheLoggerFile"
      - name: "iSettingProvider"
        instance: "TheSettingProvider"

Note about DDI instance ordering
- The order of `instance:` entries in the DDI input YAML determines the generated registry `CreateAll()` and `InitializeAllAsync()` ordering. Ordering is significant when one instance depends on another during initialization. Ensure that dependencies are listed earlier than dependents. The generator will emit code in YAML order; the YAML author is responsible for ordering.

Reporting issues: open a concise PR that updates architecture/docs and includes minimal repro steps for runtime behavior changes.
