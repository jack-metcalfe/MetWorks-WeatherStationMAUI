// Template:            Assignments.Initializer
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Template:            File.Header
// Version:             1.1
// Template Requested:  Assignments.Initializer
#nullable enable
using System.Threading.Tasks;

namespace MetWorks.ServiceRegistry
{
    // Per-instance async initializer.
    // Declared as partial to allow modularization if needed.
    // Only emitted for instances that have assignment-driven initialization.
    internal static partial class TheLightningStreamShipper_Initializer
    {
        public static async Task Initialize_TheLightningStreamShipperAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetTheLightningStreamShipper_Internal();

            await registry.WhenTheLoggerResilientInitializedAsync().ConfigureAwait(false);
            await registry.WhenTheSettingRepositoryInitializedAsync().ConfigureAwait(false);
            await registry.WhenTheInstanceIdentifierInitializedAsync().ConfigureAwait(false);
            await registry.WhenTheStreamShippingDatabaseReadinessInitializedAsync().ConfigureAwait(false);
            await registry.WhenTheStreamShippingRepositoryInitializedAsync().ConfigureAwait(false);
            await registry.WhenTheStreamShippingHttpClientProviderInitializedAsync().ConfigureAwait(false);
            await registry.WhenTheProvenanceTrackerInitializedAsync().ConfigureAwait(false);

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                iLogger: registry.GetTheLoggerResilient(),
                iSettingRepository: registry.GetTheSettingRepository(),
                iEventRelayBasic: registry.GetTheEventRelayBasic(),
                iInstanceIdentifier: registry.GetTheInstanceIdentifier(),
                streamShippingDatabaseReadiness: registry.GetTheStreamShippingDatabaseReadiness(),
                streamShippingRepository: registry.GetTheStreamShippingRepository(),
                httpClient: registry.GetTheStreamShippingHttpClientProvider().Client,
                externalCancellation: registry.GetTheRootCancellationTokenSource().Token,
                provenanceTracker: registry.GetTheProvenanceTracker()
            ).ConfigureAwait(false);
        }
    }
}
