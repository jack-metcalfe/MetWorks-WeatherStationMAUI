using MetWorks.Data.Sqlite;
using MetWorks.Resource.Store;

namespace MetWorks.Persistence.Rollups;

public static class RollupsSqlScripts
{
    public static IReadOnlyList<SqlScript> GetAll() =>
    [
        ReadResourceSql("rollup_state"),
        ReadResourceSql("observation_rollup_1h"),
        ReadResourceSql("observation_rollup_1d"),
        ReadResourceSql("wind_rollup_1h"),
        ReadResourceSql("wind_rollup_1d"),
        ReadResourceSql("lightning_rollup_1d"),
    ];

    static SqlScript ReadResourceSql(string name)
    {
        var sql = ResourceProvider.GetString($"Ingest/SQLite/{name}.sql");
        if (string.IsNullOrWhiteSpace(sql))
            throw new InvalidOperationException($"Missing embedded SQLite resource 'Ingest/SQLite/{name}.sql'.");

        return new SqlScript(name, sql);
    }
}
