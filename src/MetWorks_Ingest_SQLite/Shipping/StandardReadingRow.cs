namespace MetWorks.Ingest.SQLite.Shipping;

internal sealed record StandardReadingRow(long RowId, string Id, long ApplicationReceivedUtcEpoch, string JsonDocumentOriginal);
