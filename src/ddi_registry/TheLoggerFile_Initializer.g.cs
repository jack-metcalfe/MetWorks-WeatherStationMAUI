// Template:            Assignments.Initializer
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Template:            File.Header
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Generated On:        2026-01-15T03:06:48.6676207Z
#nullable enable
using System.Threading.Tasks;

namespace ServiceRegistry
{
    // Per-instance async initializer.
    // Declared as partial to allow modularization if needed.
    // Only emitted for instances that have assignment-driven initialization.
    internal static partial class TheLoggerFile_Initializer
    {
        public static async Task Initialize_TheLoggerFileAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetTheLoggerFile_Internal();

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                // Parameter: iLogger: registry.GetTheLoggerStub()
                iLogger: registry.GetTheLoggerStub(),
                // Parameter: iSettingRepository: registry.GetTheSettingRepository()
                iSettingRepository: registry.GetTheSettingRepository()
            );
        }
    }
}