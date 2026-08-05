namespace MetWorks.Persistence.Rollups;

public interface ILightningRollupRepository
{
    Task<bool> InitializeAsync(
        MetWorks.Data.Sqlite.ISqliteDatabase sqliteDatabase,
        MetWorks.Interfaces.IInstanceIdentifier instanceIdentifier,
        CancellationToken cancellationToken);

    Task RollupDayAsync(int maxBucketsPerRun, CancellationToken cancellationToken);
}
