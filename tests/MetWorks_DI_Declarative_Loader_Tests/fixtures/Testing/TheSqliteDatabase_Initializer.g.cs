// Template:            Assignments.Initializer
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Template:            File.Header
// Version:             1.1
// Template Requested:  Assignments.Initializer
// Generated On:        2026-02-20T04:28:36.4052461Z
#nullable enable
using System.Threading.Tasks;

namespace MetWorks.ServiceRegistry
{
    // Per-instance async initializer.
    // Declared as partial to allow modularization if needed.
    // Only emitted for instances that have assignment-driven initialization.
    internal static partial class TheSqliteDatabase_Initializer
    {
        public static async Task Initialize_TheSqliteDatabaseAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetTheSqliteDatabase_Internal();

            await registry.WhenTheLoggerFileInitializedAsync().ConfigureAwait(false);
            await registry.WhenTheSqliteDatabaseOptionsInitializedAsync().ConfigureAwait(false);

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                iLogger: registry.GetTheLoggerFile(),
                options: registry.GetTheSqliteDatabaseOptions(),
                cancellationToken: registry.GetTheRootCancellationTokenSource().Token
            ).ConfigureAwait(false);
        }
    }
}
