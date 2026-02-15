namespace MetWorks.Persistence.Ingest;

public sealed record RawPacketRecord(
    string Table,
    string Id,
    string JsonDocumentOriginal,
    long ApplicationReceivedUtcTimestampz,
    string? InstallationId);
