namespace MetWorks.Interfaces;
public interface IMetricsSummaryPersister
{
    Task PersistAsync(
        DateTime capturedUtc, 
        int captureIntervalSeconds, 
        int schemaVersion, 
        string jsonMetricsSummary, 
        CancellationToken cancellationToken = default
    );
}
