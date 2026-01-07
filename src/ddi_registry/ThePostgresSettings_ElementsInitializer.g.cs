// Template:            Elements.Initializer
// Version:             1.1
// Template Requested:  Elements.Initializer
// Template:            File.Header
// Version:             1.1
// Template Requested:  Elements.Initializer
// Generated On:        2026-01-07T05:31:08.6249926Z
#nullable enable

namespace ServiceRegistry
{
    // The ElementsInitializer builds element-driven instances.
    // All element expressions are fully computed by the pipeline.
    internal static partial class ThePostgresSettings_ElementsInitializer
    {
        public static Settings.SettingConfiguration[] CreateElements(Registry registry)
        {
            // Construct the instance using element-driven values.
            return new Settings.SettingConfiguration[] { registry.GetThePostgresConnection_Internal() };
        }
    }
}
