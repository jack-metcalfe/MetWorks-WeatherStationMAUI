// Template:            Instance.Factory
// Version:             1.1
// Template Requested:  Instance.Factory
// Template:            File.Header
// Version:             1.1
// Template Requested:  Instance.Factory
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The InstanceFactory encapsulates per-instance creation logic.
    // Declared as partial to allow modularization if needed.
    // It handles both element-driven and assignment-driven construction,
    // and immediately registers the created instance with the Registry.
    internal static partial class TheRollupsDatabaseReadiness_InstanceFactory
    {
        public static MetWorks.Persistence.Rollups.RollupsDatabaseReadiness Create(Registry registry)
        {
            // Assignment-driven instance: construct with new().
            // This is always valid because ContainerClass is a concrete class.
            var instance = new MetWorks.Persistence.Rollups.RollupsDatabaseReadiness();
            

            // Register immediately so other instances can reference it.
            registry.RegisterTheRollupsDatabaseReadiness(instance);

            return instance;
        }
    }
}