namespace MetWorks.Persistence.Ingest;

public interface IRawPacketDatabaseReadiness
{
    Task EnsureReadyAsync(CancellationToken cancellationToken);
}
