namespace MetWorks.Ingest.SQLite;
internal static class SqliteFeatureProbe
{
    internal static async Task<bool> SupportsGeneratedColumnsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT sqlite_version();";
        var version = (await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))?.ToString();

        // Most practical check: attempt to create a table with a generated column.
        // If SQLite runtime doesn't support it, this will throw.
        await using var create = connection.CreateCommand();
        create.CommandText = "CREATE TEMP TABLE __mw_gc_probe (a INTEGER, b INTEGER GENERATED ALWAYS AS (a + 1) STORED);";
        await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        await using var drop = connection.CreateCommand();
        drop.CommandText = "DROP TABLE __mw_gc_probe;";
        await drop.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return !string.IsNullOrWhiteSpace(version);
    }
}
