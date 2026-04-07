namespace MetWorks.Persistence.StreamShipping;

public sealed record ShipperStateSnapshot(
    string InstallationId,
    string Table,
    long? LastShippedRowId,
    long? LastAckedRowId,
    long? LastLossyDeletedRowId,
    long LossyDeletedRowCount,
    DateTime? LastLossyDeleteUtc);
