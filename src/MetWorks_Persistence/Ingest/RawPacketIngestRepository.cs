using System.Text.RegularExpressions;
using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Ingest;

public sealed class RawPacketIngestRepository : IRawPacketIngestRepository
{
    static readonly Regex ValidIdentifier = new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    ISqliteDatabase? _sqliteDatabase;

    public RawPacketIngestRepository()
    {
    }

    public Task<bool> InitializeAsync(ISqliteDatabase sqliteDatabase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);

        _sqliteDatabase = sqliteDatabase;
        return Task.FromResult(true);
    }

    ISqliteDatabase GetInitializedSqliteDatabase() =>
        _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(RawPacketIngestRepository)} has not been initialized.");

    public async Task ProbeJsonAsync(CancellationToken cancellationToken)
    {
        await using var session = await GetInitializedSqliteDatabase().OpenSessionAsync(cancellationToken).ConfigureAwait(false);

        _ = await session.ScalarAsync<long>(
            "SELECT json_extract('{\"a\":1}', '$.a');",
            parameters: null,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task InsertAsync(RawPacketRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.Table))
            throw new ArgumentException("Table is required.", nameof(record));

        if (!ValidIdentifier.IsMatch(record.Table))
            throw new ArgumentException(
                "Table name contains invalid characters. Only letters, digits and underscore are allowed.",
                nameof(record));

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new ArgumentException("Id is required.", nameof(record));

        if (string.IsNullOrWhiteSpace(record.JsonDocumentOriginal))
            throw new ArgumentException("JsonDocumentOriginal is required.", nameof(record));

        const string columns = "(id, json_document_original, application_received_utc_timestampz, installation_id)";
        var sql = $"INSERT INTO \"{record.Table}\" {columns} VALUES ($id, $json, $received_utc, $installation_id);";

        await using var session = await GetInitializedSqliteDatabase().OpenSessionAsync(cancellationToken).ConfigureAwait(false);

        _ = await session.ExecuteAsync(
            sql,
            [
                new DbParam("$id", record.Id),
                new DbParam("$json", record.JsonDocumentOriginal),
                new DbParam("$received_utc", record.ApplicationReceivedUtcTimestampz),
                new DbParam("$installation_id", string.IsNullOrWhiteSpace(record.InstallationId) ? DBNull.Value : record.InstallationId),
            ],
            cancellationToken).ConfigureAwait(false);
    }
}
