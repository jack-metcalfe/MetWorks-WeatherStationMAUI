// Template:            Instance.Factory
// Version:             1.1
// Template Requested:  Instance.Factory
// Template:            File.Header
// Version:             1.1
// Template Requested:  Instance.Factory
// Generated On:        2026-02-01T03:34:47.7996337Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The InstanceFactory encapsulates per-instance creation logic.
    // Declared as partial to allow modularization if needed.
    // It handles both element-driven and assignment-driven construction,
    // and immediately registers the created instance with the Registry.
    internal static partial class TheEventRelayPath_InstanceFactory
    {
        public static MetWorks.EventRelay.EventRelayPath Create(Registry registry)
        {
            // Assignment-driven instance: construct with new().
            // This is always valid because ContainerClass is a concrete class.
            var instance = new MetWorks.EventRelay.EventRelayPath();

            // Register immediately so other instances can reference it.
            registry.RegisterTheEventRelayPath(instance);

            return instance;
        }
    }
}