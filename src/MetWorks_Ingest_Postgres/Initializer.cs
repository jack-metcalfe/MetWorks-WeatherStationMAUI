namespace MetWorks.Ingest.Postgres;
/// <summary>
/// Applies DDL scripts to initialize the schema.  Designed to be tolerant of transient failures:
/// - each script is attempted and failures are logged but do not abort the whole initialization.
/// - caller can pass a cancellation token (e.g., a short timeout) when running initialization synchronously.
/// - recommended: run DatabaseInitializeAsync in background with retries if desired by caller.
/// </summary>
internal class Initializer
{
    internal static async Task<bool> DatabaseInitializeAsync(
        ILogger iFileLogger,
        NpgsqlConnection npgSqlConnection,
        CancellationToken cancellationToken = default
    )
    {
        iFileLogger.Information("🔄 Beginning PostgreSQL schema initialization (tolerant mode)");

        // Ensure connection is open
        if (npgSqlConnection.State != System.Data.ConnectionState.Open)
        {
            try
            {
                await npgSqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                iFileLogger.Warning("⚠️ Schema initialization canceled while opening connection");
                return false;
            }
            catch (Exception exOpen)
            {
                iFileLogger.Warning($"⚠️ Could not open connection for schema init: {exOpen.Message}");
                return false;
            }
        }

        int applied = 0;
        foreach (var UdpPacketTableEntry in UdpPacketTableData.PacketTableDataMap.Values)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                iFileLogger.Warning("⚠️ Schema initialization canceled");
                break;
            }

            try
            {
                var scriptPath = Path.Combine(@"Ingest",DatabaseEnum.PostgreSQL.ToString(), UdpPacketTableEntry.TableScriptName);
                var script = IResourceProvider.GetString(scriptPath.Replace('\\', '/'));
                if (string.IsNullOrEmpty(script))
                {
                    iFileLogger.Warning($"⚠️ PostgreSQL script {scriptPath} not found in string provider; skipping.");
                    continue;
                }

                iFileLogger.Information($"Applying PostgreSQL DDL from {scriptPath}");
                // Execute as a command; scripts may contain multiple statements, Npgsql supports it.
                await using var cmd = new NpgsqlCommand(script, npgSqlConnection);
                cmd.CommandTimeout = 60; // seconds
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                applied++;
            }
            catch (OperationCanceledException)
            {
                iFileLogger.Warning("⚠️ Schema script execution canceled");
                break;
            }
            catch (Exception exception)
            {
                // Log the script error but continue with the next script. Caller can retry entire initializer later.
                iFileLogger.Warning($"⚠️ Failed to apply script '{UdpPacketTableEntry}': {exception.Message}");
                iFileLogger.Debug($"   Stack trace: {exception.StackTrace}");
            }
        }

        iFileLogger.Information($"✅ PostgreSQL schema initialization completed. Scripts applied: {applied}/{UdpPacketTableData.PacketTableDataMap.Count}");
        return applied > 0;
    }
}