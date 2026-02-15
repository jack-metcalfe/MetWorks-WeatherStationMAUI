using Microsoft.Data.Sqlite;

namespace MetWorks.Data.Sqlite;

public static class SqliteFeatureProbe
{
    public static async Task<bool> SupportsGeneratedColumnsAsync(string connectionString, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var create = connection.CreateCommand();
        create.CommandText = "CREATE TEMP TABLE __mw_gc_probe (a INTEGER, b INTEGER GENERATED ALWAYS AS (a + 1) STORED);";
        await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        await using var drop = connection.CreateCommand();
        drop.CommandText = "DROP TABLE __mw_gc_probe;";
        await drop.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }
}
