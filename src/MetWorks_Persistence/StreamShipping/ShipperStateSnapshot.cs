namespace MetWorks.Persistence.StreamShipping;

public sealed record ShipperStateSnapshot(
    string InstallationId,
    string Source,
    long? LastShippedRowId,
    long? LastAckedRowId,
    long? LastLossyDeletedRowId,
    long LossyDeletedRowCount,
    DateTime? LastLossyDeleteUtc);
