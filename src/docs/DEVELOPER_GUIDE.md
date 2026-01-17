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
- Keep docs in src/docs and update ARCHITECTURE.md when structural changes occur.

Reporting issues: open a concise PR that updates architecture/docs and includes minimal repro steps for runtime behavior changes.
