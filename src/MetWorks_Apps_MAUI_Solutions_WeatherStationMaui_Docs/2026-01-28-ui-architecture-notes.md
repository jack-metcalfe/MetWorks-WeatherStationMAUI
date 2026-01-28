# 2026-01-28 UI Architecture Notes (Host Pages + Logical Content + Device Variants)

Related: `2026-01-28-ui-selector-schema.md`

## Goal
Grow the MAUI UI without multiplying routes/pages and without scattering device-specific strings/types.

We want:
- A small, stable set of **Host Pages** (Shell routes), e.g. Main, History, Settings.
- A growing set of **Logical Content Views** (what the user is looking at), e.g. DefaultWeather, Precipitation, FeelsLike.
- Optional **Concrete Variants** per logical content view for specific device profiles, plus an adaptive/default implementation where appropriate.
- Device selection should be **data-driven**, based on device characteristics and available variants.

## Current State (as of this session)

### Device-specific main views
- Device selection is centralized via `DeviceViewRegistry` + `DeviceViewSelector`.
- View naming/type mapping is centralized in `DeviceSelection/MainDeviceViewsCatalog.cs` using `nameof(...)`.
- MAUI DI registrations for these views are driven by `MainDeviceViewsCatalog.AllViewTypes`.

### Host page and paging
- `Pages/MainDeviceViews/MainViewPage` currently acts as a **host page**.
- The UI uses **deterministic manual paging** (a single host `ContentView` and explicit swapping) rather than `CarouselView`.
  - Rationale: `CarouselView` exhibited virtualization/recycling issues (oscillation/self-swiping) in this scenario.
  - Paging gestures:
    - Android: swipe left/right via `SwipeGestureRecognizer`
    - Windows: left/right arrow keys and left/right buttons

### Second content view
- `SecondWindowContent` is currently a placeholder for the second “swiped” screen.
- `SecondWindowContent` can be created via MAUI DI (constructor injection supported).
- `MainViewPage` was refactored to be DI-friendly:
  - `MainViewPage(IServiceProvider services)` constructor injection
  - `SecondWindowContent` resolved via injected `IServiceProvider`
  - Removed polling/waiting for `Application.Current?.Handler?.MauiContext?.Services`

## Proposed Structure

### Concepts
1. **Host Page**
   - A navigation container / route target.
   - Owns layout shell (background, top-level nav, gesture handling) and composes logical content.

2. **Logical Content View**
   - Represents meaning (“DefaultWeather”, “Precipitation”, etc.), independent of device.

3. **Concrete Variant**
   - A specific `ContentView` implementation for a device profile (or the adaptive baseline).

### Proposed folder layout (illustrative)

```
Pages/
  HostPages/
    MainHostPage.xaml(.cs)
    HistoryHostPage.xaml(.cs)

  Content/
    DefaultWeather/
      DefaultWeatherView.xaml(.cs)                # adaptive baseline
      Variants/
        Windows/DefaultWeather_1920x1200.xaml(.cs)
        Android/Samsung/ZFold4/DefaultWeather_Portrait.xaml(.cs)
        Android/Samsung/ZFold4/DefaultWeather_Landscape.xaml(.cs)

    Precipitation/
      PrecipitationView.xaml(.cs)
      Variants/...
```

### Catalog / selection
- Keep enums for:
  - `HostPageKey` (small, stable)
  - `LogicalContentKey` (grows over time)
- Do NOT create enums for concrete variants.

Instead, use a data-driven catalog describing:
- For each `LogicalContentKey`, the available variant `Type`s
- Match metadata (platform/model/orientation/resolution ranges)
- Priority ordering (device-specific overrides > adaptive baseline)

Selection API (conceptual):
- `IContentViewFactory.Create(LogicalContentKey key, DeviceContext device)` -> `View`

## DDI Extension Idea (UI catalog generation)
We discussed extending DDI with a UI section to generate:
- `HostPageKey` enum
- `LogicalContentKey` enum
- A generated UI catalog (descriptors mapping keys to types + match metadata)
- Optionally: DI registrations from the catalog and default “swipe sets” per host page

Key guiding rule: generate **data + keys**, keep selection logic handwritten for debuggability.

## Notes / constraints
- Avoid UI-thread blocking waits and polling loops.
- Prefer constructor injection / injected factories over accessing DI via `Application.Current.Handler.MauiContext.Services`.
- Use `nameof(...)` and centralized catalogs to avoid string duplication.
- Keep Shell routes stable (few routes) and do variation inside host pages.
