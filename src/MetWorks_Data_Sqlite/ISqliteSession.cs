namespace MetWorks.Data.Sqlite;

public interface ISqliteSession : IAsyncDisposable
{
    Task<int> ExecuteAsync(string sql, IReadOnlyList<DbParam>? parameters, CancellationToken cancellationToken);

    Task<T?> ScalarAsync<T>(
        string sql,
        IReadOnlyList<DbParam>? parameters,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        IReadOnlyList<DbParam>? parameters,
        Func<DbRow, T> map,
        CancellationToken cancellationToken);

    Task ExecuteInTransactionAsync(Func<ISqliteSession, CancellationToken, Task> work, CancellationToken cancellationToken);
}
