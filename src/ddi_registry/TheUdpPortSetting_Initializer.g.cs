// Template:            Assignments.Initializer
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Template:            File.Header
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Generated On:        2026-01-07T05:31:08.6249926Z
#nullable enable
using System.Threading.Tasks;

namespace ServiceRegistry
{
    // Per-instance async initializer.
    // Declared as partial to allow modularization if needed.
    // Only emitted for instances that have assignment-driven initialization.
    internal static partial class TheUdpPortSetting_Initializer
    {
        public static async Task Initialize_TheUdpPortSettingAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetTheUdpPortSetting_Internal();

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                // Parameter: defaultValue: "50222"
                defaultValue: "50222",
                // Parameter: description: "UDP Port for listening for readings from Tempest Weather Station"
                description: "UDP Port for listening for readings from Tempest Weather Station",
                // Parameter: expectedValueType: "Int32"
                expectedValueType: "Int32",
                // Parameter: group: "UDPSettings"
                group: "UDPSettings",
                // Parameter: isEditable: true
                isEditable: true,
                // Parameter: path: "/services/UDPSettings/PreferredPort"
                path: "/services/UDPSettings/PreferredPort",
                // Parameter: enumValues: Array.Empty<System.String>()
                enumValues: Array.Empty<System.String>(),
                // Parameter: isSecret: null
                isSecret: null
            );
        }
    }
}