// Template:            Assignments.Initializer
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Template:            File.Header
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Generated On:        2026-03-01T07:07:53.2766146Z
#nullable enable
using System.Threading.Tasks;

namespace MetWorks.ServiceRegistry
{
    // Per-instance async initializer.
    // Declared as partial to allow modularization if needed.
    // Only emitted for instances that have assignment-driven initialization.
    internal static partial class TheStreamShippingHttpClientProvider_Initializer
    {
        public static async Task Initialize_TheStreamShippingHttpClientProviderAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetTheStreamShippingHttpClientProvider_Internal();

            await registry.WhenTheLoggerResilientInitializedAsync().ConfigureAwait(false);
            await registry.WhenTheSettingRepositoryInitializedAsync().ConfigureAwait(false);
            await registry.WhenTheProvenanceTrackerInitializedAsync().ConfigureAwait(false);

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                iLogger: registry.GetTheLoggerResilient(),
                iSettingRepository: registry.GetTheSettingRepository(),
                iEventRelayBasic: registry.GetTheEventRelayBasic(),
                externalCancellation: registry.GetTheRootCancellationTokenSource().Token,
                provenanceTracker: registry.GetTheProvenanceTracker()
            ).ConfigureAwait(false);
        }
    }
}
