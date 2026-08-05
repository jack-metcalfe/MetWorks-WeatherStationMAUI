namespace MetWorks.Persistence.Logging;

public interface ILoggerSqliteRepository
{
    Task InsertAsync(string table, LoggerSqliteLogEvent logEvent, CancellationToken cancellationToken);
}
