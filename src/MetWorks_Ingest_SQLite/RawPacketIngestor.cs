namespace MetWorks.Ingest.SQLite;
/// <summary>
/// SQLite listener sink with robust lifecycle and cooperative cancellation support.
/// </summary>
public sealed class RawPacketIngestor : ServiceBase
{
    bool _isDatabaseAvailable = false;
    int _isInitializing = 0;
    DateTime _lastConnectionAttempt = DateTime.MinValue;
    DateTime _lastSuccessfulWrite = DateTime.MinValue;
    int _failureCount = 0;

    long _totalWrites;
    long _totalWriteFailures;
    DateTime _lastStatsLogUtc = DateTime.MinValue;
    long _writesAtLastStatsLog;

    const int MaxConsecutiveFailures = 5;

    static readonly TimeSpan StatsLogInterval = TimeSpan.FromMinutes(1);

	bool _bufferingEnabled = false;

    Timer? _healthCheckTimer;

    IInstanceIdentifier? _instanceIdentifier;
    Guid _installationIdGuid;
    bool _shouldRotateInstallationIdOnDbCreate;

    SqliteWriteCoordinator? _writeCoordinator;
    IRawPacketDatabaseReadiness? _rawPacketDatabaseReadiness;
    IRawPacketIngestRepository? _rawPacketIngestRepository;

    int _schemaInitialized = 0;
    string? _resolvedDbPath;
    bool _dbFileExistedAtStartup;

    public RawPacketIngestor()
    {
    }

    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        SqliteWriteCoordinator sqliteWriteCoordinator,
        IRawPacketDatabaseReadiness rawPacketDatabaseReadiness,
        IRawPacketIngestRepository rawPacketIngestRepository,
        CancellationToken externalCancellation,
        ProvenanceTracker? provenanceTracker
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);
        ArgumentNullException.ThrowIfNull(sqliteWriteCoordinator);
        ArgumentNullException.ThrowIfNull(rawPacketDatabaseReadiness);
        ArgumentNullException.ThrowIfNull(rawPacketIngestRepository);

        iLogger.Information($"RawPacketIngestor(SQLite).InitializeAsync() starting - thread={Environment.CurrentManagedThreadId}");

        try
        {
            InitializeBase(
                iLogger.ForContext(GetType()),
                iSettingRepository,
                iEventRelayBasic,
                externalCancellation,
                provenanceTracker
            );

            _writeCoordinator = sqliteWriteCoordinator;
            _rawPacketDatabaseReadiness = rawPacketDatabaseReadiness;
            _rawPacketIngestRepository = rawPacketIngestRepository;

			_bufferingEnabled = false;

            _instanceIdentifier = iInstanceIdentifier;
            var iid = _shouldRotateInstallationIdOnDbCreate
                ? _instanceIdentifier.CreateNewInstallationId()
                : _instanceIdentifier.GetOrCreateInstallationId();
            if (!Guid.TryParse(iid, out _installationIdGuid))
            {
                ILogger.Error($"Installation id '{iid}' is not a valid GUID. Aborting initialization.");
                return false;
            }

            var connected = await TryEstablishConnectionAsync().ConfigureAwait(false);
            if (!connected)
            {
                ILogger.Error("❌ SQLite initial connection failed during initialization. Aborting SQLite listener startup.");
                return false;
            }

            await StartAsync().ConfigureAwait(false);

            try { MarkReady(); } catch { }
            return true;
        }
        catch (Exception exception)
        {
            iLogger.Error($"❌ Error during SQLite listener initialization: {exception.Message}");
            iLogger.Warning("⚠️ Starting SQLite listener in degraded mode");
            await StartDegradedAsync().ConfigureAwait(false);
            return true;
        }

    }

    async Task<bool> TryEstablishConnectionAsync()
    {
        if (LinkedCancellationToken.IsCancellationRequested)
        {
            ILogger.Warning("⚠️ Connection attempt cancelled by external shutdown");
            return false;
        }

        if (Interlocked.CompareExchange(ref _isInitializing, 1, 0) != 0)
        {
            ILogger.Debug("⏳ Connection attempt already in progress, skipping");
            return false;
        }

        _lastConnectionAttempt = DateTime.UtcNow;

        try
        {
            using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedTestCts = CancellationTokenSource.CreateLinkedTokenSource(testCts.Token, LinkedCancellationToken);

            var rawPacketDatabaseReadiness = _rawPacketDatabaseReadiness;
            if (rawPacketDatabaseReadiness is null)
                throw new InvalidOperationException($"{nameof(IRawPacketDatabaseReadiness)} is not initialized.");

            var rawPacketIngestRepository = _rawPacketIngestRepository;
            if (rawPacketIngestRepository is null)
                throw new InvalidOperationException($"{nameof(IRawPacketIngestRepository)} is not initialized.");
            if (ShouldRunSchemaInitialization())
            {
                await rawPacketDatabaseReadiness.EnsureReadyAsync(linkedTestCts.Token).ConfigureAwait(false);
            }

            // Make sure JSON1 functions are available.
            await rawPacketIngestRepository.ProbeJson1Async(linkedTestCts.Token).ConfigureAwait(false);

            Interlocked.Exchange(ref _schemaInitialized, 1);

            _isDatabaseAvailable = true;
            _failureCount = 0;
            _lastSuccessfulWrite = DateTime.UtcNow;

            return true;
        }
        catch (OperationCanceledException operationCanceledException)
        {
            _isDatabaseAvailable = false;
            ILogger.Warning("⚠️ Connection attempt cancelled by external shutdown");
            return false;
        }
        catch (Exception exception)
        {
            _isDatabaseAvailable = false;
            _lastConnectionAttempt = DateTime.UtcNow;
            _failureCount++;
            ILogger.Warning($"⚠️ Failed to establish SQLite connection: {exception.Message}");
            return false;
        }
        finally
        {
            Interlocked.Decrement(ref _isInitializing);
        }
    }

    bool ShouldRunSchemaInitialization()
    {
        if (Volatile.Read(ref _schemaInitialized) != 0)
            return false;

        // If we know we created the DB this run, always run DDL once.
        if (_shouldRotateInstallationIdOnDbCreate)
            return true;

        // If we can resolve a db path, run DDL only if the file didn't exist at startup.
        if (!string.IsNullOrWhiteSpace(_resolvedDbPath))
            return !_dbFileExistedAtStartup;

		// If we can't resolve the db path, be conservative and allow one initialization attempt.
        return true;
    }

    async Task StartDegradedAsync()
    {
        ILogger.Warning("🔶 SQLite listener running in DEGRADED MODE");
        IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);
        StartHealthMonitoring();
        await Task.CompletedTask;
    }

    async Task<bool> StartAsync()
    {
        ILogger.Information("✅ SQLite Listener started in ACTIVE mode");
        IEventRelayBasic.Register<IRawPacketRecordTyped>(this, ReceiveHandler);
        StartHealthMonitoring();
        return await Task.FromResult(true);
    }

    void StartHealthMonitoring()
    {
        if (_healthCheckTimer is not null) return;

        _healthCheckTimer = new Timer(
            HealthCheckCallback,
            null,
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60)
        );

        ILogger.Information("🏥 Health monitoring started for SQLite listener");
    }

    void HealthCheckCallback(object? state)
    {
        try
        {
            if (LinkedCancellationToken.IsCancellationRequested) return;

            var status = _isDatabaseAvailable ? "ACTIVE" : "DEGRADED";
            var timeSinceLastWrite = DateTime.UtcNow - _lastSuccessfulWrite;
            if (_isDatabaseAvailable)
            {
                if (timeSinceLastWrite > TimeSpan.FromMinutes(5) && _lastSuccessfulWrite != DateTime.MinValue)
                {
					ILogger.Warning($"⚠️ SQLite [{status}] No writes in {timeSinceLastWrite.TotalMinutes:F1} minutes.");
                }
                else
                {
                    ILogger.Debug($"💚 SQLite [{status}] Healthy - Last write: {timeSinceLastWrite.TotalSeconds:F0}s ago. Failures: {_failureCount}");
                }
            }
            else
            {
                var timeSinceLastAttempt = DateTime.UtcNow - _lastConnectionAttempt;
				ILogger.Warning($"🔶 SQLite [{status}] Unavailable - Last attempt: {timeSinceLastAttempt.TotalSeconds:F0}s ago. Failures: {_failureCount}");
            }

            if (_failureCount >= MaxConsecutiveFailures && _isDatabaseAvailable)
            {
                ILogger.Error($"❌ SQLite has {_failureCount} consecutive failures. Marking as UNAVAILABLE.");
                _isDatabaseAvailable = false;
            }
        }
        catch (Exception exception)
        {
            ILogger.Error($"❌ Error in health check: {exception.Message}");
        }
    }

    void ReceiveHandler(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        StartBackground(_ => ProcessMessage(iRawPacketRecordTyped));
    }

    async Task ProcessMessage(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        if (!_isDatabaseAvailable)
            return;

        await WriteToDatabase(iRawPacketRecordTyped).ConfigureAwait(false);
    }

    async Task WriteToDatabase(IRawPacketRecordTyped iRawPacketRecordTyped)
    {
        var tableString = UdpPacketTableData.PacketTableDataMap[iRawPacketRecordTyped.PacketEnum].TableName;

        try
        {
            var coordinator = _writeCoordinator;
            if (coordinator is null)
                throw new InvalidOperationException($"{nameof(MetWorks.Common.Utility.SqliteWriteCoordinator)} is not initialized.");

            var rawPacketIngestRepository = _rawPacketIngestRepository;
            if (rawPacketIngestRepository is null)
                throw new InvalidOperationException($"{nameof(MetWorks.Persistence.Ingest.IRawPacketIngestRepository)} is not initialized.");

            int rowsAffected = 0;
            await coordinator.RunAsync(async token =>
            {
                await rawPacketIngestRepository.InsertAsync(
                    new MetWorks.Persistence.Ingest.RawPacketRecord(
                        Table: tableString,
                        Id: iRawPacketRecordTyped.Id.ToString(),
                        JsonDocumentOriginal: iRawPacketRecordTyped.RawPacketJson,
                        ApplicationReceivedUtcTimestampz: iRawPacketRecordTyped.ReceivedUtcUnixEpochSecondsAsLong,
                        InstallationId: _installationIdGuid == Guid.Empty ? null : _installationIdGuid.ToString()),
                    token).ConfigureAwait(false);

                rowsAffected = 1;
            }, LinkedCancellationToken).ConfigureAwait(false);

            _lastSuccessfulWrite = DateTime.UtcNow;
            _failureCount = 0;

            Interlocked.Increment(ref _totalWrites);
            MaybeLogStats();

            ProvenanceTracker?.LinkDatabaseRecord(iRawPacketRecordTyped.Id, iRawPacketRecordTyped.Id);

            _ = rowsAffected;
        }
		catch (OperationCanceledException) when (LinkedCancellationToken.IsCancellationRequested)
		{
			_failureCount++;
			_lastConnectionAttempt = DateTime.UtcNow;

			Interlocked.Increment(ref _totalWriteFailures);
		}
        catch (Exception exception)
        {
            _failureCount++;
            _lastConnectionAttempt = DateTime.UtcNow;

            Interlocked.Increment(ref _totalWriteFailures);

            ProvenanceTracker?.RecordError(
                iRawPacketRecordTyped.Id,
                "ListenerSink",
                "Database Write",
                exception);

            _isDatabaseAvailable = false;

        }
    }

    void MaybeLogStats()
    {
        var nowUtc = DateTime.UtcNow;
        if (nowUtc - _lastStatsLogUtc < StatsLogInterval) return;

        var total = Interlocked.Read(ref _totalWrites);
        var failures = Interlocked.Read(ref _totalWriteFailures);

        var writesSinceLast = total - _writesAtLastStatsLog;
        var minutes = Math.Max(StatsLogInterval.TotalMinutes, 1);
        var ratePerMin = writesSinceLast / minutes;

        _lastStatsLogUtc = nowUtc;
        _writesAtLastStatsLog = total;

        ILogger.Information($"📊 SQLite writes: total={total}, failures={failures}, rate_per_min={ratePerMin:F1}");
    }

    protected override async Task OnDisposeAsync()
    {
        try
        {
            try { _healthCheckTimer?.Dispose(); } catch { }
            _healthCheckTimer = null;

            try
            {
                IEventRelayBasic.Unregister<IRawPacketRecordTyped>(this);
            }
            catch { }

            try { ILogger.Information("🧹 SQLite listener disposed"); } catch { }
        }
        catch { }

        await Task.CompletedTask;
    }
}
