namespace MetWorks.Persistence.StationMetadata;

public interface IStationMetadataRepository
{
    Task InsertAsync(StationMetadataInsertRow row, CancellationToken cancellationToken);
}
