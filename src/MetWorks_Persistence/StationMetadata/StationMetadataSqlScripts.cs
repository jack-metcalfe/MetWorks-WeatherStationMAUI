using MetWorks.Data.Sqlite;
using MetWorks.Resource.Store;

namespace MetWorks.Persistence.StationMetadata;

internal static class StationMetadataSqlScripts
{
    internal static IReadOnlyList<SqlScript> GetAll() =>
    [
        new(
            Name: "station_metadata",
            Sql: ResourceProvider.GetString("Ingest/SQLite/station_metadata.sql")
                ?? throw new InvalidOperationException("Embedded resource 'Ingest/SQLite/station_metadata.sql' not found in MetWorks.Resource.Store assembly."))
    ];
}
