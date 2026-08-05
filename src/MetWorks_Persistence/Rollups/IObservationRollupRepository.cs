namespace MetWorks.Persistence.Rollups;

public interface IObservationRollupRepository
{
    Task RollupHourAsync(int maxBucketsPerRun, CancellationToken cancellationToken);

    Task RollupDayAsync(int maxBucketsPerRun, CancellationToken cancellationToken);
}
