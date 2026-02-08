namespace MetWorks.Ingest.SQLite;
/// <summary>
/// Applies DDL scripts to initialize the schema. Designed to be tolerant of transient failures:
/// - each script is attempted and failures are logged but do not abort the whole initialization.
/// - caller can pass a cancellation token (e.g., a short timeout) when running initialization synchronously.
/// - recommended: run DatabaseInitializeAsync in background with retries if desired by caller.
/// </summary>
internal static class Initializer
{
    internal static async Task<bool> DatabaseInitializeAsync(
        ILogger iFileLogger,
        SqliteConnection sqliteConnection,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(iFileLogger);
        ArgumentNullException.ThrowIfNull(sqliteConnection);

        iFileLogger.Information("🔄 Beginning SQLite schema initialization (tolerant mode)");

        if (sqliteConnection.State != System.Data.ConnectionState.Open)
        {
            try
            {
                await sqliteConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
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
        foreach (var udpPacketTableEntry in UdpPacketTableData.PacketTableDataMap.Values)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                iFileLogger.Warning("⚠️ Schema initialization canceled");
                break;
            }

            try
            {
                var scriptPath = Path.Combine(@"Ingest", DatabaseEnum.SQLite.ToString(), udpPacketTableEntry.TableScriptName);
                var script = IResourceProvider.GetString(scriptPath.Replace('\\', '/'));
                if (string.IsNullOrEmpty(script))
                {
                    iFileLogger.Warning($"⚠️ SQLite script {scriptPath} not found in string provider; skipping.");
                    continue;
                }

                iFileLogger.Information($"Applying SQLite DDL from {scriptPath}");

                await using var cmd = sqliteConnection.CreateCommand();
                cmd.CommandText = script;
                cmd.CommandTimeout = 60;
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
                iFileLogger.Warning($"⚠️ Failed to apply script '{udpPacketTableEntry}': {exception.Message}");
                iFileLogger.Debug($"   Stack trace: {exception.StackTrace}");
            }
        }

        iFileLogger.Information($"✅ SQLite schema initialization completed. Scripts applied: {applied}/{UdpPacketTableData.PacketTableDataMap.Count}");
        return applied > 0;
    }
}
