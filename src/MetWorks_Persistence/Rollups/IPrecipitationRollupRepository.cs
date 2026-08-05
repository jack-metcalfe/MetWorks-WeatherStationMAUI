namespace MetWorks.Persistence.Rollups;

public interface IPrecipitationRollupRepository
{
    Task<bool> InitializeAsync(
        MetWorks.Data.Sqlite.ISqliteDatabase sqliteDatabase,
        MetWorks.Interfaces.IInstanceIdentifier instanceIdentifier,
        CancellationToken cancellationToken);

    Task AdvanceWatermarkAsync(int bucketWidthSeconds, int maxBucketsPerRun, CancellationToken cancellationToken);
}
