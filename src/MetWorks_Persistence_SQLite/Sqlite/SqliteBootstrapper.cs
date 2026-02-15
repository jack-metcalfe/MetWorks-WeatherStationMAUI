namespace MetWorks.Persistence.SQLite;
public sealed class SqliteBootstrapper
{
    bool _isInitialized;

    ILogger? _iLogger;
    ILogger ILogger
    {
        get
        {
            if (!_isInitialized || _iLogger is null)
            {
                throw new InvalidOperationException($"{nameof(SqliteBootstrapper)} not initialized.");
            }
            return _iLogger;
        }
        set => _iLogger = value;
    }

    ISettingProvider? _iSettingProvider;
    ISettingProvider ISettingProvider
    {
        get
        {
            if (!_isInitialized || _iSettingProvider is null)
            {
                throw new InvalidOperationException($"{nameof(SqliteBootstrapper)} not initialized.");
            }
            return _iSettingProvider;
        }
        set => _iSettingProvider = value;
    }

    public string? ResolvedDatabasePath { get; private set; }

    public SqliteBootstrapper() { }

    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingProvider iSettingProvider,
        IPlatformPaths? iPlatformPaths,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingProvider);

        if (_isInitialized) return true;

        ILogger = iLogger;
        ISettingProvider = iSettingProvider;
        _isInitialized = true;

        try
        {
            var settings = SqliteDbSettingsLoader.Load(ISettingProvider);

            var paths = iPlatformPaths ?? new DefaultPlatformPaths();

            var rawDbPath = settings.DbPath;
            var isPathRooted = Path.IsPathRooted(rawDbPath);
            var dbPath = ResolveDbPath(paths, rawDbPath);

            ILogger.Information(
                $"sqlite.bootstrap paths: app_data_dir='{paths.AppDataDirectory}' db_path_raw='{rawDbPath}' db_path_is_absolute='{isPathRooted}' db_path_resolved='{dbPath}'"
            );

            var builder = !string.IsNullOrWhiteSpace(settings.ConnectionString)
                ? new SqliteConnectionStringBuilder(settings.ConnectionString)
                : new SqliteConnectionStringBuilder
                {
                    DataSource = dbPath
                };

            ResolvedDatabasePath = builder.DataSource;

            await using var connection = new SqliteConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await ApplyPragmasAsync(connection, settings, cancellationToken).ConfigureAwait(false);

            var ddlApplied = await SqliteSchemaBootstrapper.ApplyAllAsync(
                ILogger,
                connection,
                cancellationToken
            ).ConfigureAwait(false);

            ILogger.Information($"sqlite.bootstrap ddl: applied='{ddlApplied}'");

            ILogger.Information($"SQLite bootstrap OK. DataSource='{ResolvedDatabasePath}'.");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Must not fail app startup.
            ILogger.Warning($"SQLite bootstrap failed: {exception.GetType().Name}: {exception.Message}");
            return false;
        }
    }

    static string ResolveDbPath(IPlatformPaths paths, string dbPath)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (string.IsNullOrWhiteSpace(dbPath))
        {
            throw new ArgumentException("dbPath setting is required", nameof(dbPath));
        }

        if (Path.IsPathRooted(dbPath))
        {
            return dbPath;
        }

        var candidate = Path.Combine(paths.AppDataDirectory, dbPath);
        var absolute = Path.GetFullPath(candidate);
        var appDataFull = Path.GetFullPath(paths.AppDataDirectory);

        if (!absolute.StartsWith(appDataFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved db path escapes the application data directory.");
        }

        var dir = Path.GetDirectoryName(absolute);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        return absolute;
    }

    static async Task ApplyPragmasAsync(
        SqliteConnection connection,
        SqliteDbSettings settings,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string synchronous = "NORMAL";

        var commands = new List<string>
        {
            $"PRAGMA journal_mode={settings.JournalMode};",
            $"PRAGMA synchronous={synchronous};",
            $"PRAGMA busy_timeout={settings.BusyTimeoutMs};"
        };

        foreach (var sql in commands)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // Meta table is now handled by SqliteSchemaBootstrapper.
}
