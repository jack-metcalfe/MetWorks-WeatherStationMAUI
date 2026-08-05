namespace MetWorks.Persistence.StreamShipping;

public sealed record StandardReadingRow(
    long RowId,
    string Id,
    long ApplicationReceivedUtcEpoch,
    string JsonDocumentOriginal);
