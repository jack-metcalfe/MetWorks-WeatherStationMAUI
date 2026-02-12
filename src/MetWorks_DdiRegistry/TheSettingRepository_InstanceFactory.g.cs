// Template:            Instance.Factory
// Version:             1.1
// Template Requested:  Instance.Factory
// Template:            File.Header
// Version:             1.1
// Template Requested:  Instance.Factory
// Generated On:        2026-02-12T05:35:58.8534477Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The InstanceFactory encapsulates per-instance creation logic.
    // Declared as partial to allow modularization if needed.
    // It handles both element-driven and assignment-driven construction,
    // and immediately registers the created instance with the Registry.
    internal static partial class TheSettingRepository_InstanceFactory
    {
        public static MetWorks.Common.Settings.SettingRepository Create(Registry registry)
        {
            // Assignment-driven instance: construct with new().
            // This is always valid because ContainerClass is a concrete class.
            var instance = new MetWorks.Common.Settings.SettingRepository();

            // Register immediately so other instances can reference it.
            registry.RegisterTheSettingRepository(instance);

            return instance;
        }
    }
}