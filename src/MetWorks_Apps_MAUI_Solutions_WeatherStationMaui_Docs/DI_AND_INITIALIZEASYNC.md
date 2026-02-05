# Declarative DI and InitializeAsync

External reference: https://github.com/jack-metcalfe/MetWorks-DeclarativeDI

Pattern summary
- Construction: components are created with a public parameterless constructor. No runtime services are passed to the ctor.
- Initialization: runtime dependencies are provided by calling an async initializer:

```csharp
// recommended signature example (adjust interfaces to repo types)
public Task<bool> InitializeAsync(
    ILogger logger,
    ISettingProvider settings,
    IEventRelay eventRelay,
    IServiceProvider serviceProvider = null
);
```

Rationale
- Keeps object graph creation simple and serializable for discovery; initialization injects environment-specific services (logging, config, event bus) after construction.
- InitializeAsync returns bool to signal readiness; implement defensive behavior and log errors — initialization failures should surface clearly to the host.

Usage notes
- See src/settings/SettingRepository.cs for InitializeAsync usage patterns and how settings are usually provided to components.
- The DDI repo above contains helpers for registering and invoking InitializeAsync for all DDI components; prefer using the shared helpers to ensure consistent lifecycles.

## DDI YAML authoring notes (this solution)

- `instance:` entries must be defined before first use (no forward references).
  - This ordering constraint applies to `instance:` only; `namespace:` ordering is not constrained.
- The `namespace:` class `parameter:` list is the contract for initializer wiring.
  - Any initializer argument assigned in `instance:` must exist in the class `parameter:` list.
- Dotted instance property access (e.g., `RootCancellationTokenSource.Token`, `TheInstanceIdentifier.InstallationId`) requires:
  - declaring the property under the class’s `property:` list in `namespace:`
  - the property to exist on the concrete implementation (and on the interface if you expose the instance to MAUI DI).
