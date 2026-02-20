# C3: Dotted-property references (`instance: Foo.Bar.Baz`) — implementation notes

Status: design notes captured (implementation pending).

This document records the intended approach for C3 (“Validate dotted-property references”) and provides YAML authoring examples for multi-level property chains.

## Problem statement

DDI YAML supports binding an initializer parameter to another named instance (e.g., `instance: TheFoo`). Sometimes the initializer needs a *property* of that instance (e.g., `instance: RootCancellationTokenSource.Token`).

Once you allow one `.` segment, users quickly want more (`A.B.C.D`). C3 ensures these dotted chains are safe:

- The referenced property exists on the concrete C# type.
- The property is declared in the YAML model (`namespace:` → `class:` → `property:`).
- When the base instance is exposed to MAUI DI (`exposeToMauiDi: true`), the property should ideally also exist on the modeled interface.

## Terminology

For a YAML token like:

- `instance: TheA.B.C.D`

We treat it as:

- Base instance name: `TheA`
- Property path segments: `B`, `C`, `D`

## YAML authoring: what additions look like for multi-level chains

The key rule is: **every hop in the chain must be representable as a typed property in the YAML model**.

That means:

1. The base instance’s concrete type must declare property `B` under `property:`.
2. The type of `B` must be declared as a `class:` or `interface:` type reference.
3. The next type (the type of `B`) must declare property `C` under `property:`.
4. Repeat for as many levels as needed.

### Example: 1-level chain

Goal: bind initializer param `cancellationToken` to `RootCancellationTokenSource.Token`.

```yaml
namespace:
  - name: "System.Threading"
    class:
      - name: "CancellationTokenSource"
        interface: null
        parameter: []
        property:
          - name: "Token"
            class: "System.Threading.CancellationToken"
            interface: null

      - name: "CancellationToken"
        interface: null
        parameter: []
        property: []

instance:
  - name: "RootCancellationTokenSource"
    class: "System.Threading.CancellationTokenSource"

  - name: "MyService"
    class: "MyNs.MyService"
    assignment:
      - name: "cancellationToken"
        instance: "RootCancellationTokenSource.Token"
```

### Example: 2-level chain

Goal: `instance: TheOptionsProvider.Options.ConnectionString`

You need:

- `OptionsProvider` class declares `Options` property.
- `Options` type declares `ConnectionString` property.

```yaml
namespace:
  - name: "MyNs"
    class:
      - name: "OptionsProvider"
        interface: null
        parameter: []
        property:
          - name: "Options"
            class: "MyNs.AppOptions"
            interface: null

      - name: "AppOptions"
        interface: null
        parameter: []
        property:
          - name: "ConnectionString"
            class: "System.String"
            interface: null

  - name: "System"
    class:
      - name: "String"
        interface: null
        parameter: []
        property: []

instance:
  - name: "TheOptionsProvider"
    class: "MyNs.OptionsProvider"

  - name: "MyService"
    class: "MyNs.MyService"
    assignment:
      - name: "connectionString"
        instance: "TheOptionsProvider.Options.ConnectionString"
```

### Example: 3+ levels

Same pattern. If you reference `A.B.C.D`, then `A`’s type must declare `B`, `B`’s type must declare `C`, and `C`’s type must declare `D`.

If any intermediate type/property is missing from `namespace:`, validation will (should) fail with an actionable diagnostic.

## Planned implementation approach (code)

### 1) Loader parsing: represent a property path (not just one property)

Currently `instance:` is effectively split into `{ Instance, InstanceProperty }` (single segment).

C3 requires parsing into:

- `Instance` (base instance name)
- `InstancePropertyPath` (ordered list of segments)

Generator output then becomes:

- `registry.GetTheA().B.C.D`

### 2) YAML-model validation: define-before-use across the chain

Validation should walk the property path segments and verify that each segment is declared in the YAML model:

- Starting from base instance’s class (`TheA` → `ClassQualified`)
- For each segment `seg`:
  - Find `seg` under `property:` for the current class
  - Read its declared type (`class:` or `interface:`)
  - Set “current class” to that type for the next hop

This is what makes multi-level chains feasible without guesswork.

### 3) Reflection validation: ensure the C# concrete types actually match

Using build-time reflection (metadata load context, like C1):

- Resolve base concrete type
- For each segment:
  - Verify `PropertyInfo` exists
  - Verify CLR property type matches YAML declared type (including array/nullable rules)
  - Advance current CLR type

### 4) Interface exposure rule (MAUI DI ergonomics)

When a base instance is `exposeToMauiDi: true` and the YAML class models an interface:

- The dotted property chain should ideally be valid on the interface as well.

In practice:

- Validate the property exists on the interface type for each segment (or at least for the first segment), otherwise consumers typed as the interface can’t compile.

The exact strictness (first segment vs all segments) should be decided based on what patterns we actually want to allow.

## Diagnostics (anticipated)

We’ll keep diagnostics explicit and localizable-friendly (short, precise). Likely new diagnostic codes (names TBD) along the lines of:

- Property missing from YAML model (`namespace:` `property:`)
- Property missing on C# type
- Property type mismatch (YAML vs C#)
- Interface missing property (when `exposeToMauiDi: true`)

## Testing notes

Add loader tests covering:

- Valid 1-level chain (no diagnostics)
- Valid 2-level chain (no diagnostics)
- Missing intermediate `property:` entry in YAML (diagnostic)
- Property exists in YAML but not on C# type (diagnostic)
- `exposeToMauiDi: true` + interface missing property (diagnostic)

## Step log

### Step 0 — design + YAML authoring guidance (this document)

- Captured scalable “walk the chain” approach.
- Documented the YAML additions pattern (“declare intermediate type + declare its property”) with 1-level and 2-level examples.

### Step 1 — allow multi-level property paths in `assignment.instance`

- Updated the syntax DTO `MetWorks.DI.Declarative.Syntax.Models.Assignment`:
  - renamed `InstanceProperty` → `InstancePropertyPath` (stores the full remainder after the base instance name, e.g. `Node.Leaf.Value`).
- Updated loader parsing so `instance: TheChainRoot.Node.Leaf.Value` becomes:
  - base instance name: `TheChainRoot`
  - property path: `Node.Leaf.Value`
- Updated the generated initializer argument expression to append the full property path.

### Step 2 — build-time validator for dotted property chains (YAML + reflection)

- Added `MetWorks.DI.Declarative.Generator.DottedPropertyReferenceValidator`.
- Validates each dotted segment by:
  - checking the property exists in the YAML class model (`namespace:` → `class:` → `property:`)
  - checking the property exists on the reflected concrete type
  - checking the reflected property type matches the YAML-declared type
- When the base instance is `exposeToMauiDi: true`, warns if the first segment is not declared on the exposed interface.

### Step 3 — toolchain integration + tests

- Integrated the validator into `MetWorks_DI_Declarative_CodeGenTool` so dotted-property errors fail fast before any files are generated.
  - Warnings are printed but do not fail generation.
- Added xUnit tests + fixtures covering:
  - valid multi-level chain
  - missing YAML property
  - missing concrete C# property
  - exposed interface missing first segment (warning)

(As C3 implementation proceeds, append additional step entries here with links to code changes and tests.)
