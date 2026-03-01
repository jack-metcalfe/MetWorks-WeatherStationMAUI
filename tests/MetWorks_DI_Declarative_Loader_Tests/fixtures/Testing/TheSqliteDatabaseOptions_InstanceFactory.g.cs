// Template:            Instance.Factory
// Version:             1.1
// Template Requested:  Instance.Factory
// Template:            File.Header
// Version:             1.1
// Template Requested:  Instance.Factory
// Generated On:        2026-03-01T03:31:43.3092815Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The InstanceFactory encapsulates per-instance creation logic.
    // Declared as partial to allow modularization if needed.
    // It handles both element-driven and assignment-driven construction,
    // and immediately registers the created instance with the Registry.
    internal static partial class TheSqliteDatabaseOptions_InstanceFactory
    {
        public static MetWorks.Data.Sqlite.SqliteDatabaseOptions Create(Registry registry)
        {
            // Assignment-driven instance: construct with new().
            // This is always valid because ContainerClass is a concrete class.
            var instance = new MetWorks.Data.Sqlite.SqliteDatabaseOptions();
            

            // Register immediately so other instances can reference it.
            registry.RegisterTheSqliteDatabaseOptions(instance);

            return instance;
        }
    }
}