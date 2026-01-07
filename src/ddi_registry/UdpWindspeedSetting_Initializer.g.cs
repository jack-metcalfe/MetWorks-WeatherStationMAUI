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
    internal static partial class UdpWindspeedSetting_Initializer
    {
        public static async Task Initialize_UdpWindspeedSettingAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetUdpWindspeedSetting_Internal();

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                // Parameter: defaultValue: "mile/hour"
                defaultValue: "mile/hour",
                // Parameter: description: "Override wind speed unit for UDP readings"
                description: "Override wind speed unit for UDP readings",
                // Parameter: expectedValueType: "String"
                expectedValueType: "String",
                // Parameter: group: "units"
                group: "units",
                // Parameter: isEditable: true
                isEditable: true,
                // Parameter: path: "/services/udp/unitOverrides/windSpeed"
                path: "/services/udp/unitOverrides/windSpeed",
                // Parameter: enumValues: registry.GetWindspeedOptions_Internal()
                enumValues: registry.GetWindspeedOptions_Internal(),
                // Parameter: isSecret: null
                isSecret: null
            );
        }
    }
}