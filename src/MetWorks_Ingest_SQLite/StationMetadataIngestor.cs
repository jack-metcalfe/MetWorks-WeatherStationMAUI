namespace MetWorks.Ingest.SQLite;
public sealed class StationMetadataIngestor : ServiceBase, IStationMetadataPersister
{
    Guid _installationIdGuid;
    MetWorks.Persistence.StationMetadata.IStationMetadataDatabaseReadiness? _databaseReadiness;
    MetWorks.Persistence.StationMetadata.IStationMetadataRepository? _repository;

    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IInstanceIdentifier iInstanceIdentifier,
        MetWorks.Persistence.StationMetadata.IStationMetadataDatabaseReadiness stationMetadataDatabaseReadiness,
        MetWorks.Persistence.StationMetadata.IStationMetadataRepository stationMetadataRepository,
        CancellationToken externalCancellation = default
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);
        ArgumentNullException.ThrowIfNull(stationMetadataDatabaseReadiness);
        ArgumentNullException.ThrowIfNull(stationMetadataRepository);

        InitializeBase(
            iLogger.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation
        );

        _databaseReadiness = stationMetadataDatabaseReadiness;
        _repository = stationMetadataRepository;

        var iid = iInstanceIdentifier.GetOrCreateInstallationId();
        if (!Guid.TryParse(iid, out _installationIdGuid))
            _installationIdGuid = Guid.Empty;

        IEventRelayBasic.Register<StationMetadata>(this, md =>
        {
            StartBackground(ct => PersistAsync(md, ct));
        });

        MarkReady();
        return Task.FromResult(true);
    }

    public async Task PersistAsync(StationMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        await Ready.ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();


        var readiness = _databaseReadiness;
        if (readiness is null)
            throw new InvalidOperationException($"{nameof(MetWorks.Persistence.StationMetadata.IStationMetadataDatabaseReadiness)} is not initialized.");

        var repository = _repository;
        if (repository is null)
            throw new InvalidOperationException($"{nameof(MetWorks.Persistence.StationMetadata.IStationMetadataRepository)} is not initialized.");

        try
        {
            await readiness.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = false });

            await repository.InsertAsync(
                new MetWorks.Persistence.StationMetadata.StationMetadataInsertRow(
                    Id: IdGenerator.CreateCombGuid().ToString(),
                    RetrievedUtc: metadata.RetrievedUtc.UtcDateTime,
                    StationId: metadata.StationId,
                    StationName: metadata.StationName,
                    TempestDeviceName: metadata.TempestDeviceName,
                    Latitude: metadata.Latitude,
                    Longitude: metadata.Longitude,
                    ElevationMeters: metadata.ElevationMeters,
                    JsonDocumentOriginal: json,
                    InstallationId: _installationIdGuid == Guid.Empty ? null : _installationIdGuid.ToString()),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            ILogger.Error("Error persisting station metadata: {Message}", exception);
        }
    }
}
