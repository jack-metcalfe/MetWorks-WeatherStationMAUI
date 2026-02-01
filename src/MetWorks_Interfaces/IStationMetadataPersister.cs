namespace MetWorks.Interfaces;
public interface IStationMetadataPersister
{
    Task PersistAsync(StationMetadata metadata, CancellationToken cancellationToken = default);
}
