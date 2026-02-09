// Template:            Assignments.Initializer
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Template:            File.Header
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Generated On:        2026-02-09T02:55:58.0510994Z
#nullable enable
using System.Threading.Tasks;

namespace MetWorks.ServiceRegistry
{
    // Per-instance async initializer.
    // Declared as partial to allow modularization if needed.
    // Only emitted for instances that have assignment-driven initialization.
    internal static partial class TheMetricsSummaryIngestor_Initializer
    {
        public static async Task Initialize_TheMetricsSummaryIngestorAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetTheMetricsSummaryIngestor_Internal();

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                iLoggerResilient: registry.GetTheLoggerResilient(),
                iSettingRepository: registry.GetTheSettingRepository(),
                iEventRelayBasic: registry.GetTheEventRelayBasic(),
                installationId: registry.GetTheInstanceIdentifier().InstallationId,
                metricsLatestSnapshotStore: registry.GetTheMetricsLatestSnapshotStore(),
                externalCancellation: registry.GetRootCancellationTokenSource().Token,
                provenanceTracker: registry.GetTheProvenanceTracker()
            ).ConfigureAwait(false);
        }
    }
}
