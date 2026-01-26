Summary of changes: 

- Introduced per-installation instance identifier service
  - New project: `src/MetWorks_InstanceIdentifier`
  - `IInstanceIdentifier` and `InstanceIdentifier` implemented; follows Declarative DI pattern (parameterless ctor + `InitializeAsync(ILogger, ISettingProvider)`).
  - `InstanceIdentifier` reads/generates a GUID at `/services/instance/installationId` and persists it via settings override.

- Settings persistence and provider improvements
  - `ISettingProvider.SaveValueOverride(string path, string value)` added to interface.
  - `SettingProvider` persists single-value overrides to an atomic YAML file under `%LocalAppData%/MetWorks-WeatherStationMAUI/data.settings.yaml` (injectable overrides base for tests).
  - `SettingProvider` supports constructor injection of overrides directory for tests.

- EventRelay and settings usage
  - Use `LookupDictionaries.<Group>GroupSettingsDefinition.BuildSettingPath(name)` and `.BuildGroupPath()` to avoid hard-coded `/services/...` strings.
  - `SensorReadingTransformer` updated to register/unregister with `BuildGroupPath()` for unit-of-measure setting change notifications.

- Tests
  - New test project: `src/tests/MetWorks.Common.Settings.Tests`
  - Tests added for `SettingProvider` override persistence and `InstanceIdentifier` behavior.
  - Added `EventRelayPathTests` to validate prefix-based registration and message delivery.

- Documentation
  - `SETTINGS.md` and `DEVELOPER_GUIDE.md` updated with canonical settings API guidance and examples.
  - `WeatherStationMaui.yaml` updated to include `TheInstanceIdentifier` so the DDI generator can wire it.
  - ToDo and plan docs updated to reflect the changes and further tasks.

Rationale
- Keep settings usage DRY and centralized via `LookupDictionaries` so path formats are not duplicated.
- Reuse settings provider for per-installation persisted data rather than adding a new storage mechanism.
- Provide a declarative, testable InstanceIdentifier service that can be initialized by the generated DDI.

Notes
- The DDI generator must be run externally to emit the registry wiring; the YAML snippet is included.
- I did not modify generated `.g.cs` files directly.

How to validate locally
1. dotnet build
2. dotnet test src/tests/MetWorks.Common.Settings.Tests
3. Run the MAUI app and verify the local overrides file: `%LocalAppData%/MetWorks-WeatherStationMAUI/data.settings.yaml` contains `/services/instance/installationId` after first run.

Files changed/added (high-level)
- src/MetWorks_InstanceIdentifier/*
- src/MetWorks_Common_Settings/SettingProvider.cs
- src/MetWorks_Constants/* (added BuildGroupPath)
- src/MetWorks_Ingest_Transformer/SensorReadingTransformer.cs
- src/tests/MetWorks.Common.Settings.Tests/*
- src/MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs/* (docs updates, YAML)

If you want, I can also:
- Add a small developer CLI for rotate/reset of installation id.
- Add a debug-only MAUI page/menu to rotate/reset the installation id.
- Run an additional docs sweep and replace all textual `/services/...` references with canonical API examples.

Signed-off-by: GitHub Copilot
