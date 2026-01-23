// Template:            Assignments.Initializer
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Template:            File.Header
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Generated On:        2026-01-22T04:50:43.7490342Z
#nullable enable
using System.Threading.Tasks;

namespace MetWorks.ServiceRegistry
{
    // Per-instance async initializer.
    // Declared as partial to allow modularization if needed.
    // Only emitted for instances that have assignment-driven initialization.
    internal static partial class TheSensorReadingTransformer_Initializer
    {
        public static async Task Initialize_TheSensorReadingTransformerAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetTheSensorReadingTransformer_Internal();

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                // Parameter: iLogger: registry.GetTheLoggerFile()
                iLogger: registry.GetTheLoggerFile(),
                // Parameter: iSettingRepository: registry.GetTheSettingRepository()
                iSettingRepository: registry.GetTheSettingRepository(),
                // Parameter: iEventRelayBasic: registry.GetTheEventRelayBasic()
                iEventRelayBasic: registry.GetTheEventRelayBasic(),
                // Parameter: externalCancellation: registry.GetRootCancellationTokenSource().Token
                externalCancellation: registry.GetRootCancellationTokenSource().Token,
                // Parameter: provenanceTracker: registry.GetTheProvenanceTracker()
                provenanceTracker: registry.GetTheProvenanceTracker()
            );
        }
    }
}