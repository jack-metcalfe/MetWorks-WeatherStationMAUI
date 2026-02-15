namespace MetWorks.Persistence.StationMetadata;

public interface IStationMetadataDatabaseReadiness
{
    Task EnsureReadyAsync(CancellationToken cancellationToken);
}
