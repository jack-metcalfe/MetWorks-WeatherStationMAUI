# 2026-01-28 UI Selector Schema (Overrides + Variants + Screen Classes)

## Purpose
Document the proposed data-driven selection mechanism for choosing a concrete `ContentView` implementation for a given logical content key, while keeping the selector stable and minimizing long-term maintenance.

This note complements: `2026-01-28-ui-architecture-notes.md`.

## Design goals
- Stable selection logic that works on unknown devices.
- Small curated list of device-specific overrides for “perfect fit” on known devices.
- Inventory of concrete layouts (views) tied to code and DI; avoid string/type duplication.
- Avoid UI-thread polling / blocking waits; resolve via DI through injected services.

## Decisions (recorded)
- `variantKey` will be a **string** (composite key, not a simple enum).
  - Example: `"DefaultWeather.Win1920x1200"`.
- Fallback order will prefer:
  1) curated device overrides
  2) screen-class baseline (`Compact`/`Medium`/`Expanded`)
  3) adaptive/default

## Data sources

### A) Curated overrides (YAML)
Small repo-managed YAML file.

- Purpose: map a known device identity to a preferred `variantKey` for a logical content view.
- This is intended to stay small (personal devices, supported loaners).
- Backing may change later (SQLite, etc.), so access is via interface.

Recommended interface:
- `IDeviceOverrideSource`
  - `bool TryGetOverride(LogicalContentKey content, DeviceContext device, out string variantKey)`

Suggested YAML `device-overrides.yaml` shape:

```yaml
version: 1

devices:
  - id: "my-desktop"
    platform: "WinUI"
    manufacturer: "Microsoft"
    model: "GE68HX13V"
    overrides:
      DefaultWeather: "DefaultWeather.Win1920x1200"
      Precipitation: "Precipitation.Adaptive"
    notes: "Primary dev machine"

  - id: "zfold4"
    platform: "Android"
    manufacturer: "Samsung"
    model: "SM-F936U"
    overrides:
      DefaultWeather:
        Portrait: "DefaultWeather.ZFold4.Portrait"
        Landscape: "DefaultWeather.ZFold4.Landscape"
    notes: "Loaner device"
```

Notes:
- Use identity fields (`platform`, `manufacturer`, `model`) for matching.
- Allow optional orientation-specific overrides.
- Avoid raw pixel resolution as the primary key.

### B) Variant catalog (DDI-generated data)
DDI/code generation should track what concrete implementations exist, since they are coupled to CLR types and DI.

- Purpose: inventory of available variants for each logical content key.
- Maps `variantKey` -> `Type`.
- Optionally includes match metadata and priority.

Recommended interface:
- `IContentVariantCatalog`
  - `bool TryGetViewType(LogicalContentKey content, string variantKey, out Type viewType)`

### C) Screen class rules (code)
A small, stable function that maps `DeviceContext` -> `ScreenClass`.

- Use dp-based metrics derived from `IDisplayInfo`:
  - `widthDp = WidthPx / Density`
  - `heightDp = HeightPx / Density`
  - `minDp = min(widthDp, heightDp)`
- Typical thresholds:
  - `Compact`: `minDp < 600`
  - `Medium`: `600 <= minDp < 840`
  - `Expanded`: `minDp >= 840`

## Selector pipeline (single responsibility)
A selector/factory is responsible for turning:
- (`LogicalContentKey`, `DeviceContext`) -> `View` instance

Suggested flow:
1. Attempt override lookup:
   - `IDeviceOverrideSource.TryGetOverride(...)`
2. If no override, compute `ScreenClass` and choose baseline variant key:
   - e.g. `DefaultWeather.Compact`, `.Medium`, `.Expanded`
3. If that baseline variant is not available, use adaptive/default:
   - e.g. `DefaultWeather.Adaptive`
4. Resolve `viewType` from `IContentVariantCatalog` and instantiate via DI:
   - `ActivatorUtilities.CreateInstance(services, viewType)`

## Notes on complexity
- Keep the selector deterministic first; add fuzzy matching only if needed.
- Keep catalogs explicit; avoid assembly scanning.
- Keep DI resolution explicit; do not reintroduce UI-thread blocking waits.
