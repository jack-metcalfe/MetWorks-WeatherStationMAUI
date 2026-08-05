using MetWorks.Data.Sqlite;
using MetWorks.Interfaces;

namespace MetWorks.Persistence.Rollups;

public sealed class PrecipitationRollupRepository : IPrecipitationRollupRepository
{
    const string SourceTableName = "precipitation";

    ISqliteDatabase? _sqliteDatabase;
    IInstanceIdentifier? _instanceIdentifier;

    public PrecipitationRollupRepository()
    {
    }

    public Task<bool> InitializeAsync(
        ISqliteDatabase sqliteDatabase,
        IInstanceIdentifier instanceIdentifier,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);
        ArgumentNullException.ThrowIfNull(instanceIdentifier);

        _sqliteDatabase = sqliteDatabase;
        _instanceIdentifier = instanceIdentifier;
        return Task.FromResult(true);
    }

    ISqliteDatabase GetInitializedSqliteDatabase() =>
        _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(PrecipitationRollupRepository)} has not been initialized.");

    IInstanceIdentifier GetInitializedInstanceIdentifier() =>
        _instanceIdentifier ?? throw new InvalidOperationException($"{nameof(PrecipitationRollupRepository)} has not been initialized.");

    public async Task AdvanceWatermarkAsync(
        int bucketWidthSeconds,
        int maxBucketsPerRun,
        CancellationToken cancellationToken)
    {
        if (bucketWidthSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(bucketWidthSeconds));

        if (maxBucketsPerRun <= 0)
            return;

        var installationId = GetInitializedInstanceIdentifier().GetOrCreateInstallationId();
        if (string.IsNullOrWhiteSpace(installationId))
            throw new InvalidOperationException("Installation id is empty.");

        var watermarkStore = new RollupWatermarkStore(installationId);

        await using var session = await GetInitializedSqliteDatabase().OpenSessionAsync(cancellationToken).ConfigureAwait(false);

        var watermark = await watermarkStore.TryGetWatermarkAsync(
            session,
            SourceTableName,
            bucketWidthSeconds,
            cancellationToken).ConfigureAwait(false);

        var latest = await TryGetLatestDeviceEpochAsync(session, installationId, cancellationToken).ConfigureAwait(false);
        if (latest is null)
            return;

        var alignedLatestBucketStart = (latest.Value / bucketWidthSeconds) * bucketWidthSeconds;

        long startEpoch;
        if (watermark is null)
        {
            var earliest = await TryGetEarliestDeviceEpochAsync(session, installationId, cancellationToken).ConfigureAwait(false);
            if (earliest is null)
                return;

            startEpoch = (earliest.Value / bucketWidthSeconds) * bucketWidthSeconds;
        }
        else
        {
            startEpoch = watermark.Value;
        }

        var endExclusive = alignedLatestBucketStart;
        if (startEpoch >= endExclusive)
            return;

        var maxEndExclusive = startEpoch + (long)bucketWidthSeconds * maxBucketsPerRun;
        if (maxEndExclusive < endExclusive)
            endExclusive = maxEndExclusive;

        endExclusive = (endExclusive / bucketWidthSeconds) * bucketWidthSeconds;

        if (endExclusive <= startEpoch)
            return;

        await session.ExecuteInTransactionAsync(async (txSession, ct) =>
        {
            await watermarkStore.UpsertWatermarkAsync(
                txSession,
                SourceTableName,
                bucketWidthSeconds,
                watermarkDeviceEpoch: endExclusive,
                ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    static Task<long?> TryGetEarliestDeviceEpochAsync(
        ISqliteSession session,
        string installationId,
        CancellationToken cancellationToken)
    {
        return session.ScalarAsync<long?>(
            "SELECT MIN(device_received_utc_timestamp_epoch) FROM precipitation WHERE installation_id = $installation_id;",
            [new DbParam("$installation_id", installationId)],
            cancellationToken);
    }

    static Task<long?> TryGetLatestDeviceEpochAsync(
        ISqliteSession session,
        string installationId,
        CancellationToken cancellationToken)
    {
        return session.ScalarAsync<long?>(
            "SELECT MAX(device_received_utc_timestamp_epoch) FROM precipitation WHERE installation_id = $installation_id;",
            [new DbParam("$installation_id", installationId)],
            cancellationToken);
    }
}
