namespace MetWorks.Persistence.Rollups;

public interface IRollupsDatabaseReadiness
{
    Task EnsureReadyAsync(CancellationToken cancellationToken);
}
