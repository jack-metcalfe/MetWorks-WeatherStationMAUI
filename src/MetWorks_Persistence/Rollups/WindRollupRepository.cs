using MetWorks.Data.Sqlite;
using MetWorks.Interfaces;

namespace MetWorks.Persistence.Rollups;

public sealed class WindRollupRepository : IWindRollupRepository
{
    ISqliteDatabase? _sqliteDatabase;
    IInstanceIdentifier? _instanceIdentifier;

    public WindRollupRepository()
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
        _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(WindRollupRepository)} has not been initialized.");

    IInstanceIdentifier GetInitializedInstanceIdentifier() =>
        _instanceIdentifier ?? throw new InvalidOperationException($"{nameof(WindRollupRepository)} has not been initialized.");

    public Task RollupHourAsync(int maxBucketsPerRun, CancellationToken cancellationToken)
    {
        return RollupAsync(
            rollupTableName: "wind_rollup_1h",
            bucketWidthSeconds: 3600,
            maxBucketsPerRun,
            cancellationToken);
    }

    public Task RollupDayAsync(int maxBucketsPerRun, CancellationToken cancellationToken)
    {
        return RollupAsync(
            rollupTableName: "wind_rollup_1d",
            bucketWidthSeconds: 86400,
            maxBucketsPerRun,
            cancellationToken);
    }

    async Task RollupAsync(
        string rollupTableName,
        int bucketWidthSeconds,
        int maxBucketsPerRun,
        CancellationToken cancellationToken)
    {
        if (maxBucketsPerRun <= 0) return;

        var installationId = GetInitializedInstanceIdentifier().GetOrCreateInstallationId();
        if (string.IsNullOrWhiteSpace(installationId))
            throw new InvalidOperationException("Installation id is empty.");

        var watermarkStore = new RollupWatermarkStore(installationId);

        await using var session = await GetInitializedSqliteDatabase().OpenSessionAsync(cancellationToken).ConfigureAwait(false);

        var watermark = await watermarkStore.TryGetWatermarkAsync(
            session,
            WindRollupSql.SourceTableName,
            bucketWidthSeconds,
            cancellationToken).ConfigureAwait(false);

        var latest = await TryGetLatestDeviceEpochAsync(session, installationId, cancellationToken).ConfigureAwait(false);
        if (latest is null) return;

        var alignedLatestBucketStart = (latest.Value / bucketWidthSeconds) * bucketWidthSeconds;

        long startEpoch;
        if (watermark is null)
        {
            var earliest = await TryGetEarliestDeviceEpochAsync(session, installationId, cancellationToken).ConfigureAwait(false);
            if (earliest is null) return;

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

        var sql = WindRollupSql.BuildUpsertRollupSql(rollupTableName, bucketWidthSeconds);

        await session.ExecuteInTransactionAsync(async (txSession, ct) =>
        {
            _ = await txSession.ExecuteAsync(
                sql,
                [
                    new DbParam("$installation_id", installationId),
                    new DbParam("$range_start_epoch", startEpoch),
                    new DbParam("$range_end_epoch", endExclusive)
                ],
                ct).ConfigureAwait(false);

            await watermarkStore.UpsertWatermarkAsync(
                txSession,
                WindRollupSql.SourceTableName,
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
            "SELECT MIN(device_received_utc_timestamp_epoch) FROM wind WHERE installation_id = $installation_id;",
            [new DbParam("$installation_id", installationId)],
            cancellationToken);
    }

    static Task<long?> TryGetLatestDeviceEpochAsync(
        ISqliteSession session,
        string installationId,
        CancellationToken cancellationToken)
    {
        return session.ScalarAsync<long?>(
            "SELECT MAX(device_received_utc_timestamp_epoch) FROM wind WHERE installation_id = $installation_id;",
            [new DbParam("$installation_id", installationId)],
            cancellationToken);
    }
}
