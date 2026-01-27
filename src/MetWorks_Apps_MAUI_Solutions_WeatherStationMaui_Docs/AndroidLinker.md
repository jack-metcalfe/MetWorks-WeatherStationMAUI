Android linker note
====================

What and where
- A conservative linker descriptor was added to the MAUI Android project at:
  `src/MetWorks_Apps_MAUI_WeatherStationMaui/Platforms/Android/linker.xml`

Why this exists
- The Mono/ILLink linker used for Android builds removes IL that appears unused. Types referenced only via reflection (for example the unit classes discovered by `RedStar.Amounts` with `Assembly.GetExportedTypes()`) can be trimmed and cause `ReflectionTypeLoadException` at runtime on device.
- The `linker.xml` preserves the RedStar assemblies so reflection-based registration succeeds without editing or forking third-party sources.

How to refine later
- The descriptor is intentionally conservative (`preserve="all"`) to guarantee correctness. After collecting diagnostics (see `UnitsOfMeasureInitializer` logging of `ReflectionTypeLoadException.LoaderExceptions`), narrow the file to specific `<type ... />` entries for only the missing types to reduce APK size.

Notes
- Prefer non-invasive fixes: linker XML or `[Preserve]` attributes, not editing upstream libraries.
- Debug tip: temporarily set `<AndroidLinkMode>None</AndroidLinkMode>` in the Android csproj for debug builds to verify linker is the cause, but don't leave it in place for release builds.
- Diagnostics for loader exceptions are emitted in `UnitsOfMeasureInitializer` so logcat should show which types the linker removed if problems occur.
