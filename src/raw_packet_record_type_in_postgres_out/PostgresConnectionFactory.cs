namespace RawPacketRecordTypedInPostgresOut;

using System.Threading;
using Npgsql;
using System.Data;

/// <summary>
/// Creates a PostgreSQL connection using the configured connection string.
/// Supports both synchronous and asynchronous connection creation.
/// </summary>
internal class PostgresConnectionFactory
{
    ILogger? IFileLogger { get; set; }
    ILogger IFileLoggerSafe => NullPropertyGuard.GetSafeClass(
        IFileLogger, "PostgresConnectionFactory not initialized. Call InitializeAsync before using.");

    string? ConnectionString { get; set; }
    string ConnectionStringSafe => NullPropertyGuard.GetSafeClass(
        ConnectionString, "PostgresConnectionFactory not initialized. Call InitializeAsync before using.", IFileLoggerSafe);

    PostgresConnectionFactory()
    {
    }

    async Task<bool> InitializeAsync(ILogger iFileLogger, string connectionString)
    {
        IFileLogger = iFileLogger;
        ConnectionString = connectionString;
        return await Task.FromResult(true);
    }

    public static async Task<PostgresConnectionFactory> CreateAsync(
        ILogger iFileLogger, string connectionString)
    {
        var postgresConnectionFactory = new PostgresConnectionFactory();
        if (await postgresConnectionFactory.InitializeAsync(iFileLogger, connectionString))
            return postgresConnectionFactory;

        throw new InvalidOperationException("Failed to initialize PostgresConnectionFactory.");
    }

    /// <summary>
    /// Creates a new connection and opens it synchronously.
    /// Use this for backward compatibility or when you need a synchronously-opened connection.
    /// </summary>
    public IDbConnection CreateConnection()
    {
        try
        {
            var connection = new NpgsqlConnection(ConnectionStringSafe);
            connection.Open();
            IFileLoggerSafe.Debug($"🔌 Connection opened (sync): {connection.State}");
            return connection;
        }
        catch (Exception exception)
        {
            throw IFileLoggerSafe.LogExceptionAndReturn(
                new InvalidOperationException(
                    "Failed to create and open PostgreSQL connection.", exception));
        }
    }

    /// <summary>
    /// Creates a new connection WITHOUT opening it.
    /// Caller is responsible for opening the connection.
    /// Useful for testing or when you need more control over connection lifecycle.
    /// </summary>
    public NpgsqlConnection CreateConnectionClosed()
    {
        try
        {
            var connection = new NpgsqlConnection(ConnectionStringSafe);
            IFileLoggerSafe.Debug($"🔌 Connection created (closed): {connection.State}");
            return connection;
        }
        catch (Exception exception)
        {
            throw IFileLoggerSafe.LogExceptionAndReturn(
                new InvalidOperationException(
                    "Failed to create PostgreSQL connection.", exception));
        }
    }

    /// <summary>
    /// Creates a new connection and opens it asynchronously.
    /// Preferred method for async operations - more efficient than CreateConnection().
    /// This overload does not accept cancellation.
    /// </summary>
    public async Task<NpgsqlConnection> CreateConnectionAsync()
        => await CreateConnectionAsync(CancellationToken.None).ConfigureAwait(false);

    /// <summary>
    /// Creates a new connection and opens it asynchronously with cancellation support.
    /// Use this when you need to support cancellation of the connection attempt.
    /// </summary>
    public async Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connection = new NpgsqlConnection(ConnectionStringSafe);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            IFileLoggerSafe.Debug($"🔌 Connection opened (async): {connection.State}");
            return connection;
        }
        catch (OperationCanceledException)
        {
            IFileLoggerSafe.Warning("⚠️ Connection opening was canceled");
            throw;
        }
        catch (Exception exception)
        {
            throw IFileLoggerSafe.LogExceptionAndReturn(
                new InvalidOperationException(
                    "Failed to create and open PostgreSQL connection asynchronously.", exception));
        }
    }

    /// <summary>
    /// Tests the connection by opening it and running a simple query.
    /// Returns true if successful, false otherwise.
    /// Accepts an optional cancellation token for the test operation.
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            IFileLoggerSafe.Debug($"✅ Connection test successful: SELECT 1 returned {result}");
            return result != null && result.ToString() == "1";
        }
        catch (OperationCanceledException)
        {
            IFileLoggerSafe.Warning("⚠️ Connection test canceled");
            return false;
        }
        catch (Exception exception)
        {
            IFileLoggerSafe.Warning($"⚠️ Connection test failed: {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the PostgreSQL server version.
    /// Useful for diagnostics and logging.
    /// </summary>
    public async Task<string?> GetServerVersionAsync()
    {
        try
        {
            await using var connection = await CreateConnectionAsync().ConfigureAwait(false);
            await using var command = new NpgsqlCommand("SELECT version()", connection);
            var version = await command.ExecuteScalarAsync();

            return version?.ToString();
        }
        catch (Exception exception)
        {
            IFileLoggerSafe.Warning($"⚠️ Failed to get server version: {exception.Message}");
            return null;
        }
    }
}