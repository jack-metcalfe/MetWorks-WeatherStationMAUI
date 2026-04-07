using MetWorks.Data.Sqlite;
using MetWorks.Resource.Store;

namespace MetWorks.Persistence.StreamShipping;

internal static class StreamShippingSqlScripts
{
    internal static IReadOnlyList<SqlScript> GetAll() =>
    [
        new(
            Name: "shipper_state",
            Sql: ResourceProvider.GetString("Ingest/SQLite/shipper_state.sql")
                ?? throw new InvalidOperationException("Embedded resource 'Ingest/SQLite/shipper_state.sql' not found in MetWorks.Resource.Store assembly."))
    ];
}
