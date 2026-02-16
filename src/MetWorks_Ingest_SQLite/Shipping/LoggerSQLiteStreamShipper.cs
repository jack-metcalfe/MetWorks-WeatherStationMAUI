using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MetWorks.Persistence.StreamShipping;

namespace MetWorks.Ingest.SQLite.Shipping;

public sealed class LoggerSQLiteStreamShipper : ServiceBase
{
    const string Source = "logger_sqlite";
    const string DefaultLoggerTableName = DatabaseConstants.DefaultLoggerSqliteTableName;

    const int DefaultShipIntervalSeconds = 30;
    const int DefaultMaxBatchRows = 500;

    const int MinShipIntervalSeconds = 1;
    const int MaxShipIntervalSeconds = 24 * 60 * 60;

    string _tableName = DefaultLoggerTableName;
    string _installationId = string.Empty;

    string _endpointUrl = string.Empty;
    int _shipIntervalSeconds = DefaultShipIntervalSeconds;
    int _maxBatchRows = DefaultMaxBatchRows;

    HttpClient? _httpClient;

    IStreamShippingDatabaseReadiness? _streamShippingDatabaseReadiness;
    IStreamShippingRepository? _streamShippingRepository;
    ILoggerStreamShippingRepository? _loggerStreamShippingRepository;

    public LoggerSQLiteStreamShipper()
    {
    }

    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        IStreamShippingDatabaseReadiness streamShippingDatabaseReadiness,
        IStreamShippingRepository streamShippingRepository,
        ILoggerStreamShippingRepository loggerStreamShippingRepository,
        HttpClient httpClient,
        CancellationToken externalCancellation,
        ProvenanceTracker provenanceTracker
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);
        ArgumentNullException.ThrowIfNull(streamShippingDatabaseReadiness);
        ArgumentNullException.ThrowIfNull(streamShippingRepository);
        ArgumentNullException.ThrowIfNull(loggerStreamShippingRepository);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(provenanceTracker);

        InitializeBase(
            iLogger.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker
        );

        _httpClient = httpClient;

        _streamShippingDatabaseReadiness = streamShippingDatabaseReadiness;
        _streamShippingRepository = streamShippingRepository;
        _loggerStreamShippingRepository = loggerStreamShippingRepository;

        var enabled = iSettingRepository.GetValueOrDefault<bool>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_enabled));

        if (!enabled)
        {
            ILogger.Information("LoggerSQLiteStreamShipper is disabled via settings");
            try { MarkReady(); } catch { }
            return true;
        }

        _endpointUrl = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_endpointUrl));

        _shipIntervalSeconds = iSettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_shipIntervalSeconds));

        if (_shipIntervalSeconds <= 0)
            _shipIntervalSeconds = DefaultShipIntervalSeconds;

        if (_shipIntervalSeconds < MinShipIntervalSeconds)
            _shipIntervalSeconds = MinShipIntervalSeconds;
        else if (_shipIntervalSeconds > MaxShipIntervalSeconds)
            _shipIntervalSeconds = MaxShipIntervalSeconds;

        _maxBatchRows = iSettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_maxBatchRows));

        if (_maxBatchRows <= 0)
            _maxBatchRows = DefaultMaxBatchRows;

        _tableName = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.LoggerSQLiteGroupSettingsDefinition.BuildPath(SettingConstants.LoggerSQLite_tableName));

        if (string.IsNullOrWhiteSpace(_tableName))
            _tableName = DefaultLoggerTableName;

        _installationId = iInstanceIdentifier.GetOrCreateInstallationId();

        if (string.IsNullOrWhiteSpace(_endpointUrl))
        {
            ILogger.Warning("LoggerSQLiteStreamShipper endpointUrl is not configured; shipper will not run.");
            try { MarkReady(); } catch { }
            return true;
        }

        if (string.IsNullOrWhiteSpace(_installationId))
        {
            ILogger.Warning("LoggerSQLiteStreamShipper has no installation id; shipper will not run.");
            try { MarkReady(); } catch { }
            return true;
        }

        StartBackground(ct => ShipLoopAsync(TimeSpan.FromSeconds(_shipIntervalSeconds), ct));

        try { MarkReady(); } catch { }
        ILogger.Information($"LoggerSQLiteStreamShipper started (interval={_shipIntervalSeconds}s, maxBatchRows={_maxBatchRows}, table={_tableName})");
        return true;
    }

    async Task ShipLoopAsync(TimeSpan interval, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, token).ConfigureAwait(false);
                await ShipOnceAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex)
            {
                ILogger.Warning($"LoggerSQLiteStreamShipper: HTTP failure: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                ILogger.Warning($"LoggerSQLiteStreamShipper: failure: {ex.Message}");
            }
        }
    }

    async Task ShipOnceAsync(CancellationToken token)
    {
        if (_httpClient is null)
            throw new InvalidOperationException("HttpClient is not initialized.");

        var readiness = _streamShippingDatabaseReadiness;
        if (readiness is null)
            throw new InvalidOperationException("Stream shipping database readiness is not initialized.");

        var stateRepo = _streamShippingRepository;
        if (stateRepo is null)
            throw new InvalidOperationException("Stream shipping repository is not initialized.");

        var loggerRepo = _loggerStreamShippingRepository;
        if (loggerRepo is null)
            throw new InvalidOperationException("Logger stream shipping repository is not initialized.");

        if (string.IsNullOrWhiteSpace(_installationId))
            throw new InvalidOperationException("Installation id is not initialized.");

        await readiness.EnsureReadyAsync(token).ConfigureAwait(false);

        var state = await stateRepo.TryGetStateAsync(Source, token).ConfigureAwait(false);

        await TryPurgeOldRowsAsync(
            loggerRepo,
            stateRepo,
            state,
            _tableName,
            token).ConfigureAwait(false);

        var lastAcked = state?.LastAckedRowId ?? 0;
        var rows = await loggerRepo.ReadLoggerBatchAsync(_tableName, lastAcked, _maxBatchRows, token).ConfigureAwait(false);
        if (rows.Count == 0)
            return;

        var maxId = rows[^1].Id;

        var ackedUpTo = await UploadNdjsonAsync(
            httpClient: _httpClient,
            endpointUrl: _endpointUrl,
            table: _tableName,
            installationId: _installationId,
            rows: rows,
            token: token).ConfigureAwait(false);

        if (ackedUpTo is null)
            return;

        await stateRepo.UpsertShippingProgressAsync(
            source: Source,
            lastShippedRowId: maxId,
            lastAckedRowId: ackedUpTo.Value,
            cancellationToken: token).ConfigureAwait(false);
    }

    static async Task<long?> UploadNdjsonAsync(
        HttpClient httpClient,
        string endpointUrl,
        string table,
        string installationId,
        IReadOnlyList<LoggerLogRow> rows,
        CancellationToken token)
    {
        await using var payloadStream = new MemoryStream();
        await using (var gzip = new GZipStream(payloadStream, CompressionLevel.SmallestSize, leaveOpen: true))
        await using (var writer = new StreamWriter(gzip, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 16 * 1024, leaveOpen: true))
        {
            foreach (var row in rows)
            {
                var obj = new
                {
                    source = Source,
                    table,
                    installationId,
                    rowid = row.Id,
                    timestamp_utc = row.TimestampUtc,
                    level = row.Level,
                    message = row.Message,
                    exception = row.Exception,
                    properties_json = row.PropertiesJson,
                    installation_id = row.InstallationId
                };

                var line = JsonSerializer.Serialize(obj);
                await writer.WriteLineAsync(line.AsMemory(), token).ConfigureAwait(false);
            }

            await writer.FlushAsync(token).ConfigureAwait(false);
        }

        payloadStream.Position = 0;

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl)
        {
            Content = new StreamContent(payloadStream)
        };

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-ndjson");
        request.Content.Headers.ContentEncoding.Add("gzip");

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: token).ConfigureAwait(false);

        if (doc.RootElement.TryGetProperty("ackedUpToRowId", out var ackedEl) && ackedEl.ValueKind == JsonValueKind.Number)
            return ackedEl.GetInt64();

        if (doc.RootElement.TryGetProperty("acked_up_to_rowid", out var ackedSnake) && ackedSnake.ValueKind == JsonValueKind.Number)
            return ackedSnake.GetInt64();

        return null;
    }

    static async Task TryPurgeOldRowsAsync(
        ILoggerStreamShippingRepository loggerRepo,
        IStreamShippingRepository stateRepo,
        ShipperStateSnapshot? state,
        string tableName,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table is required.", nameof(tableName));

        var retention = new LoggerRetentionOptions(
            RetainFor: TimeSpan.FromDays(7),
            PurgeInterval: TimeSpan.FromHours(1));

        if (retention.RetainFor <= TimeSpan.Zero || retention.PurgeInterval <= TimeSpan.Zero)
            return;

        var now = DateTime.UtcNow;
        if (state?.LastLossyDeleteUtc is not null)
        {
            var elapsed = now - state.LastLossyDeleteUtc.Value;
            if (elapsed < retention.PurgeInterval)
                return;
        }

        var acked = state?.LastAckedRowId ?? 0;
        if (acked <= 0)
            return;

        var cutoff = now - retention.RetainFor;

        var deleted = await loggerRepo.PurgeAckedOlderThanAsync(
            table: tableName,
            ackedUpToRowId: acked,
            cutoffUtc: cutoff,
            cancellationToken: token).ConfigureAwait(false);

        if (deleted <= 0)
            return;

        await loggerRepo.RecordLossyDeletionAsync(
            source: Source,
            deletedThroughRowId: acked,
            deletedRowCount: deleted,
            deletionUtc: now,
            cancellationToken: token).ConfigureAwait(false);

        await stateRepo.UpsertShippingProgressAsync(
            Source,
            lastShippedRowId: acked,
            lastAckedRowId: acked,
            cancellationToken: token).ConfigureAwait(false);
    }
}
