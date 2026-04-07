namespace MetWorks.Persistence.StreamShipping;

public interface ILoggerStreamShippingRepository
{
    Task<IReadOnlyList<LoggerLogRow>> ReadLoggerBatchAsync(
        string table,
        long lastAckedRowId,
        int maxRows,
        CancellationToken cancellationToken);

    Task<int> PurgeAckedOlderThanAsync(
        string table,
        long ackedUpToRowId,
        DateTime cutoffUtc,
        CancellationToken cancellationToken);

    Task RecordLossyDeletionAsync(
        string table,
        long deletedThroughRowId,
        int deletedRowCount,
        DateTime deletionUtc,
        CancellationToken cancellationToken);
}
