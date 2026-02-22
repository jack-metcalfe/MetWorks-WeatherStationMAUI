// Template:            Assignments.Initializer
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Template:            File.Header
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Generated On:        2026-02-22T22:24:44.5468545Z
#nullable enable
using System.Threading.Tasks;

namespace MetWorks.ServiceRegistry
{
    // Per-instance async initializer.
    // Declared as partial to allow modularization if needed.
    // Only emitted for instances that have assignment-driven initialization.
    internal static partial class TheLoggingDatabaseReadiness_Initializer
    {
        public static async Task Initialize_TheLoggingDatabaseReadinessAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetTheLoggingDatabaseReadiness_Internal();

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
