namespace MetWorks.Interfaces;
public interface ITempestRestClient
{
    Task<TempestStationSnapshot> GetStationSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed record TempestStationSnapshot(
    long StationId,
    DateTimeOffset RetrievedUtc,
    string RawJson
);
