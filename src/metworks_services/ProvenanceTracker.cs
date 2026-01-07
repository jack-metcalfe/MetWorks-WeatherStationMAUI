using MetWorksModels.Provenance;

namespace MetWorksServices;

/// <summary>
/// Centralized in-memory provenance tracking service.
/// Maintains lineage for the last 1000 weather data packets with LRU eviction.
/// Thread-safe for concurrent access from multiple pipeline components.
/// </summary>
public class ProvenanceTracker
{
    private const int MaxLineages = 1000;
    
    private readonly ConcurrentDictionary<Guid, DataLineage> _lineageStore = new();
    private readonly ConcurrentQueue<Guid> _lineageQueue = new(); // LRU tracking
    private IFileLogger? _fileLogger;
    
    private IFileLogger FileLoggerSafe => 
        _fileLogger ?? throw new InvalidOperationException("ProvenanceTracker not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Initializes the provenance tracker.
    /// </summary>
    public async Task<bool> InitializeAsync(IFileLogger iFileLogger)
    {
        if (iFileLogger is null)
            throw new ArgumentNullException(nameof(iFileLogger), "File logger cannot be null.");

        _fileLogger = iFileLogger;
        _fileLogger.Information("🔍 ProvenanceTracker initialized successfully");
        
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Begins tracking a new raw packet. Creates initial lineage record.
    /// </summary>
    /// <param name="packet">Raw UDP packet to track</param>
    /// <returns>Initial DataLineage record</returns>
    public DataLineage TrackNewPacket(IRawPacketRecordTyped packet)
    {
        if (packet is null)
            throw new ArgumentNullException(nameof(packet));

        var lineage = new DataLineage
        {
            RawPacketId = packet.Id,
            PacketType = packet.PacketEnum,
            Status = DataStatus.Received,
            ReceivedUtc = DateTime.UtcNow,
            ProcessingSteps = new List<ProvenanceStep>
            {
                new ProvenanceStep
                {
                    StepName = "UDP Receipt",
                    Timestamp = DateTime.UtcNow,
                    Component = "UdpTransformer",
                    Details = $"Packet type: {packet.PacketEnum}",
                    ResultingStatus = DataStatus.Received
                }
            },
            OriginalJson = packet.RawPacketJson
        };

        // Add to store and queue
        _lineageStore[packet.Id] = lineage;
        _lineageQueue.Enqueue(packet.Id);

        // Enforce LRU eviction
        EnforceLruLimit();

        FileLoggerSafe.Debug($"📊 Tracking new packet: {packet.Id} ({packet.PacketEnum})");
        
        return lineage;
    }

    /// <summary>
    /// Adds a processing step to an existing lineage.
    /// </summary>
    public void AddStep(Guid packetId, string stepName, string component, string? details = null, TimeSpan? duration = null)
    {
        if (!_lineageStore.TryGetValue(packetId, out var lineage))
        {
            FileLoggerSafe.Warning($"⚠️ Cannot add step '{stepName}' - lineage not found for packet: {packetId}");
            return;
        }

        var step = new ProvenanceStep
        {
            StepName = stepName,
            Timestamp = DateTime.UtcNow,
            Component = component,
            Details = details,
            Duration = duration
        };

        lineage.ProcessingSteps.Add(step);
        
        // Update lineage with new timestamp
        var updatedLineage = lineage with { LastUpdated = DateTime.UtcNow };
        _lineageStore[packetId] = updatedLineage;

        FileLoggerSafe.Debug($"📝 Added step '{stepName}' to packet {packetId}");
    }

    /// <summary>
    /// Updates the status of a packet's lineage.
    /// </summary>
    public void UpdateStatus(Guid packetId, DataStatus newStatus)
    {
        if (!_lineageStore.TryGetValue(packetId, out var lineage))
        {
            FileLoggerSafe.Warning($"⚠️ Cannot update status - lineage not found for packet: {packetId}");
            return;
        }

        var updatedLineage = lineage with 
        { 
            Status = newStatus,
            LastUpdated = DateTime.UtcNow 
        };
        
        _lineageStore[packetId] = updatedLineage;

        FileLoggerSafe.Debug($"📊 Updated status for packet {packetId}: {newStatus}");
    }

    /// <summary>
    /// Records an error that occurred during packet processing.
    /// </summary>
    public void RecordError(Guid packetId, string component, string stepName, Exception exception)
    {
        if (!_lineageStore.TryGetValue(packetId, out var lineage))
        {
            FileLoggerSafe.Warning($"⚠️ Cannot record error - lineage not found for packet: {packetId}");
            return;
        }

        var error = ProcessingError.FromException(packetId, component, stepName, exception);
        
        var errors = lineage.Errors?.ToList() ?? new List<ProcessingError>();
        errors.Add(error);

        var updatedLineage = lineage with 
        { 
            Errors = errors,
            Status = DataStatus.Failed,
            LastUpdated = DateTime.UtcNow 
        };
        
        _lineageStore[packetId] = updatedLineage;

        FileLoggerSafe.Error($"❌ Error recorded for packet {packetId} in {component}.{stepName}: {exception.Message}");
    }

    /// <summary>
    /// Links a transformed reading to its source packet.
    /// </summary>
    public void LinkTransformedReading(Guid packetId, Guid transformedId)
    {
        if (!_lineageStore.TryGetValue(packetId, out var lineage))
        {
            FileLoggerSafe.Warning($"⚠️ Cannot link transformed reading - lineage not found for packet: {packetId}");
            return;
        }

        var updatedLineage = lineage with 
        { 
            TransformedReadingId = transformedId,
            Status = DataStatus.Transformed,
            LastUpdated = DateTime.UtcNow 
        };
        
        _lineageStore[packetId] = updatedLineage;

        FileLoggerSafe.Debug($"🔗 Linked transformed reading {transformedId} to packet {packetId}");
    }

    /// <summary>
    /// Links a database record to its source packet.
    /// </summary>
    public void LinkDatabaseRecord(Guid packetId, Guid dbRecordId)
    {
        if (!_lineageStore.TryGetValue(packetId, out var lineage))
        {
            FileLoggerSafe.Warning($"⚠️ Cannot link database record - lineage not found for packet: {packetId}");
            return;
        }

        var updatedLineage = lineage with 
        { 
            DatabaseRecordId = dbRecordId,
            Status = DataStatus.Persisted,
            LastUpdated = DateTime.UtcNow 
        };
        
        _lineageStore[packetId] = updatedLineage;

        FileLoggerSafe.Debug($"💾 Linked database record {dbRecordId} to packet {packetId}");
    }

    /// <summary>
    /// Retrieves the complete lineage for a specific packet.
    /// </summary>
    public DataLineage? GetLineage(Guid packetId)
    {
        return _lineageStore.TryGetValue(packetId, out var lineage) ? lineage : null;
    }

    /// <summary>
    /// Gets the most recent lineages (COMB GUID sorted chronologically).
    /// </summary>
    public List<DataLineage> GetRecentLineages(int count = 100)
    {
        return _lineageStore.Values
            .OrderByDescending(l => l.RawPacketId) // COMB GUIDs sort chronologically
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Gets all lineages with a specific status.
    /// </summary>
    public List<DataLineage> GetLineagesByStatus(DataStatus status)
    {
        return _lineageStore.Values
            .Where(l => l.Status == status)
            .OrderByDescending(l => l.RawPacketId)
            .ToList();
    }

    /// <summary>
    /// Gets lineages within a specific time range.
    /// </summary>
    public List<DataLineage> GetLineagesByTimeRange(DateTime start, DateTime end)
    {
        return _lineageStore.Values
            .Where(l => l.ReceivedUtc >= start && l.ReceivedUtc <= end)
            .OrderBy(l => l.ReceivedUtc)
            .ToList();
    }

    /// <summary>
    /// Calculates performance statistics across all tracked lineages.
    /// </summary>
    public ProvenanceStatistics GetStatistics()
    {
        var allLineages = _lineageStore.Values.ToList();
        
        if (allLineages.Count == 0)
        {
            return new ProvenanceStatistics
            {
                TotalPackets = 0,
                TotalErrors = 0,
                AverageProcessingTime = TimeSpan.Zero,
                P50ProcessingTime = TimeSpan.Zero,
                P95ProcessingTime = TimeSpan.Zero,
                P99ProcessingTime = TimeSpan.Zero
            };
        }

        var processingTimes = allLineages
            .Select(l => l.TotalProcessingTime)
            .OrderBy(t => t)
            .ToList();

        return new ProvenanceStatistics
        {
            TotalPackets = allLineages.Count,
            TotalErrors = allLineages.Count(l => l.HasErrors),
            AverageProcessingTime = TimeSpan.FromMilliseconds(processingTimes.Average(t => t.TotalMilliseconds)),
            P50ProcessingTime = GetPercentile(processingTimes, 0.50),
            P95ProcessingTime = GetPercentile(processingTimes, 0.95),
            P99ProcessingTime = GetPercentile(processingTimes, 0.99),
            ByStatus = allLineages.GroupBy(l => l.Status)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByPacketType = allLineages.GroupBy(l => l.PacketType)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// Exports a lineage as JSON for diagnostics.
    /// </summary>
    public string ExportLineageAsJson(Guid packetId)
    {
        var lineage = GetLineage(packetId);
        
        if (lineage is null)
            return $"{{\"error\": \"Lineage not found for packet {packetId}\"}}";

        return JsonSerializer.Serialize(lineage, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    /// <summary>
    /// Gets the total number of tracked lineages.
    /// </summary>
    public int GetLineageCount() => _lineageStore.Count;

    // ========================================
    // Private Helper Methods
    // ========================================

    private void EnforceLruLimit()
    {
        while (_lineageStore.Count > MaxLineages && _lineageQueue.TryDequeue(out var oldestId))
        {
            if (_lineageStore.TryRemove(oldestId, out var removed))
            {
                FileLoggerSafe.Debug($"🗑️ Evicted old lineage (LRU): {oldestId}");
            }
        }
    }

    private static TimeSpan GetPercentile(List<TimeSpan> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return TimeSpan.Zero;
        
        int index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
        
        return sortedValues[index];
    }
}

/// <summary>
/// Performance statistics for provenance tracking.
/// </summary>
public record ProvenanceStatistics
{
    public required int TotalPackets { get; init; }
    public required int TotalErrors { get; init; }
    public required TimeSpan AverageProcessingTime { get; init; }
    public required TimeSpan P50ProcessingTime { get; init; }
    public required TimeSpan P95ProcessingTime { get; init; }
    public required TimeSpan P99ProcessingTime { get; init; }
    public Dictionary<DataStatus, int>? ByStatus { get; init; }
    public Dictionary<PacketEnum, int>? ByPacketType { get; init; }
}