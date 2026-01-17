# MetWorks Weather Station (developer README)

Purpose
  MetWorks is a .NET 10 weather-monitoring solution (MAUI UI primary target) that receives UDP broadcasts (Tempest), transforms them into typed domain readings with unit-safety, provides provenance tracking, and optionally persists to PostgreSQL.

Quick facts
  - Target: .NET 10, primary runtime target: Android (development on Windows supported)
  - UI: .NET MAUI (weather-station-maui)
  - Key libs: RedStar.Amounts (unit system), metworks_services (transformations), udp_packets (parsing)
  - DI: Declarative DI (external repo) — preferred over MAUI built-in DI for this project
  - Provenance: COMB GUIDs used for source & transformed readings to preserve lineage

Prerequisites
  - .NET 10 SDK installed
  - Visual Studio 2026 recommended (or latest VS that supports .NET 10)
  - Android SDK if developing for Android
  - Optional: PostgreSQL for persistence

Repo layout (high level)
  - `src/weather-station-maui/` — MAUI UI project + ViewModels (desktop/Android)
  - `src/udp_packets/` — packet DTOs and `TempestPacketParser`
  - `src/metworks_services/` — `WeatherDataTransformer`, provenance integration
  - `src/metworks_models/` — domain interfaces and reading records
  - `src/settings/` — `SettingRepository`, providers, overrides
  - `src/raw_packet_record_type_in_postgres_out/` — optional Postgres persistence sink
  - `src/basic_event_relay/` — lightweight pub/sub used across solution
  - `src/RedStar.Amounts/` — unit system and weather extensions

Quick build & run (developer flow)
  1. Create change branch:
     `git checkout -b UpdateDocToImpl`
  2. Restore and build:
     `dotnet restore`
     `dotnet build -c Debug`
  3. From Visual Studio 2026: Open solution at repo root and run `weather-station-maui` target.
  4. Running without Postgres: the app degrades gracefully; you may disable DB persistence in settings (see `docs/SETTINGS.md`).

Developing & debugging tips
  - Use the `BasicEventRelay` to observe internal events (transformations, settings events).
  - Transformer & parser are the best starting points: `src/metworks_services/WeatherDataTransformer.cs` and `src/udp_packets/TempestPacketParser.cs`.
  - Settings are injected via `SettingRepository.InitializeAsync(...)` — prefer InitializeAsync-based patterns (see `docs/DI_AND_INITIALIZEASYNC.md`).

Where to start reading docs
  - Start with `docs/ARCHITECTURE.md` (detailed diagram + rationale)
  - Then `docs/DEVELOPER_GUIDE.md` for build and debug workflows
  - `docs/SETTINGS.md` for runtime configuration and override behavior

Contact / external reference
  - Declarative DI generator: https://github.com/jack-metcalfe/MetWorks-DeclarativeDI