namespace MetWorks.Persistence.StreamShipping;

public interface IStreamShippingRepository
{
    Task<ShipperStateSnapshot?> TryGetStateAsync(string table, CancellationToken cancellationToken);

    Task UpsertShippingProgressAsync(
        string table,
        long? lastShippedRowId,
        long? lastAckedRowId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<StandardReadingRow>> ReadStandardReadingsBatchAsync(
        string table,
        string installationId,
        long lastAckedRowId,
        int maxRows,
        CancellationToken cancellationToken
    );
}
