# Settings

Location: `src/MetWorks_Resource_Store/data/settings.yaml` and `SettingProvider` in `src/MetWorks_Common_Settings`

Overview
- Settings are defined in the embedded `data.settings.yaml` resource and exposed at runtime via `ISettingProvider` / `ISettingRepository`.
- The canonical path format for settings is `/services/{group}/{settingName}`. Use the provided helpers to avoid hard-coded strings:
  - `LookupDictionaries.<Group>GroupSettingsDefinition.BuildSettingPath("{settingName}")` — gets full path for a specific setting.
  - `LookupDictionaries.<Group>GroupSettingsDefinition.BuildGroupPath()` — gets the canonical group prefix (e.g. `/services/unitOfMeasure`) for prefix-based subscriptions.

Accessing settings (example)
 - Read a typed value:
 ```csharp
 var tempUnit = iSettingRepository.GetValueOrDefault<string>(
     LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(SettingConstants.UnitOfMeasure_airTemperature)
 );
 ```

Reacting to changes (example)
 - Register a prefix-based handler that will receive any setting under the unit-of-measure group:
 ```csharp
 var prefix = LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildGroupPath();
 iSettingRepository.IEventRelayPath.Register(prefix, OnUnitSettingChanged);
 ```

Persistence and overrides
- The provider loads definitions from the embedded `data.settings.yaml`. Runtime overrides are persisted to an overrides file under the app-local directory (by default `%LocalAppData%/MetWorks-WeatherStationMAUI/data.settings.yaml`).
- `SettingProvider` exposes `SaveValueOverride(string path, string value)` to write a single-value override atomically.

Per-installation identifier
- A stable per-installation GUID setting is defined at `/services/instance/installationId`. The `InstanceIdentifier` service reads or generates this GUID and persists it via the settings override mechanism. Use `IInstanceIdentifier` (resolve via DI) to access or rotate the id:
```csharp
var installationId = serviceProvider.GetRequiredService<IInstanceIdentifier>();
var id = installationId.GetOrCreateInstallationId();
// rotate
installationId.ResetInstallationId();
// or set explicitly
installationId.SetInstallationId("...new-guid...");
```

Testing guidance
- Tests that need to persist overrides can construct `SettingProvider` with a temporary overrides directory via the `SettingProvider(string? overridesBaseDirectory)` constructor so they do not touch real AppData.

Security
- Do not store secrets in the public settings resource. Mark secret definitions with `isSecret: true` in `data.settings.yaml`. For production secrets prefer secure platform stores and reference them from the settings provider at runtime.
