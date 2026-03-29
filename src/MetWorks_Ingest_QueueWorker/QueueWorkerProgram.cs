var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Weather")
    ?? builder.Configuration["WEATHER_SQL_CONNECTIONSTRING"]
    ?? throw new InvalidOperationException("Missing SQL connection string. Set ConnectionStrings:Weather or WEATHER_SQL_CONNECTIONSTRING.");

ValidateSqlConnectionString(connectionString);

builder.Services.AddHostedService(sp => new QueueWorker(
    sp.GetRequiredService<ILogger<QueueWorker>>(),
    connectionString,
    workerId: Environment.MachineName));

await builder.Build().RunAsync();

static void ValidateSqlConnectionString(string cs)
{
    try
    {
        _ = new SqlConnectionStringBuilder(cs);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine($"Invalid SQL connection string: {ex.Message}");
        Console.Error.WriteLine("Use standard keywords like 'User ID=' (NOT 'userid='). Example:");
        Console.Error.WriteLine("  export WEATHER_SQL_CONNECTIONSTRING='Server=localhost;Database=weather;User ID=weather;Password=***;Encrypt=True;TrustServerCertificate=True;'");
        Environment.Exit(2);
    }
}

sealed class QueueWorker(ILogger<QueueWorker> logger, string connectionString, string workerId) : BackgroundService
{
    private readonly ILogger<QueueWorker> _logger = logger;
    private readonly string _connectionString = connectionString;
    private readonly string _workerId = workerId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queue worker starting as {WorkerId}", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = await DequeueAsync(batchSize: 200, lockSeconds: 60, stoppingToken);

                if (batch.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                foreach (var item in batch)
                {
                    try
                    {
                        await InsertRawIngestAsync(item, stoppingToken);
                        await MarkDoneAsync(item.QueueId, stoppingToken);
                    }
                    catch (SqlException ex) when (ex.Number is 2601 or 2627)
                    {
                        // Duplicate in RawIngest; safe to treat as done.
                        await MarkDoneAsync(item.QueueId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        var delaySeconds = ComputeBackoffSeconds(item.Attempts + 1);
                        await MarkFailedAsync(item.QueueId, item.Attempts, ex, delaySeconds, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker loop error");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Queue worker stopping");
    }

    private async Task<List<QueueItem>> DequeueAsync(int batchSize, int lockSeconds, CancellationToken token)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(token);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "Weather.DequeueIngestBatch";
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@BatchSize", batchSize);
        cmd.Parameters.AddWithValue("@LockSeconds", lockSeconds);
        cmd.Parameters.AddWithValue("@WorkerId", _workerId);

        var items = new List<QueueItem>(batchSize);
        await using var rdr = await cmd.ExecuteReaderAsync(token);
        while (await rdr.ReadAsync(token))
        {
            items.Add(new QueueItem(
                QueueId: rdr.GetInt64(0),
                RecordHash: rdr.GetString(1),
                RecordId: rdr.IsDBNull(2) ? null : rdr.GetGuid(2),
                InstallationId: rdr.IsDBNull(3) ? null : rdr.GetGuid(3),
                RecordTable: rdr.IsDBNull(4) ? null : rdr.GetString(4),
                PayloadType: rdr.IsDBNull(5) ? null : rdr.GetString(5),
                SourceRowId: rdr.IsDBNull(6) ? null : rdr.GetInt64(6),
                AppReceivedUtcEpoch: rdr.IsDBNull(7) ? null : rdr.GetInt64(7),
                Attempts: rdr.GetInt32(8),
                RawJson: rdr.GetString(9)));
        }

        if (items.Count > 0)
            _logger.LogInformation("Dequeued {Count}", items.Count);

        return items;
    }

    private async Task InsertRawIngestAsync(QueueItem item, CancellationToken token)
    {
        using var doc = JsonDocument.Parse(item.RawJson);
        var root = doc.RootElement;

        var recordTable = item.RecordTable;
        if (recordTable is null && root.TryGetProperty("table", out var tableEl) && tableEl.ValueKind == JsonValueKind.String)
            recordTable = tableEl.GetString();

        Guid? recordId = item.RecordId;
        if (recordId is null && root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String && Guid.TryParse(idEl.GetString(), out var gid))
            recordId = gid;

        Guid? installationId = item.InstallationId;
        if (installationId is null)
        {
            if (root.TryGetProperty("installationId", out var instEl) && instEl.ValueKind == JsonValueKind.String && Guid.TryParse(instEl.GetString(), out var iid))
                installationId = iid;
            else if (root.TryGetProperty("installation_id", out var instEl2) && instEl2.ValueKind == JsonValueKind.String && Guid.TryParse(instEl2.GetString(), out var iid2))
                installationId = iid2;
        }

        long? sourceRowId = item.SourceRowId;
        if (sourceRowId is null && root.TryGetProperty("rowid", out var rowidEl) && rowidEl.ValueKind == JsonValueKind.Number)
            sourceRowId = rowidEl.GetInt64();

        long? appReceivedEpoch = item.AppReceivedUtcEpoch;
        if (appReceivedEpoch is null && root.TryGetProperty("application_received_utc_timestampz", out var recvEl) && recvEl.ValueKind == JsonValueKind.Number)
            appReceivedEpoch = recvEl.GetInt64();

        DateTime? timestampUtc = null;
        string? level = null;
        string? message = null;
        string? exception = null;
        string? propertiesJson = null;

        if (root.TryGetProperty("timestamp_utc", out var tsEl) && tsEl.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(tsEl.GetString(), out var dto))
            timestampUtc = dto.UtcDateTime;

        if (root.TryGetProperty("level", out var lvlEl) && lvlEl.ValueKind == JsonValueKind.String)
            level = lvlEl.GetString();
        if (root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
            message = msgEl.GetString();
        if (root.TryGetProperty("exception", out var exEl) && exEl.ValueKind == JsonValueKind.String)
            exception = exEl.GetString();
        if (root.TryGetProperty("properties_json", out var pjEl) && pjEl.ValueKind == JsonValueKind.String)
            propertiesJson = pjEl.GetString();

        string? payloadType = item.PayloadType;
        string? stationSn = null;
        string? hubSn = null;
        string? payloadJson = null;

        if (root.TryGetProperty("payload", out var payloadEl) && payloadEl.ValueKind == JsonValueKind.Object)
        {
            if (payloadType is null && payloadEl.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String)
                payloadType = typeEl.GetString();
            if (payloadEl.TryGetProperty("serial_number", out var snEl) && snEl.ValueKind == JsonValueKind.String)
                stationSn = snEl.GetString();
            if (payloadEl.TryGetProperty("hub_sn", out var hubEl) && hubEl.ValueKind == JsonValueKind.String)
                hubSn = hubEl.GetString();
            payloadJson = payloadEl.GetRawText();
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(token);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Weather.RawIngest
(
    RecordHash, RecordTable,
    InstallationId, RecordId, SourceRowId,
    TimestampUtc, Level, Message, Exception, PropertiesJson,
    AppReceivedUtcEpoch, PayloadType, StationSerialNumber, HubSerialNumber, Payload,
    SourceFile, SourceLine
)
VALUES
(
    @RecordHash, @RecordTable,
    @InstallationId, @RecordId, @SourceRowId,
    @TimestampUtc, @Level, @Message, @Exception, @PropertiesJson,
    @AppReceivedUtcEpoch, @PayloadType, @StationSerialNumber, @HubSerialNumber, @Payload,
    NULL, NULL
);";

        cmd.Parameters.AddWithValue("@RecordHash", item.RecordHash);
        cmd.Parameters.AddWithValue("@RecordTable", (object?)recordTable ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@InstallationId", (object?)installationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RecordId", (object?)recordId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SourceRowId", (object?)sourceRowId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TimestampUtc", (object?)timestampUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Level", (object?)level ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Message", (object?)message ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Exception", (object?)exception ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PropertiesJson", (object?)propertiesJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@AppReceivedUtcEpoch", (object?)appReceivedEpoch ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PayloadType", (object?)payloadType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StationSerialNumber", (object?)stationSn ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@HubSerialNumber", (object?)hubSn ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Payload", (object?)payloadJson ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(token);
    }

    private async Task MarkDoneAsync(long queueId, CancellationToken token)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(token);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "Weather.MarkIngestDone";
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@QueueId", queueId);
        cmd.Parameters.AddWithValue("@WorkerId", _workerId);
        await cmd.ExecuteNonQueryAsync(token);
    }

    private async Task MarkFailedAsync(long queueId, int attempts, Exception ex, int delaySeconds, CancellationToken token)
    {
        _logger.LogError(ex, "Failed QueueId={QueueId}", queueId);
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(token);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "Weather.MarkIngestFailed";
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@QueueId", queueId);
        cmd.Parameters.AddWithValue("@WorkerId", _workerId);
        cmd.Parameters.AddWithValue("@Error", ex.ToString());
        cmd.Parameters.AddWithValue("@DelaySeconds", delaySeconds);
        cmd.Parameters.AddWithValue("@MaxAttempts", 10);
        await cmd.ExecuteNonQueryAsync(token);
    }

    private static int ComputeBackoffSeconds(int attempts)
    {
        // 2, 4, 8, ... seconds up to 300s
        var v = (int)Math.Pow(2, Math.Clamp(attempts, 1, 16));
        return Math.Min(v, 300);
    }
}

readonly record struct QueueItem(
    long QueueId,
    string RecordHash,
    Guid? RecordId,
    Guid? InstallationId,
    string? RecordTable,
    string? PayloadType,
    long? SourceRowId,
    long? AppReceivedUtcEpoch,
    int Attempts,
    string RawJson);
