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
    internal static partial class TheMetricsDatabaseReadiness_Initializer
    {
        public static async Task Initialize_TheMetricsDatabaseReadinessAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetTheMetricsDatabaseReadiness_Internal();

            await registry.WhenTheSqliteDatabaseInitializedAsync().ConfigureAwait(false);

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                sqliteDatabase: registry.GetTheSqliteDatabase(),
                cancellationToken: registry.GetTheRootCancellationTokenSource().Token
            ).ConfigureAwait(false);
        }
    }
}
