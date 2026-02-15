using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MetWorks.Data.Sqlite;

sealed class SqliteSession(SqliteConnection connection) : ISqliteSession, IAsyncDisposable
{
    readonly SqliteConnection _connection = connection;

    public async Task<int> ExecuteAsync(string sql, IReadOnlyList<DbParam>? parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql)) return 0;

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        AddParameters(cmd, parameters);

        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<T?> ScalarAsync<T>(string sql, IReadOnlyList<DbParam>? parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql)) return default;

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        AddParameters(cmd, parameters);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (scalar is null || scalar is DBNull)
            return default;

        return (T)Convert.ChangeType(scalar, typeof(T), CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        IReadOnlyList<DbParam>? parameters,
        Func<DbRow, T> map,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (string.IsNullOrWhiteSpace(sql))
            return Array.Empty<T>();

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        AddParameters(cmd, parameters);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var results = new List<T>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                values[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            results.Add(map(new DbRow(values)));
        }

        return results;
    }

    public async Task ExecuteInTransactionAsync(Func<ISqliteSession, CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        await using var tx = await _connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await work(this, cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            try { await tx.RollbackAsync(cancellationToken).ConfigureAwait(false); } catch { }
            throw;
        }
    }

    static void AddParameters(SqliteCommand cmd, IReadOnlyList<DbParam>? parameters)
    {
        if (parameters is null || parameters.Count == 0) return;

        foreach (var p in parameters)
        {
            if (string.IsNullOrWhiteSpace(p.Name))
                continue;

            cmd.Parameters.AddWithValue(p.Name, p.Value ?? DBNull.Value);
        }
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
