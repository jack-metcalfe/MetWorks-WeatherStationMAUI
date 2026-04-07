using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MetWorks.Persistence.StreamShipping;

namespace MetWorks.Ingest.SQLite.Shipping;
public sealed class ObservationStreamShipper : ServiceBase
{
    const string Table = "observation";

    const int DefaultShipIntervalSeconds = 30;
    const int DefaultMaxBatchRows = 500;

    string _installationId = string.Empty;

    string _endpointUrl = string.Empty;
    int _shipIntervalSeconds = DefaultShipIntervalSeconds;
    int _maxBatchRows = DefaultMaxBatchRows;

    HttpClient? _httpClient;

    IStreamShippingDatabaseReadiness? _streamShippingDatabaseReadiness;
    IStreamShippingRepository? _streamShippingRepository;

    public ObservationStreamShipper()
    {
    }

    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        IStreamShippingDatabaseReadiness streamShippingDatabaseReadiness,
        IStreamShippingRepository streamShippingRepository,
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

        var enabled = iSettingRepository.GetValueOrDefault<bool>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_enabled));

        if (!enabled)
        {
            ILogger.Information("ObservationStreamShipper is disabled via settings");
            try { MarkReady(); } catch { }
            return true;
        }

        _endpointUrl = iSettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_endpointUrl));

        _shipIntervalSeconds = iSettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_shipIntervalSeconds));

        if (_shipIntervalSeconds <= 0)
            _shipIntervalSeconds = DefaultShipIntervalSeconds;

        _maxBatchRows = iSettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.StreamShippingGroupSettingsDefinition.BuildPath(SettingConstants.StreamShipping_maxBatchRows));

        if (_maxBatchRows <= 0)
            _maxBatchRows = DefaultMaxBatchRows;

        _installationId = iInstanceIdentifier.GetOrCreateInstallationId();

        if (string.IsNullOrWhiteSpace(_endpointUrl))
        {
            ILogger.Warning("ObservationStreamShipper endpointUrl is not configured; shipper will not run.");
            try { MarkReady(); } catch { }
            return true;
        }

        if (string.IsNullOrWhiteSpace(_installationId))
        {
            ILogger.Warning("ObservationStreamShipper has no installation id; shipper will not run.");
            try { MarkReady(); } catch { }
            return true;
        }

        StartBackground(ct => ShipLoopAsync(TimeSpan.FromSeconds(_shipIntervalSeconds), ct));

        try { MarkReady(); } catch { }
        ILogger.Information($"ObservationStreamShipper started (interval={_shipIntervalSeconds}s, maxBatchRows={_maxBatchRows})");
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
            catch (OperationCanceledException ex) when (!token.IsCancellationRequested)
            {
                ILogger.Error("ObservationStreamShipper: request timed out", ex);
            }
            catch (HttpRequestException ex)
            {
                ILogger.Error("ObservationStreamShipper: HTTP failure", ex);
            }
            catch (InvalidOperationException ex)
            {
                ILogger.Error("ObservationStreamShipper: failure", ex);
            }
            catch (Exception ex)
            {
                ILogger.Error("ObservationStreamShipper: unexpected failure", ex);
            }
        }
    }

    async Task ShipOnceAsync(CancellationToken token)
    {
        try
        {
            if (_httpClient is null)
                throw new InvalidOperationException("HttpClient is not initialized.");

            var readiness = _streamShippingDatabaseReadiness;
            if (readiness is null)
                throw new InvalidOperationException("Stream shipping database readiness is not initialized.");

            var repo = _streamShippingRepository;
            if (repo is null)
                throw new InvalidOperationException("Stream shipping repository is not initialized.");

            if (string.IsNullOrWhiteSpace(_installationId))
                throw new InvalidOperationException("Installation id is not initialized.");

            await readiness.EnsureReadyAsync(token).ConfigureAwait(false);

            var state = await repo.TryGetStateAsync(Table, token).ConfigureAwait(false);
            var lastAcked = state?.LastAckedRowId ?? 0;

            var rows = await repo.ReadStandardReadingsBatchAsync(
                table: Table,
                installationId: _installationId,
                lastAckedRowId: lastAcked,
                maxRows: _maxBatchRows,
                cancellationToken: token).ConfigureAwait(false);

            if (rows.Count == 0)
                return;

            var maxRowId = rows[^1].RowId;

            var ackedUpTo = await UploadNdjsonAsync(
                httpClient: _httpClient,
                endpointUrl: _endpointUrl,
                table: Table,
                installationId: _installationId,
                rows: rows,
                iLogger: ILogger,
                token: token).ConfigureAwait(false);

            if (ackedUpTo is null)
                return;

            await repo.UpsertShippingProgressAsync(
                table: Table,
                lastShippedRowId: maxRowId,
                lastAckedRowId: ackedUpTo.Value,
                cancellationToken: token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex) when (!token.IsCancellationRequested)
        {
            ILogger.Error("ObservationStreamShipper: request timed out during shipping", ex);
        }
        catch (HttpRequestException ex)
        {
            ILogger.Error("ObservationStreamShipper: HTTP failure during shipping", ex);
        }
        catch (InvalidOperationException ex)
        {
            ILogger.Error("ObservationStreamShipper: failure during shipping", ex);
        }
        catch (Exception ex)
        {
            ILogger.Error("ObservationStreamShipper: unexpected error during shipping", ex);
        }
    }

    internal static async Task<long?> UploadNdjsonAsync(
        HttpClient httpClient,
        string endpointUrl,
        string table,
        string installationId,
        IReadOnlyList<StandardReadingRow> rows,
        ILogger iLogger,
        CancellationToken token)
    {
        await using var payloadStream = new MemoryStream();
        await using (var gzip = new GZipStream(payloadStream, CompressionLevel.SmallestSize, leaveOpen: true))
        await using (var writer = new StreamWriter(gzip, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 16 * 1024, leaveOpen: true))
        {
            foreach (var row in rows)
            {
                using var payloadDoc = JsonDocument.Parse(row.JsonDocumentOriginal);
                var obj = new
                {
                    table,
                    installationId,
                    rowid = row.RowId,
                    id = row.Id,
                    application_received_utc_timestampz = row.ApplicationReceivedUtcEpoch,
                    payload = payloadDoc.RootElement
                };

                var line = JsonSerializer.Serialize(obj);
                await writer.WriteLineAsync(line.AsMemory(), token).ConfigureAwait(false);
            }

            await writer.FlushAsync(token).ConfigureAwait(false);
        }

        payloadStream.Position = 0;

        var gzipBytes = payloadStream.Length;
        var rowCount = rows.Count;
        var startTicks = Stopwatch.GetTimestamp();
        var success = false;

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl)
        {
            Content = new StreamContent(payloadStream)
        };

        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-ndjson");
        request.Content.Headers.ContentEncoding.Add("gzip");

        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                iLogger.Error($"UploadNdjsonAsync: server returned {(int)response.StatusCode} {response.ReasonPhrase} (endpoint={endpointUrl}, table={table}, rows={rowCount}, gzipBytes={gzipBytes}): {errorBody}");
                return null;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: token).ConfigureAwait(false);

            if (doc.RootElement.TryGetProperty("ackedUpToRowId", out var ackedEl) && ackedEl.ValueKind == JsonValueKind.Number)
            {
                success = true;
                return ackedEl.GetInt64();
            }

            if (doc.RootElement.TryGetProperty("acked_up_to_rowid", out var ackedSnake) && ackedSnake.ValueKind == JsonValueKind.Number)
            {
                success = true;
                return ackedSnake.GetInt64();
            }

            iLogger.Error($"UploadNdjsonAsync: server returned unrecognized ACK response (endpoint={endpointUrl}, table={table}, rows={rowCount}, status={(int)response.StatusCode}): {doc.RootElement.GetRawText()}");
            return null;
        }
        catch (OperationCanceledException ex) when (!token.IsCancellationRequested)
        {
            iLogger.Error($"ObservationStreamShipper: request timed out (endpoint={endpointUrl}, rows={rowCount}, gzipBytes={gzipBytes})", ex);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            iLogger.Error($"ObservationStreamShipper: upload failed (endpoint={endpointUrl}, rows={rowCount}, gzipBytes={gzipBytes})", ex);
            throw;
        }
        finally
        {
            var elapsedTicks = Stopwatch.GetTimestamp() - startTicks;
            StreamShippingUploadMetrics.Record(
                table: table,
                rows: rowCount,
                gzipBytes: gzipBytes,
                elapsedTicks: elapsedTicks,
                success: success);
        }
    }
}
