// Template:            Instance.Factory
// Version:             1.1
// Template Requested:  Instance.Factory
// Template:            File.Header
// Version:             1.1
// Template Requested:  Instance.Factory
// Generated On:        2026-02-08T19:18:29.6186727Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The InstanceFactory encapsulates per-instance creation logic.
    // Declared as partial to allow modularization if needed.
    // It handles both element-driven and assignment-driven construction,
    // and immediately registers the created instance with the Registry.
    internal static partial class TheMetricsLatestSnapshotStore_InstanceFactory
    {
        public static MetWorks.Common.Metrics.MetricsLatestSnapshotStore Create(Registry registry)
        {
            // Assignment-driven instance: construct with new().
            // This is always valid because ContainerClass is a concrete class.
            var instance = new MetWorks.Common.Metrics.MetricsLatestSnapshotStore();

            // Register immediately so other instances can reference it.
            registry.RegisterTheMetricsLatestSnapshotStore(instance);

            return instance;
        }
    }
}