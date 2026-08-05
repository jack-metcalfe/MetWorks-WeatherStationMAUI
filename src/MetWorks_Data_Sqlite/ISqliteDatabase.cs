namespace MetWorks.Data.Sqlite;

public interface ISqliteDatabase
{
    Task<ISqliteSession> OpenSessionAsync(CancellationToken cancellationToken);

    Task ExecuteDdlAsync(IReadOnlyList<SqlScript> scripts, CancellationToken cancellationToken);
}
