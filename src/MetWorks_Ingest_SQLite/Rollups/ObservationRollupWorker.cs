using System.Diagnostics;

namespace MetWorks.Ingest.SQLite.Rollups;
public sealed class ObservationRollupWorker : ServiceBase
{
    bool _isDatabaseAvailable = false;
    int _isInitializing = 0;
    DateTime _lastConnectionAttempt = DateTime.MinValue;
    int _failureCount = 0;

    const int ReconnectionIntervalSeconds = 30;

    readonly SemaphoreSlim _gate = new(1, 1);

    Timer? _timer;
    Timer? _reconnectionTimer;

    string _connectionString = string.Empty;
    string _dbPath = string.Empty;
    string _installationId = string.Empty;

    RollupWatermarkStore? _watermarkStore;

    public ObservationRollupWorker()
    {
    }

    public async Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null)
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);

        iLoggerResilient.Information($"ObservationRollupWorker(SQLite).InitializeAsync() starting - thread={Environment.CurrentManagedThreadId}");

        try
        {
            InitializeBase(
                iLoggerResilient.ForContext(GetType()),
                iSettingRepository,
                iEventRelayBasic,
                externalCancellation,
                provenanceTracker
            );

            _connectionString = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_connectionString));

            _dbPath = iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.JsonToSQLiteGroupSettingsDefinition.BuildSettingPath(SettingConstants.JsonToSQLite_dbPath));

            _installationId = iInstanceIdentifier.GetOrCreateInstallationId();
            if (string.IsNullOrWhiteSpace(_installationId))
            {
                ILogger.Error("Installation id is empty. Aborting rollup worker initialization.");
                return false;
            }

            _watermarkStore = new RollupWatermarkStore(_installationId);

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                if (string.IsNullOrWhiteSpace(_dbPath))
                {
                    ILogger.Warning("⚠️ SQLite dbPath not configured. ObservationRollupWorker will not start.");
                    return true;
                }

                var resolvedDbPath = Path.IsPathRooted(_dbPath)
                    ? _dbPath
                    : Path.Combine(AppContext.BaseDirectory, _dbPath);

                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = resolvedDbPath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Cache = SqliteCacheMode.Shared
                };

                _connectionString = builder.ToString();
            }

            var connected = await TryEstablishConnectionAsync(_connectionString).ConfigureAwait(false);
            if (!connected)
            {
                ILogger.Warning("⚠️ ObservationRollupWorker initial connection failed. Starting in degraded mode.");
            }

            StartAsync();

            try { MarkReady(); } catch { }
            return true;
        }
        catch (Exception exception)
        {
            iLoggerResilient.Error($"❌ Error during ObservationRollupWorker initialization: {exception.Message}");
            StartAsync();
            return true;
        }
    }

    void StartAsync()
    {
        if (_timer is null)
        {
            _timer = new Timer(
                TimerCallback,
                null,
                dueTime: TimeSpan.FromSeconds(30),
                period: TimeSpan.FromSeconds(30));
        }

        StartReconnectionTimer();
    }

    void StartReconnectionTimer()
    {
        if (_reconnectionTimer is not null) return;

        _reconnectionTimer = new Timer(
            ReconnectionCallback,
            null,
            TimeSpan.FromSeconds(ReconnectionIntervalSeconds),
            TimeSpan.FromSeconds(ReconnectionIntervalSeconds));
    }

    void ReconnectionCallback(object? state)
    {
        if (_isDatabaseAvailable) return;
        if (LinkedCancellationToken.IsCancellationRequested) return;

        var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
        if (timeSinceLastAttempt < TimeSpan.FromSeconds(ReconnectionIntervalSeconds - 5))
            return;

        StartBackground(async token =>
        {
            try
            {
                if (token.IsCancellationRequested) return;
                if (string.IsNullOrWhiteSpace(_connectionString)) return;

                var connected = await TryEstablishConnectionAsync(_connectionString).ConfigureAwait(false);
                if (connected)
                    ILogger.Information("✅ ObservationRollupWorker SQLite reconnection SUCCESSFUL.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                ILogger.Warning($"⚠️ ObservationRollupWorker reconnection task failed: {ex.Message}");
            }
        });
    }

    async Task<bool> TryEstablishConnectionAsync(string connectionString)
    {
        if (LinkedCancellationToken.IsCancellationRequested) return false;

        if (Interlocked.CompareExchange(ref _isInitializing, 1, 0) != 0) return false;

        _lastConnectionAttempt = DateTime.UtcNow;

        try
        {
            using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedTestCts = CancellationTokenSource.CreateLinkedTokenSource(testCts.Token, LinkedCancellationToken);

            await using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync(linkedTestCts.Token).ConfigureAwait(false);

            await ApplyConnectionPragmasAsync(conn, linkedTestCts.Token).ConfigureAwait(false);

            await EnsureRollupSchemaAsync(conn, linkedTestCts.Token).ConfigureAwait(false);

            _isDatabaseAvailable = true;
            _failureCount = 0;
            return true;
        }
        catch (OperationCanceledException operationCanceledException)
        {
            _isDatabaseAvailable = false;
            return false;
        }
        catch (SqliteException sqliteException)
        {
            _isDatabaseAvailable = false;
            _failureCount++;
            ILogger.Warning($"⚠️ ObservationRollupWorker failed to establish SQLite connection: {sqliteException.Message} (ErrorCode: {sqliteException.SqliteErrorCode})");
            return false;
        }
        finally
        {
            Interlocked.Decrement(ref _isInitializing);
        }
    }

    void TimerCallback(object? state)
    {
        if (LinkedCancellationToken.IsCancellationRequested) return;

        if (!_isDatabaseAvailable) return;

        StartBackground(ct => RunOnceAsync(ct));
    }

    async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        if (!await _gate.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return;

        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            await ApplyConnectionPragmasAsync(conn, cancellationToken).ConfigureAwait(false);

            await EnsureRollupSchemaAsync(conn, cancellationToken).ConfigureAwait(false);

            await RollupAsync(
                conn,
                rollupTableName: "observation_rollup_1h",
                bucketWidthSeconds: 3600,
                maxBucketsPerRun: 24,
                cancellationToken).ConfigureAwait(false);

            await RollupAsync(
                conn,
                rollupTableName: "observation_rollup_1d",
                bucketWidthSeconds: 86400,
                maxBucketsPerRun: 7,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Debug.Assert(false);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 5 /* SQLITE_BUSY */)
        {
            ILogger.Debug("ObservationRollupWorker: SQLITE_BUSY; backing off.");
        }
        catch (SqliteException ex)
        {
            ILogger.Warning($"ObservationRollupWorker: SQLite error: {ex.Message} (code={ex.SqliteErrorCode})");
        }
        finally
        {
            try { _gate.Release(); } catch { }
        }
    }

    static async Task ApplyConnectionPragmasAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        // Apply per-connection settings to reduce writer contention.
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    async Task EnsureRollupSchemaAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var script in 
                new[] {
                    "Ingest/SQLite/rollup_state.sql",
                    "Ingest/SQLite/observation_rollup_1h.sql",
                    "Ingest/SQLite/observation_rollup_1d.sql"
                }
            )
            {
                var ddl = IResourceProvider.GetString(script);
                if (string.IsNullOrWhiteSpace(ddl))
                {
                    ILogger.Warning($"ObservationRollupWorker: missing embedded DDL '{script}'");
                    continue;
                }

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = ddl;
                cmd.CommandTimeout = 60;
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch(Exception exception)
        {
            ILogger.Warning($"ObservationRollupWorker: error ensuring rollup schema: {exception.Message}");
        }
    }

    async Task RollupAsync(
        SqliteConnection conn,
        string rollupTableName,
        int bucketWidthSeconds,
        int maxBucketsPerRun,
        CancellationToken cancellationToken
    )
    {
        if (maxBucketsPerRun <= 0) return;

        if (_watermarkStore is null) return;

        var watermark = await _watermarkStore.TryGetWatermarkAsync(
            conn,
            ObservationRollupSql.SourceTableName,
            bucketWidthSeconds,
            cancellationToken
        ).ConfigureAwait(false);

        var latest = await TryGetLatestDeviceEpochAsync(conn, cancellationToken)
            .ConfigureAwait(false);

        if (latest is null) return;

        var alignedLatestBucketStart = (latest.Value / bucketWidthSeconds) * bucketWidthSeconds;

        long startEpoch;
        if (watermark is null)
        {
            // Initialize watermark to the earliest bucket we can compute, based on first event.
            var earliest = await TryGetEarliestDeviceEpochAsync(conn, cancellationToken)
                .ConfigureAwait(false);
            if (earliest is null) return;

            startEpoch = (earliest.Value / bucketWidthSeconds) * bucketWidthSeconds;
        }
        else
        {
            startEpoch = watermark.Value;
        }

        // Don't roll up the current (possibly incomplete) bucket.
        var endExclusive = alignedLatestBucketStart;
        if (startEpoch >= endExclusive)
            return;

        // Cap work per run.
        var maxEndExclusive = startEpoch + (long)bucketWidthSeconds * maxBucketsPerRun;
        if (maxEndExclusive < endExclusive)
            endExclusive = maxEndExclusive;

        // Ensure endExclusive is bucket-aligned.
        endExclusive = (endExclusive / bucketWidthSeconds) * bucketWidthSeconds;

        if (endExclusive <= startEpoch)
            return;

        var sql = ObservationRollupSql.BuildUpsertRollupSql(rollupTableName, bucketWidthSeconds);

        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.CommandTimeout = 60;

            cmd.Parameters.AddWithValue("$installation_id", _installationId);
            cmd.Parameters.AddWithValue("$range_start_epoch", startEpoch);
            cmd.Parameters.AddWithValue("$range_end_epoch", endExclusive);

            _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await _watermarkStore.UpsertWatermarkAsync(
            conn,
            ObservationRollupSql.SourceTableName,
            bucketWidthSeconds,
            watermarkDeviceEpoch: endExclusive,
            cancellationToken).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        ILogger.Debug($"ObservationRollupWorker: rolled up {rollupTableName} [{startEpoch}, {endExclusive})");
    }

    async Task<long?> TryGetEarliestDeviceEpochAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MIN(device_received_utc_timestamp_epoch) FROM observation WHERE installation_id = $installation_id;";
        cmd.Parameters.AddWithValue("$installation_id", _installationId);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (scalar is null || scalar is DBNull)
            return null;

        return Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
    }

    async Task<long?> TryGetLatestDeviceEpochAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MAX(device_received_utc_timestamp_epoch) FROM observation WHERE installation_id = $installation_id;";
        cmd.Parameters.AddWithValue("$installation_id", _installationId);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (scalar is null || scalar is DBNull)
            return null;

        return Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
    }

    protected override async Task OnDisposeAsync()
    {
        try
        {
            try { _timer?.Dispose(); } catch { }
            _timer = null;

            try { _reconnectionTimer?.Dispose(); } catch { }
            _reconnectionTimer = null;

            try { await _gate.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false); } catch { }
            try { _gate.Release(); } catch { }
        }
        finally
        {
            _gate.Dispose();
        }

        await Task.CompletedTask;
    }
}
