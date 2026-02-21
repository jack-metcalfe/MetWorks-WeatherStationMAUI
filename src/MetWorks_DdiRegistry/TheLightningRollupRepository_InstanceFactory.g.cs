// Template:            Instance.Factory
// Version:             1.1
// Template Requested:  Instance.Factory
// Template:            File.Header
// Version:             1.1
// Template Requested:  Instance.Factory
// Generated On:        2026-02-21T03:45:20.3939932Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The InstanceFactory encapsulates per-instance creation logic.
    // Declared as partial to allow modularization if needed.
    // It handles both element-driven and assignment-driven construction,
    // and immediately registers the created instance with the Registry.
    internal static partial class TheLightningRollupRepository_InstanceFactory
    {
        public static MetWorks.Persistence.Rollups.LightningRollupRepository Create(Registry registry)
        {
            // Assignment-driven instance: construct with new().
            // This is always valid because ContainerClass is a concrete class.
            var instance = new MetWorks.Persistence.Rollups.LightningRollupRepository();
            

            // Register immediately so other instances can reference it.
            registry.RegisterTheLightningRollupRepository(instance);

            return instance;
        }
    }
}