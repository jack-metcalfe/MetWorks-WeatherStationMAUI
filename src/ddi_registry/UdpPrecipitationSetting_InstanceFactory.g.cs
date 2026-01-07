// Template:            Instance.Factory
// Version:             1.1
// Template Requested:  Instance.Factory
// Template:            File.Header
// Version:             1.1
// Template Requested:  Instance.Factory
// Generated On:        2026-01-07T05:31:08.6249926Z
#nullable enable

namespace ServiceRegistry
{
    // The InstanceFactory encapsulates per-instance creation logic.
    // Declared as partial to allow modularization if needed.
    // It handles both element-driven and assignment-driven construction,
    // and immediately registers the created instance with the Registry.
    internal static partial class UdpPrecipitationSetting_InstanceFactory
    {
        public static Settings.SettingConfiguration Create(Registry registry)
        {
            // Assignment-driven instance: construct with new().
            // This is always valid because ContainerClass is a concrete class.
            var instance = new Settings.SettingConfiguration();

            // Register immediately so other instances can reference it.
            registry.RegisterUdpPrecipitationSetting(instance);

            return instance;
        }
    }
}