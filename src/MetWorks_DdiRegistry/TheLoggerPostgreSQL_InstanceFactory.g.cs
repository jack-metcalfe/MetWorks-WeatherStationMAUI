// Template:            Instance.Factory
// Version:             1.1
// Template Requested:  Instance.Factory
// Template:            File.Header
// Version:             1.1
// Template Requested:  Instance.Factory
// Generated On:        2026-01-26T23:47:15.2298447Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The InstanceFactory encapsulates per-instance creation logic.
    // Declared as partial to allow modularization if needed.
    // It handles both element-driven and assignment-driven construction,
    // and immediately registers the created instance with the Registry.
    internal static partial class TheLoggerPostgreSQL_InstanceFactory
    {
        public static MetWorks.Common.Logging.LoggerPostgreSQL Create(Registry registry)
        {
            // Assignment-driven instance: construct with new().
            // This is always valid because ContainerClass is a concrete class.
            var instance = new MetWorks.Common.Logging.LoggerPostgreSQL();

            // Register immediately so other instances can reference it.
            registry.RegisterTheLoggerPostgreSQL(instance);

            return instance;
        }
    }
}