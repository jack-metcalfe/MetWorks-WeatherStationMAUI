using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MetWorks.Ingest.SQLite.Shipping;

internal static class LoggerSQLiteStreamShipping
{
    internal readonly record struct RetentionOptions(
        TimeSpan RetainFor,
        TimeSpan PurgeInterval
    );

    internal static readonly RetentionOptions DefaultRetention = new(
        RetainFor: TimeSpan.FromDays(7),
        PurgeInterval: TimeSpan.FromHours(1)
    );

    public static async Task ShipOnceAsync(
        SqliteConnection conn,
        Guid installationId,
        string source,
        string table,
        int maxBatchRows,
        HttpClient httpClient,
        string endpointUrl,
        RetentionOptions retention,
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

        if (string.IsNullOrWhiteSpace(endpointUrl))
            throw new ArgumentException("Endpoint URL is required.", nameof(endpointUrl));

        if (maxBatchRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBatchRows));

        var stateStore = new ShipperStateStore(installationId.ToString());
        await stateStore.EnsureTableAsync(conn, token).ConfigureAwait(false);

        var state = await stateStore.TryGetAsync(conn, source, token).ConfigureAwait(false);
        var lastAcked = state?.LastAckedRowId ?? 0;

        await TryPurgeOldRowsAsync(conn, stateStore, source, table, state, retention, token).ConfigureAwait(false);

        var rows = await ReadBatchAsync(conn, table, lastAckedRowId: lastAcked, maxRows: maxBatchRows, token).ConfigureAwait(false);
        if (rows.Count == 0)
            return;

        var maxId = rows[^1].Id;

        var ackedUpTo = await UploadNdjsonAsync(httpClient, endpointUrl, source, table, installationId, rows, token).ConfigureAwait(false);
        if (ackedUpTo is null)
            return;

        await stateStore.UpsertShippingProgressAsync(
            conn,
            source,
            lastShippedRowId: maxId,
            lastAckedRowId: ackedUpTo.Value,
            token).ConfigureAwait(false);
    }

    static async Task<List<LoggerSQLiteLogRow>> ReadBatchAsync(
        SqliteConnection conn,
        string table,
        long lastAckedRowId,
        int maxRows,
        CancellationToken token)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
SELECT id, timestamp_utc, level, message, exception, properties, installation_id
FROM ""{table}""
WHERE id > $last_acked_id
ORDER BY id
LIMIT $limit;";

        cmd.Parameters.AddWithValue("$last_acked_id", lastAckedRowId);
        cmd.Parameters.AddWithValue("$limit", maxRows);

        var list = new List<LoggerSQLiteLogRow>(capacity: Math.Min(maxRows, 512));

        await using var reader = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false);
        while (await reader.ReadAsync(token).ConfigureAwait(false))
        {
            var id = reader.GetInt64(0);
            var ts = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var level = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var message = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            var ex = reader.IsDBNull(4) ? null : reader.GetString(4);
            var props = reader.IsDBNull(5) ? null : reader.GetString(5);
            var installationId = reader.IsDBNull(6) ? null : reader.GetString(6);

            list.Add(new LoggerSQLiteLogRow(
                Id: id,
                TimestampUtc: ts,
                Level: level,
                Message: message,
                Exception: ex,
                PropertiesJson: props,
                InstallationId: installationId));
        }

        return list;
    }

    static async Task<long?> UploadNdjsonAsync(
        HttpClient httpClient,
        string endpointUrl,
        string source,
        string table,
        Guid installationId,
        List<LoggerSQLiteLogRow> rows,
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
                    source,
                    table,
                    installationId = installationId.ToString(),
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
        SqliteConnection conn,
        ShipperStateStore stateStore,
        string source,
        string table,
        ShipperStateSnapshot? state,
        RetentionOptions retention,
        CancellationToken token)
    {
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

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
DELETE FROM ""{table}""
WHERE id <= $acked_id AND timestamp_utc < $cutoff_ts;";

        cmd.Parameters.AddWithValue("$acked_id", acked);
        cmd.Parameters.AddWithValue("$cutoff_ts", cutoff.ToString("O"));

        var deleted = await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        if (deleted <= 0)
            return;

        await stateStore.RecordLossyDeletionAsync(
            conn,
            source,
            deletedThroughRowId: acked,
            deletedRowCount: deleted,
            deletionUtc: now,
            cancellationToken: token).ConfigureAwait(false);
    }
}
