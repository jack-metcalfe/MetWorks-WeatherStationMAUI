using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MetWorks.Ingest.SQLite.Shipping;

internal static class StandardReadingsStreamShipping
{
    public static async Task ShipOnceAsync(
        SqliteConnection conn,
        Guid installationId,
        string source,
        string table,
        string ddlScript,
        int maxBatchRows,
        HttpClient httpClient,
        string endpointUrl,
        CancellationToken token
    )
    {
        ArgumentNullException.ThrowIfNull(conn);
        ArgumentNullException.ThrowIfNull(httpClient);
        if (installationId == Guid.Empty)
            throw new ArgumentException("Installation id is required.", nameof(installationId));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source is required.", nameof(source));
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table is required.", nameof(table));
        if (string.IsNullOrWhiteSpace(ddlScript))
            throw new ArgumentException("DDL script is required.", nameof(ddlScript));
        if (string.IsNullOrWhiteSpace(endpointUrl))
            throw new ArgumentException("Endpoint URL is required.", nameof(endpointUrl));
        if (maxBatchRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBatchRows));

        await EnsureSchemaAsync(conn, ddlScript, token).ConfigureAwait(false);

        var stateStore = new ShipperStateStore(installationId.ToString());
        var state = await stateStore.TryGetAsync(conn, source, token).ConfigureAwait(false);
        var lastAcked = state?.LastAckedRowId ?? 0;

        var rows = await ReadBatchAsync(conn, table, installationId: installationId.ToString(), lastAckedRowId: lastAcked, maxRows: maxBatchRows, token).ConfigureAwait(false);
        if (rows.Count == 0)
            return;

        var maxRowId = rows[^1].RowId;

        var ackedUpTo = await UploadNdjsonAsync(httpClient, endpointUrl, table, installationId, rows, token).ConfigureAwait(false);
        if (ackedUpTo is null)
            return;

        await stateStore.UpsertShippingProgressAsync(
            conn,
            source,
            lastShippedRowId: maxRowId,
            lastAckedRowId: ackedUpTo.Value,
            token).ConfigureAwait(false);
    }

    static async Task EnsureSchemaAsync(SqliteConnection conn, string ddlScript, CancellationToken token)
    {
        foreach (var script in new[]
        {
            "Ingest/SQLite/shipper_state.sql",
            ddlScript
        })
        {
            var ddl = IResourceProvider.GetString(script);
            if (string.IsNullOrWhiteSpace(ddl))
                continue;

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = ddl;
            cmd.CommandTimeout = 60;
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }
    }

    static async Task<List<StandardReadingRow>> ReadBatchAsync(SqliteConnection conn, string table, string installationId, long lastAckedRowId, int maxRows, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table is required.", nameof(table));
        if (string.IsNullOrWhiteSpace(installationId))
            throw new ArgumentException("Installation id is required.", nameof(installationId));

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
SELECT rowid, id, application_received_utc_timestampz, json_document_original
FROM {table}
WHERE installation_id = $installation_id AND rowid > $last_acked_rowid
ORDER BY rowid
LIMIT $limit;";

        cmd.Parameters.AddWithValue("$installation_id", installationId);
        cmd.Parameters.AddWithValue("$last_acked_rowid", lastAckedRowId);
        cmd.Parameters.AddWithValue("$limit", maxRows);

        var list = new List<StandardReadingRow>(capacity: Math.Min(maxRows, 512));

        await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
        while (await reader.ReadAsync(token).ConfigureAwait(false))
        {
            var rowId = reader.GetInt64(0);
            var id = reader.GetString(1);
            var appTs = reader.GetInt64(2);
            var json = reader.GetString(3);
            list.Add(new StandardReadingRow(rowId, id, appTs, json));
        }

        return list;
    }

    static async Task<long?> UploadNdjsonAsync(
        HttpClient httpClient,
        string endpointUrl,
        string table,
        Guid installationId,
        List<StandardReadingRow> rows,
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
                    installationId = installationId.ToString(),
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
}
