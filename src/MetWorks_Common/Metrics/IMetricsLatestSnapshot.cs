namespace MetWorks.Common.Metrics;

public interface IMetricsLatestSnapshot
{
    MetricsLatestSnapshot Current { get; }

    MetricsStructuredSnapshot? CurrentStructured { get; }
}
