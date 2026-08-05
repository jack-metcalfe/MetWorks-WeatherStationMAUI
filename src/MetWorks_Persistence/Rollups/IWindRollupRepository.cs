namespace MetWorks.Persistence.Rollups;

public interface IWindRollupRepository
{
    Task<bool> InitializeAsync(
        MetWorks.Data.Sqlite.ISqliteDatabase sqliteDatabase,
        MetWorks.Interfaces.IInstanceIdentifier instanceIdentifier,
        CancellationToken cancellationToken);

    Task RollupHourAsync(int maxBucketsPerRun, CancellationToken cancellationToken);

    Task RollupDayAsync(int maxBucketsPerRun, CancellationToken cancellationToken);
}
