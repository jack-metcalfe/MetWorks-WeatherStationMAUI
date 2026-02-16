using System.Text.RegularExpressions;
using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Metrics;

public sealed class MetricsDatabaseReadiness : IMetricsDatabaseReadiness
{
    static readonly Regex ValidIdentifier = new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    ISqliteDatabase? _sqliteDatabase;

    public MetricsDatabaseReadiness()
    {
    }

    public Task<bool> InitializeAsync(ISqliteDatabase sqliteDatabase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);

        _sqliteDatabase = sqliteDatabase;
        return Task.FromResult(true);
    }

    ISqliteDatabase GetInitializedSqliteDatabase() =>
        _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(MetricsDatabaseReadiness)} has not been initialized.");

    public Task EnsureReadyAsync(string table, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table is required.", nameof(table));

        if (!ValidIdentifier.IsMatch(table))
            throw new ArgumentException(
                "Table name contains invalid characters. Only letters, digits and underscore are allowed.",
                nameof(table));

        var sqlScript = MetricsSqlScripts.GetForTable(table);

        return GetInitializedSqliteDatabase().ExecuteDdlAsync(sqlScript, cancellationToken);
    }
}
