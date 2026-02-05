// Template:            Instance.Factory
// Version:             1.1
// Template Requested:  Instance.Factory
// Template:            File.Header
// Version:             1.1
// Template Requested:  Instance.Factory
// Generated On:        2026-02-04T20:48:31.7351773Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The InstanceFactory encapsulates per-instance creation logic.
    // Declared as partial to allow modularization if needed.
    // It handles both element-driven and assignment-driven construction,
    // and immediately registers the created instance with the Registry.
    internal static partial class TheMetricsSummaryIngestor_InstanceFactory
    {
        public static MetWorks.Common.Metrics.MetricsSummaryIngestor Create(Registry registry)
        {
            // Assignment-driven instance: construct with new().
            // This is always valid because ContainerClass is a concrete class.
            var instance = new MetWorks.Common.Metrics.MetricsSummaryIngestor();

            // Register immediately so other instances can reference it.
            registry.RegisterTheMetricsSummaryIngestor(instance);

            return instance;
        }
    }
}