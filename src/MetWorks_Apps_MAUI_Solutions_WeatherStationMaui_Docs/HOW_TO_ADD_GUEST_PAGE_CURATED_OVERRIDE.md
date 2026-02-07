# How to add a new guest page as a curated override (step-by-step)

This repo uses a small selector pipeline to choose which concrete `ContentView` to show for a given logical screen (“guest page”).

Key pieces:

- **Host composition** decides *which logical pages exist* and their swipe order.
  - `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/HostCompositionCatalog.cs`
- **Variant selection** decides *which `variantKey` string to use* for a logical page on a specific device.
  - `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/ContentViewFactory.cs`
- **Variant catalog** maps (`LogicalContentKey`, `variantKey`) → concrete `ContentView` CLR type.
  - `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/ContentVariantCatalog.cs`
- **Curated overrides** (YAML) can override the `variantKey` for a known device identity.
  - YAML file: `src/MetWorks_Apps_MAUI_WeatherStationMaui/device-overrides.yaml`
  - Loader: `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/Overrides/YamlDeviceOverrideSource.cs`

The overall flow is documented in `src/MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs/2026-01-28-ui-selector-schema.md`.

---

## Step 0: Decide what you are adding

There are two related (but different) actions you might mean:

1) **Add a new logical guest page** (new `LogicalContentKey` + new `ContentView`) and include it in the host swipe order.

2) **Add a curated override** for an *existing* logical guest page so a known device uses a specific `variantKey`.

This document covers both, because you usually do (1) once, then (2) optionally per device.

---

## Step 1: Create the new guest page `ContentView`

Create a new `ContentView` under:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/Pages/GuestPages/`

Example files (pattern):

- `LiveWindAdaptive.xaml` / `.xaml.cs`
- `MetricsOne.xaml` / `.xaml.cs`

Minimum shape:

- XAML root is a `ContentView`
- Code-behind sets the `BindingContext` to an injected viewmodel (if the page needs one)

Also ensure it’s registered with MAUI DI if it is created through `ActivatorUtilities.CreateInstance(...)`:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/MauiProgram.cs`
  - register the viewmodel and the view type

---

## Step 2: Add a new `LogicalContentKey`

Add a new enum member:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/LogicalContentKey.cs`

Example:

- `HomePage`
- `LiveWind`
- `MetricsOne`

---

## Step 3: Put the new page into the host composition (swipe order)

Edit:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/HostCompositionCatalog.cs`

Update the slot list for `HostKey.MainSwipe`:

```csharp
new[] { LogicalContentKey.HomePage, LogicalContentKey.LiveWind, LogicalContentKey.YourNewPage }
```

This controls that the new guest page is reachable by swiping and where it sits in ordering.

---

## Step 4: Ensure `ContentViewFactory` can select a baseline `variantKey`

Edit:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/ContentViewFactory.cs`

Add a case in `SelectVariantKey(...)` for your new logical key.

Typical patterns:

- “Single layout page” (no real variants): return `VariantKeys.Placeholder.Default`
- “HomePage-like” (has dp/screen-class variants): return one of the `VariantKeys.DefaultWeather.*` keys

Note:

- You do **not** need to add a new screen-class variant family (like `VariantKeys.DefaultWeather.*`) just because a page supports curated overrides.
- Screen-class variant families are only useful when you want a **non-curated fallback** based on `DeviceContext` (e.g. Compact/Medium/Expanded).
- For curated-only pages (for example `MetricsOne` or `LiveWind`), the baseline should remain something simple like `VariantKeys.Placeholder.Default`, and any device-specific selection happens via `device-overrides.yaml`.

If you skip this step you’ll get:

- `ArgumentOutOfRangeException: Unknown logical content` (exact failure seen previously)

---

## Step 5: Register the new page in the variant catalog

Edit:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/ContentVariantCatalog.cs`

Add a mapping:

```csharp
if (content == LogicalContentKey.YourNewPage)
{
    viewType = typeof(YourNewContentView);
    return true;
}
```

Notes:

- For `HomePage`, the catalog uses `variantKey switch { ... }` because there are multiple variants.
- For pages like `LiveWind` and `MetricsOne`, the `variantKey` is effectively ignored and you always return a single view type.

---

## Step 6: (Optional) Add new `variantKey` constants

If your new page truly supports multiple variants (e.g., per device/layout), add constants in:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/VariantKeys.cs`

Use the same naming style as existing entries.

---

## Example: Register curated variants for an existing guest page (`MetricsOne`)

This is an example of **adding multiple concrete layouts for a single existing `LogicalContentKey`** and then using **curated YAML device overrides** to select which concrete layout is used per device.

In other words:

- The logical page (`LogicalContentKey.MetricsOne`) already exists.
- You are adding additional concrete `ContentView` implementations (one per curated layout).
- You then use `device-overrides.yaml` to force a specific `variantKey` on known devices.

### 1) Add `variantKey` constants for `MetricsOne`

Add a dedicated nested class in `VariantKeys` (keeps strings centralized and consistent):

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/VariantKeys.cs`

Example keys:

- `MetricsOne.Win1920x1200 = "MetricsOne.Win1920x1200"`
- `MetricsOne.And2176x1812 = "MetricsOne.And2176x1812"`
- `MetricsOne.And2304x1440 = "MetricsOne.And2304x1440"`

### 2) Create the concrete `ContentView` variants

Create one `ContentView` per curated layout under:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/Pages/GuestPages/`

Example files:

- `MetricsOne1920x1200.xaml` / `.xaml.cs`
- `MetricsOne2176x1812.xaml` / `.xaml.cs`
- `MetricsOne2304x1440.xaml` / `.xaml.cs`

Typically they can all bind to the same `MetricsOneViewModel`.

### 3) Register the variant views with MAUI DI

Since pages are created via DI (`ActivatorUtilities.CreateInstance(...)`), register the concrete view types in:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/MauiProgram.cs`

Example registrations:

- `builder.Services.AddTransient<MetricsOne1920x1200>();`
- `builder.Services.AddTransient<MetricsOne2176x1812>();`
- `builder.Services.AddTransient<MetricsOne2304x1440>();`

### 4) Map `variantKey` to view types in `ContentVariantCatalog`

Update:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/DeviceSelection/ContentVariantCatalog.cs`

Instead of always returning a single view for `MetricsOne`, map based on the `variantKey` (similar to `HomePage`). This is the step that turns curated override strings into an actual different `ContentView`.

### 5) Add curated overrides to `device-overrides.yaml`

Update:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/device-overrides.yaml`

Example:

```yaml
devices:
  - id: "laptop"
    platform: "WinUI"
    manufacturer: "Microsoft"
    model: "GE68HX13V"
    overrides:
      MetricsOne: "MetricsOne.Win1920x1200"
```

Notes:

- `MetricsOne` must match `LogicalContentKey.MetricsOne.ToString()`.
- `platform`/`manufacturer`/`model` must exactly match the runtime `DeviceContext` values.

### 6) Keep `ContentViewFactory` baseline simple

It is normal that `ContentViewFactory.SelectVariantKey(...)` continues to use:

- `LogicalContentKey.MetricsOne => VariantKeys.Placeholder.Default`

Curated overrides are applied before the baseline is used, so curated devices still get the correct `variantKey`.

---

## Step 7: Add a curated override entry (YAML)

Curated overrides live in:

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/device-overrides.yaml`

### 7.1 Device identity matching

The YAML loader computes an “identity key” as:

- `"{platform}|{manufacturer}|{model}"`

See:

- `YamlDeviceOverrideSource.DeviceOverridesIndex.ComposeIdentityKey(...)`

So your YAML must match the values in `DeviceContext`:

- `platform` (e.g. `WinUI`, `Android`)
- `manufacturer` (note: your YAML currently uses lowercase `samsung` for Android devices)
- `model`

### 7.2 Override shape

The current `YamlDeviceOverrideSource` supports two shapes per content key:

1) Simple default:

```yaml
overrides:
  MetricsOne: "MetricsOne.Win1920x1200"
```

2) Orientation-specific:

```yaml
overrides:
  HomePage:
    Portrait: "HomePage.ZFold4.Portrait"
    Landscape: "HomePage.ZFold4.Landscape"
```

Internally those become:

- `Default` for the scalar case
- `Portrait`/`Landscape` for the mapping case

At runtime `TryGetOverride(...)` chooses:

1) orientation-specific entry (if present)
2) else `Default` entry

### 7.3 Example: add a curated override for a new guest page

Add (or update) the overriding device block:

```yaml
devices:
  - id: "my-device"
    platform: "WinUI"
    manufacturer: "Microsoft"
    model: "GE68HX13V"
    overrides:
      YourNewPage: "YourNewPage.Win1920x1200"
```

---

## Step 8: Validate behavior

1) Build:

```bash
dotnet build
```

2) Run the MAUI app and confirm:

- The new page exists in the swipe sequence.
- For the target device identity, the curated override is selected.
  - If you want to confirm this quickly, add a temporary debug log in `ContentViewFactory.SelectVariantKey(...)` that prints `content`, `deviceContext`, and chosen `variantKey`.

---

## Common failures and what they mean

- **"Failed to load device view"** with inner exception:
  - `ArgumentOutOfRangeException: Unknown logical content`
  - Fix: add a `LogicalContentKey` case in `ContentViewFactory.SelectVariantKey(...)`.

- **"No view type registered for {content} / {variantKey}"**
  - Fix: ensure `ContentVariantCatalog.TryGetViewType(...)` maps that pair.
  - For home-page variants, ensure your `variantKey` constant matches exactly.

- **Curated override not applying**
  - Fix: ensure `platform/manufacturer/model` strings match the `DeviceContext` values exactly.
  - Fix: ensure the YAML override content key matches `LogicalContentKey.ToString()` (e.g. `HomePage`, `MetricsOne`).

