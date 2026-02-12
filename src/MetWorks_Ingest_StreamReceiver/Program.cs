using System.Buffers;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

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
        while (true)
        {
            token.ThrowIfCancellationRequested();

            var result = await input.ReadAsync(token);
            var buffer = result.Buffer;

            while (TryReadLine(ref buffer, out var lineBytes))
            {
                var line = Encoding.UTF8.GetString(lineBytes);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                lines++;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("rowid", out var rowidEl) && rowidEl.ValueKind == JsonValueKind.Number)
                    {
                        var rowid = rowidEl.GetInt64();
                        if (maxRowId is null || rowid > maxRowId)
                            maxRowId = rowid;
                    }
                }
                catch (JsonException)
                {
                    jsonErrors++;
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
        jsonErrors
    });

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
