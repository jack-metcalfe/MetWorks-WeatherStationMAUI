namespace MetWorks.Persistence.Ingest;

public interface IRawPacketIngestRepository
{
    Task InsertAsync(RawPacketRecord record, CancellationToken cancellationToken);

    Task ProbeJsonAsync(CancellationToken cancellationToken);
}
