using MetWorks.Data.Sqlite;
using MetWorks.Resource.Store;

namespace MetWorks.Persistence.Ingest;

internal static class RawPacketSqlScripts
{
    internal static IReadOnlyList<SqlScript> GetAll() =>
    [
        ReadResourceSql("observation"),
        ReadResourceSql("wind"),
        ReadResourceSql("precipitation"),
        ReadResourceSql("lightning"),
    ];

    static SqlScript ReadResourceSql(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Script name is required.", nameof(name));

        var sql = ResourceProvider.GetString($"Ingest/SQLite/{name}.sql");
        if (string.IsNullOrWhiteSpace(sql))
            throw new InvalidOperationException($"Missing embedded SQLite resource 'Ingest/SQLite/{name}.sql'.");

        return new SqlScript(name, sql);
    }
}
