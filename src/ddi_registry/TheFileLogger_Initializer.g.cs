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
    internal static partial class TheFileLogger_Initializer
    {
        public static async Task Initialize_TheFileLoggerAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetTheFileLogger_Internal();

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                // Parameter: fileSizeLimitBytes: 10485760
                fileSizeLimitBytes: 10485760,
                // Parameter: minimumLevel: "Information"
                minimumLevel: "Information",
                // Parameter: outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                // Parameter: path: "C:/Temp/Logs/log-.txt"
                path: "C:/Temp/Logs/log-.txt",
                // Parameter: retainedFileCountLimit: 7
                retainedFileCountLimit: 7,
                // Parameter: rollingInterval: "Day"
                rollingInterval: "Day",
                // Parameter: rollOnFileSizeLimit: true
                rollOnFileSizeLimit: true
            );
        }
    }
}