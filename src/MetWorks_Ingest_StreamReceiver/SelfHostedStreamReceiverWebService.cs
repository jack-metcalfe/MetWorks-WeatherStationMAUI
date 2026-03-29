
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

var connectionString = builder.Configuration.GetConnectionString("Weather")
    ?? builder.Configuration["WEATHER_SQL_CONNECTIONSTRING"]
    ?? throw new InvalidOperationException("Missing SQL connection string. Set ConnectionStrings:Weather or WEATHER_SQL_CONNECTIONSTRING.");

ValidateSqlConnectionString(connectionString);

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

app.MapGet("/", () => Results.Text("MetWorks stream receiver is running.", MediaTypeNames.Text.Plain));
app.MapHealthChecks("/health");

app.MapPost("/ingest/v1/stream", async (HttpRequest request, CancellationToken token) =>
{
    if (request.ContentLength is 0)
        return Results.BadRequest(new { error = "Empty request body." });

    var contentType = request.ContentType ?? string.Empty;
    if (!contentType.StartsWith("application/x-ndjson", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Expected Content-Type application/x-ndjson." });

    long? maxRowId = null;
    long lines = 0;
    long jsonErrors = 0;
    long duplicates = 0;
    long enqueued = 0;

    PipeReader input;
    Stream? decompressionStream = null;

    if (request.Headers.TryGetValue("Content-Encoding", out var encValues) && encValues.ToString().Contains("gzip", StringComparison.OrdinalIgnoreCase))
    {
        decompressionStream = new GZipStream(request.Body, CompressionMode.Decompress, leaveOpen: true);
        input = PipeReader.Create(decompressionStream);
    }
    else
    {
        input = request.BodyReader;
    }

    try
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(token);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Weather.IngestQueue
(
    RecordHash, RecordId, InstallationId,
    RecordTable, PayloadType, SourceRowId, AppReceivedUtcEpoch,
    RawJson, SourceIp
)
VALUES
(
    @RecordHash, @RecordId, @InstallationId,
    @RecordTable, @PayloadType, @SourceRowId, @AppReceivedUtcEpoch,
    @RawJson, @SourceIp
);";

        var pRecordHash = cmd.Parameters.Add("@RecordHash", System.Data.SqlDbType.Char, 64);
        var pRecordId = cmd.Parameters.Add("@RecordId", System.Data.SqlDbType.UniqueIdentifier);
        var pInstallationId = cmd.Parameters.Add("@InstallationId", System.Data.SqlDbType.UniqueIdentifier);
        var pRecordTable = cmd.Parameters.Add("@RecordTable", System.Data.SqlDbType.VarChar, 50);
        var pPayloadType = cmd.Parameters.Add("@PayloadType", System.Data.SqlDbType.VarChar, 50);
        var pSourceRowId = cmd.Parameters.Add("@SourceRowId", System.Data.SqlDbType.BigInt);
        var pAppReceived = cmd.Parameters.Add("@AppReceivedUtcEpoch", System.Data.SqlDbType.BigInt);
        var pRawJson = cmd.Parameters.Add("@RawJson", System.Data.SqlDbType.NVarChar, -1);
        var pSourceIp = cmd.Parameters.Add("@SourceIp", System.Data.SqlDbType.VarChar, 64);

        var sourceIp = request.HttpContext.Connection.RemoteIpAddress?.ToString();

        while (true)
        {
            token.ThrowIfCancellationRequested();

            var result = await input.ReadAsync(token);
            var buffer = result.Buffer;

            while (TryReadLine(ref buffer, out var lineBytes))
            {
                if (lineBytes.IsEmpty)
                    continue;

                var line = Encoding.UTF8.GetString(lineBytes);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                lines++;

                var recordHash = ComputeSha256Hex(lineBytes);

                Guid? recordId = null;
                Guid? installationId = null;
                string? recordTable = null;
                string? payloadType = null;
                long? sourceRowId = null;
                long? appReceivedEpoch = null;

                var isValidJson = true;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("rowid", out var rowidEl) && rowidEl.ValueKind == JsonValueKind.Number)
                    {
                        sourceRowId = rowidEl.GetInt64();
                        if (maxRowId is null || sourceRowId > maxRowId)
                            maxRowId = sourceRowId;
                    }

                    if (root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String && Guid.TryParse(idEl.GetString(), out var gid))
                        recordId = gid;

                    if (root.TryGetProperty("installationId", out var instEl) && instEl.ValueKind == JsonValueKind.String && Guid.TryParse(instEl.GetString(), out var iid))
                        installationId = iid;
                    else if (root.TryGetProperty("installation_id", out var instEl2) && instEl2.ValueKind == JsonValueKind.String && Guid.TryParse(instEl2.GetString(), out var iid2))
                        installationId = iid2;

                    if (root.TryGetProperty("table", out var tableEl) && tableEl.ValueKind == JsonValueKind.String)
                        recordTable = tableEl.GetString();

                    if (root.TryGetProperty("application_received_utc_timestampz", out var recvEl) && recvEl.ValueKind == JsonValueKind.Number)
                        appReceivedEpoch = recvEl.GetInt64();

                    if (root.TryGetProperty("payload", out var payloadEl) && payloadEl.ValueKind == JsonValueKind.Object)
                    {
                        if (payloadEl.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String)
                            payloadType = typeEl.GetString();
                    }
                }
                catch (JsonException)
                {
                    jsonErrors++;
                    isValidJson = false;
                }

                if (!isValidJson)
                    continue;

                pRecordHash.Value = recordHash;
                pRecordId.Value = (object?)recordId ?? DBNull.Value;
                pInstallationId.Value = (object?)installationId ?? DBNull.Value;
                pRecordTable.Value = (object?)recordTable ?? DBNull.Value;
                pPayloadType.Value = (object?)payloadType ?? DBNull.Value;
                pSourceRowId.Value = (object?)sourceRowId ?? DBNull.Value;
                pAppReceived.Value = (object?)appReceivedEpoch ?? DBNull.Value;
                pRawJson.Value = line;
                pSourceIp.Value = (object?)sourceIp ?? DBNull.Value;

                try
                {
                    await cmd.ExecuteNonQueryAsync(token);
                    enqueued++;
                }
                catch (SqlException ex) when (ex.Number is 2601 or 2627)
                {
                    duplicates++;
                }
            }

            input.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
                break;
        }
    }
    finally
    {
        await input.CompleteAsync();
        decompressionStream?.Dispose();
    }

    return Results.Ok(new
    {
        ackedUpToRowId = maxRowId ?? 0,
        receivedLines = lines,
        enqueued,
        duplicates,
        jsonErrors
    });

    static string ComputeSha256Hex(ReadOnlySequence<byte> data)
    {
        Span<byte> hash = stackalloc byte[32];
        if (data.IsSingleSegment)
        {
            SHA256.HashData(data.FirstSpan, hash);
        }
        else
        {
            using var sha = SHA256.Create();
            foreach (var seg in data)
                sha.TransformBlock(seg.ToArray(), 0, seg.Length, null, 0);
            sha.TransformFinalBlock([], 0, 0);
            sha.Hash!.CopyTo(hash);
        }

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var position = buffer.PositionOf((byte)'\n');
        if (position is null)
        {
            line = default;
            return false;
        }

        line = buffer.Slice(0, position.Value);
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

        if (!line.IsEmpty)
        {
            var last = line.Slice(line.Length - 1, 1);
            if (last.FirstSpan.Length == 1 && last.FirstSpan[0] == (byte)'\r')
                line = line.Slice(0, line.Length - 1);
        }

        return true;
    }
});

app.Run();
