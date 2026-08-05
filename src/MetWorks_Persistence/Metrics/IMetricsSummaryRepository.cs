namespace MetWorks.Persistence.Metrics;

public interface IMetricsSummaryRepository
{
    Task InsertAsync(MetricsSummaryInsertRow row, CancellationToken cancellationToken);
}
