namespace MetWorks.Persistence.SQLite;
public sealed class SqliteWriteService
{
    bool _isInitialized;

    ILogger? _iLogger;
    ILogger ILogger
    {
        get
        {
            if (!_isInitialized || _iLogger is null)
            {
                throw new InvalidOperationException($"{nameof(SqliteWriteService)} not initialized.");
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
                throw new InvalidOperationException($"{nameof(SqliteWriteService)} not initialized.");
            }
            return _iSettingProvider;
        }
        set => _iSettingProvider = value;
    }

    readonly SemaphoreSlim _gate = new(1, 1);

    SqliteDbSettings? _settings;
    string? _connectionString;

    public SqliteWriteService() { }

    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingProvider iSettingProvider,
        IPlatformPaths? iPlatformPaths,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingProvider);

        if (_isInitialized) return Task.FromResult(true);

        ILogger = iLogger;
        ISettingProvider = iSettingProvider;
        _isInitialized = true;

        if (cancellationToken.IsCancellationRequested) return Task.FromResult(false);

        try
        {
            _settings = SqliteDbSettingsLoader.Load(ISettingProvider);

            var paths = iPlatformPaths ?? new DefaultPlatformPaths();
            var resolvedDbPath = ResolveDbPath(paths, _settings.DbPath);

            var builder = !string.IsNullOrWhiteSpace(_settings.ConnectionString)
                ? new SqliteConnectionStringBuilder(_settings.ConnectionString)
                : new SqliteConnectionStringBuilder
                {
                    DataSource = resolvedDbPath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Cache = SqliteCacheMode.Shared
                };

            _connectionString = builder.ConnectionString;

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            // best-effort: service presence shouldn't break startup
            ILogger.Warning($"sqlite.write_service init failed: {ex.GetType().Name}: {ex.Message}");
            _connectionString = null;
            _settings = null;
            return Task.FromResult(false);
        }
    }

    public async Task<int> ExecuteAsync(
        string sql,
        IReadOnlyList<SqliteParam> parameters,
        CancellationToken cancellationToken
    )
    {
        if (!_isInitialized) throw new InvalidOperationException($"{nameof(SqliteWriteService)} not initialized.");
        if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentException("SQL is required.", nameof(sql));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (string.IsNullOrWhiteSpace(_connectionString) || _settings is null)
            {
                return 0;
            }

            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            await ApplyPragmasAsync(conn, _settings, cancellationToken).ConfigureAwait(false);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (parameters is not null)
            {
                for (var i = 0; i < parameters.Count; i++)
                {
                    parameters[i].BindTo(cmd);
                }
            }

            return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = _gate.Release();
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

        foreach (var pragma in commands)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = pragma;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
