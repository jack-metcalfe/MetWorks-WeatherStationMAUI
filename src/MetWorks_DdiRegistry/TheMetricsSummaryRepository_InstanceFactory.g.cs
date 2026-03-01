// Template:            Instance.Factory
// Version:             1.1
// Template Requested:  Instance.Factory
// Template:            File.Header
// Version:             1.1
// Template Requested:  Instance.Factory
// Generated On:        2026-03-01T07:07:53.2766146Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The InstanceFactory encapsulates per-instance creation logic.
    // Declared as partial to allow modularization if needed.
    // It handles both element-driven and assignment-driven construction,
    // and immediately registers the created instance with the Registry.
    internal static partial class TheMetricsSummaryRepository_InstanceFactory
    {
        public static MetWorks.Persistence.Metrics.MetricsSummaryRepository Create(Registry registry)
        {
            // Assignment-driven instance: construct with new().
            // This is always valid because ContainerClass is a concrete class.
            var instance = new MetWorks.Persistence.Metrics.MetricsSummaryRepository();
            

            // Register immediately so other instances can reference it.
            registry.RegisterTheMetricsSummaryRepository(instance);

            return instance;
        }
    }
}