namespace MetWorks.Persistence.Ingest;

public interface IRawPacketIngestRepository
{
    Task InsertAsync(RawPacketRecord record, CancellationToken cancellationToken);

    Task ProbeJson1Async(CancellationToken cancellationToken);
}
