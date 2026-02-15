# DDI YAML conventions (`WeatherStationMaui.yaml`)

This document captures working conventions for the Declarative DI (DDI) input YAML.

It is written from the perspective of the `WeatherStationMaui.yaml` file under the docs project.

## File structure

A typical DDI input file has these top-level sections:

- `codeGen:`
  - Controls where generated code is emitted and how it is named (registry class name, namespace, initializer name).
- `namespace:`
  - A *model* of types (interfaces/classes), their constructor/initializer parameters, and accessible properties.
- `instance:`
  - The concrete instance graph to create, initialize, and (optionally) expose into MAUI DI.

## Core rules

### 1) Define-before-use (instance ordering)

The `instance:` section is ordered.

- An instance must appear **before** any other instance that references it.
- Forward references are not allowed.

This applies only to `instance:` ordering. The `namespace:` ordering is not constrained.

### 2) Every assignment must exist in the model

For any `instance:` entry:

- Every `assignment[].name` must exist in the corresponding class model:
  - `namespace[].class[].parameter[].name`

If the parameter is missing from the model, generation will either fail or generate code that doesn’t compile.

### 3) Prefer DDI’s two-phase initialization pattern

Across this solution, DDI-managed services typically follow this pattern:

- Parameterless constructor
- `InitializeAsync(...)` method called by generated code

That enables:

- deterministic creation graph
- async initialization
- explicit wiring (no runtime reflection)

### 4) Dotted instance access requires properties in the model

DDI supports accessing properties of an instance via dotted notation:

- Example: `TheRootCancellationTokenSource.Token`

Conventions:

- Dotted access **requires** the property to be declared in the `namespace:` model for that class under `property:`.
- If an instance is `exposeToMauiDi: true` and consumers depend on an interface, that property ideally exists on the interface too.

### 5) Interfaces vs concretes (MAUI DI exposure)

- `exposeToMauiDi: true` means the DDI-created instance is registered into MAUI DI.
- When exposing to MAUI DI:
  - ensure the `namespace:` model for the class includes an `interface:` mapping
  - favor depending on that interface from MAUI-constructed classes

DDI-created services do **not** automatically see MAUI DI registrations. Any dependency a DDI-initialized class needs must be declared and wired in YAML.

## Settings-derived values (factory pattern)

DDI YAML assignments are limited to:

- `literal:` values
- `instance:` references (including dotted property access)

If you need to build complex objects from settings (e.g., `SqliteDatabaseOptions` from `/services/sqlite/*`), use a small factory class that follows the DDI initialization pattern.

### Recommended pattern

- Create a factory class with parameterless constructor and `InitializeAsync(...)` that reads settings.
- Expose the built value via a property.
- Consume it via dotted access.

Example shape:

- `SqliteDatabaseOptionsFactory`
  - `InitializeAsync(ILogger, ISettingRepository, IPlatformPaths, CancellationToken)`
  - `Options` property

Then YAML can do:

- `TheSqliteDatabaseOptions.ConnectionString = TheSqliteDatabaseOptionsFactory.Options.ConnectionString`

## Troubleshooting checklist

When generated code doesn’t compile:

1. Confirm the `instance:` ordering satisfies define-before-use.
2. Confirm every `assignment[].name` exists in the `namespace:` model parameter list.
3. Confirm dotted property access is declared in the `namespace:` model property list.
4. Confirm the generated initializer signature matches the target `InitializeAsync(...)` signature.
5. Confirm the target project references the assemblies that define the types being created.
