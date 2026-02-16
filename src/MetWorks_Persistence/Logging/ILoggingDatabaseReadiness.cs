namespace MetWorks.Persistence.Logging;

public interface ILoggingDatabaseReadiness
{
    Task EnsureReadyAsync(CancellationToken cancellationToken);

    Task EnsureReadyAsync(string table, CancellationToken cancellationToken);
}
