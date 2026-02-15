namespace MetWorks.Persistence.StreamShipping;

public interface IStreamShippingDatabaseReadiness
{
    Task EnsureReadyAsync(CancellationToken cancellationToken);
}
