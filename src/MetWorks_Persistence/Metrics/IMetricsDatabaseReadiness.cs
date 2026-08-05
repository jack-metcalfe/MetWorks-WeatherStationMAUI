namespace MetWorks.Persistence.Metrics;

public interface IMetricsDatabaseReadiness
{
    Task EnsureReadyAsync(string table, CancellationToken cancellationToken);
}
