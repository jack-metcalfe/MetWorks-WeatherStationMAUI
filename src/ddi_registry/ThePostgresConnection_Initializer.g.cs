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
    internal static partial class ThePostgresConnection_Initializer
    {
        public static async Task Initialize_ThePostgresConnectionAsync(Registry registry)
        {
            // Step 1: retrieve the created instance from the registry.
            // Internal accessor ensures we always get the concrete class.
            var instance = registry.GetThePostgresConnection_Internal();

            // Step 2: call its async initializer with assignment values.
            // All argument expressions are fully computed by the pipeline.
            await instance.InitializeAsync(
                // Parameter: defaultValue: "Host=sampleIP;Username=sampleUser;Password=samplePassword;Database=sampleDatabase;SslMode=Disable"
                defaultValue: "Host=sampleIP;Username=sampleUser;Password=samplePassword;Database=sampleDatabase;SslMode=Disable",
                // Parameter: description: "Connection string for save weather data to Postgres"
                description: "Connection string for save weather data to Postgres",
                // Parameter: expectedValueType: "String"
                expectedValueType: "String",
                // Parameter: group: "database"
                group: "database",
                // Parameter: isEditable: true
                isEditable: true,
                // Parameter: path: "/services/RawPacketRecordTypedInPostgresOut/connectionString"
                path: "/services/RawPacketRecordTypedInPostgresOut/connectionString",
                // Parameter: enumValues: Array.Empty<System.String>()
                enumValues: Array.Empty<System.String>(),
                // Parameter: isSecret: true
                isSecret: true
            );
        }
    }
}