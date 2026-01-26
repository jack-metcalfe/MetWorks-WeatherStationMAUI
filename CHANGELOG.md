## Unreleased

### Added
- `InstanceIdentifier` service for per-installation GUID persistence via settings overrides.
- `ISettingProvider.SaveValueOverride` and `SettingProvider` support for atomic YAML overrides in app-local storage.
- Test project `MetWorks.Common.Settings.Tests` with tests for settings override persistence, `InstanceIdentifier` behavior, and `EventRelayPath` prefix registration.
- Documentation: `SETTINGS.md` updates, `DEVELOPER_GUIDE.md` settings guidance, and updates in ToDo/plan docs.

### Changed
- Code updated to use `LookupDictionaries.<Group>GroupSettingsDefinition.BuildSettingPath(...)` and `.BuildGroupPath()` to avoid hard-coded `/services/...` strings and to enable correct prefix-based subscriptions.
- `SensorReadingTransformer` updated to register/unregister using the canonical group prefix.

### Notes
- The DDI generator should be run to include `TheInstanceIdentifier` in the generated registry wiring; the generator input `WeatherStationMaui.yaml` was updated.
- Overrides file location: `%LocalAppData%/MetWorks-WeatherStationMAUI/data.settings.yaml` (configurable in `SettingProvider` constructor for tests).

Signed-off-by: GitHub Copilot
