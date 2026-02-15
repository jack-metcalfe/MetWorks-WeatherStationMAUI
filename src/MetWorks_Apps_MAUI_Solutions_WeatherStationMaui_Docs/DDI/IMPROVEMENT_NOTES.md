# DDI improvement notes

This is intentionally a lightweight backlog of potential improvements. It’s not a commitment to implement them now.

## Integration / UX

- Visual Studio integration
  - Provide a “Generate DDI code” command (like CommunityToolkit.Mvvm source generators feel) that:
    - validates YAML
    - runs generation
    - opens the diff
- YAML validation in-editor
  - JSON schema / YAML schema for:
    - required keys
    - enum-like values (e.g., `exposeToMauiDi`)
    - define-before-use checks for `instance:`

## Generator capabilities

- Stronger type checking
  - validate that each `instance.assignment[].name` matches a corresponding parameter declared in `namespace.*.class[].parameter[]`
  - validate that assignment target types are compatible (where possible)
- Better dotted access
  - allow dotted access to interface properties when an instance is exposed to MAUI DI
  - optionally support null-conditional dotted access for optional properties
- Debuggability
  - emit a concise generation report listing:
    - instances created
    - initializer call graph
    - unused namespace model entries

## YAML ergonomics

- Support explicit “factory method” bindings (so options can be built from settings without creating a helper class)
- Add a compact syntax for simple literal assignments

## Maintenance

- Provide a “format YAML” mode aligned with YamlDotNet defaults
- Provide a “sort instance list by dependency” mode (keeping define-before-use rule satisfied)
