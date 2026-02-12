// Template:            Registry
// Version:             1.1
// Template Requested:  Registry
// Template:            File.Header
// Version:             1.1
// Template Requested:  Registry
// Generated On:        2026-02-12T05:35:58.8534477Z
#nullable enable

namespace MetWorks.ServiceRegistry
{
    // The Registry class orchestrates the full lifecycle of all named instances.
    // Phase 1: Creation (synchronous)
    // Phase 2: Initialization (asynchronous)
    // Phase 3: Disposal (optional)
    public partial class Registry
    {
        // Phase 1: Creation (synchronous, safe).
        // Each instance is created via its dedicated InstanceFactory.
        // Element-driven and assignment-driven construction logic is encapsulated per instance.
        public void CreateAll()
        {
            RootCancellationTokenSource_InstanceFactory.Create(this);
            TheMetricsLatestSnapshotStore_InstanceFactory.Create(this);
            TheEventRelayBasic_InstanceFactory.Create(this);
            TheEventRelayPath_InstanceFactory.Create(this);
            TheLoggerStub_InstanceFactory.Create(this);
            TheSettingProvider_InstanceFactory.Create(this);
            TheSettingRepository_InstanceFactory.Create(this);
            TheInstanceIdentifier_InstanceFactory.Create(this);
            TheLoggerFile_InstanceFactory.Create(this);
            TheLoggerSQLite_InstanceFactory.Create(this);
            TheLoggerResilient_InstanceFactory.Create(this);
            TheProvenanceTracker_InstanceFactory.Create(this);
            TheStreamShippingHttpClientProvider_InstanceFactory.Create(this);
            TheStationMetadataStreamShipper_InstanceFactory.Create(this);
            TheMetricsSummaryIngestor_InstanceFactory.Create(this);
            TheLightningStreamShipper_InstanceFactory.Create(this);
            TheLoggerSQLiteStreamShipper_InstanceFactory.Create(this);
            TheObservationStreamShipper_InstanceFactory.Create(this);
            ThePrecipitationStreamShipper_InstanceFactory.Create(this);
            TheWindStreamShipper_InstanceFactory.Create(this);
            TheMetricsSamplerService_InstanceFactory.Create(this);
            TheObservationRollupWorker_InstanceFactory.Create(this);
            TheTempestRestClient_InstanceFactory.Create(this);
            TheSQLiteStationMetadataIngestor_InstanceFactory.Create(this);
            TheStationMetadataProvider_InstanceFactory.Create(this);
            TheUnitsOfMeasureInitializer_InstanceFactory.Create(this);
            TheSensorReadingTransformer_InstanceFactory.Create(this);
            TheUdpListener_InstanceFactory.Create(this);
            TheSQLiteRawPacketIngestor_InstanceFactory.Create(this);
        }

        // Phase 2: Initialization (async, potentially slow).
        // Only instances with assignments require async initialization.
        // Element-driven instances are fully constructed during creation.
        public async Task InitializeAllAsync()
        {
            await TheSettingProvider_Initializer.Initialize_TheSettingProviderAsync(this).ConfigureAwait(false);
            await TheSettingRepository_Initializer.Initialize_TheSettingRepositoryAsync(this).ConfigureAwait(false);
            await TheInstanceIdentifier_Initializer.Initialize_TheInstanceIdentifierAsync(this).ConfigureAwait(false);
            await TheLoggerFile_Initializer.Initialize_TheLoggerFileAsync(this).ConfigureAwait(false);
            await TheLoggerSQLite_Initializer.Initialize_TheLoggerSQLiteAsync(this).ConfigureAwait(false);
            await TheLoggerResilient_Initializer.Initialize_TheLoggerResilientAsync(this).ConfigureAwait(false);
            await TheProvenanceTracker_Initializer.Initialize_TheProvenanceTrackerAsync(this).ConfigureAwait(false);
            await TheStreamShippingHttpClientProvider_Initializer.Initialize_TheStreamShippingHttpClientProviderAsync(this).ConfigureAwait(false);
            await TheStationMetadataStreamShipper_Initializer.Initialize_TheStationMetadataStreamShipperAsync(this).ConfigureAwait(false);
            await TheMetricsSummaryIngestor_Initializer.Initialize_TheMetricsSummaryIngestorAsync(this).ConfigureAwait(false);
            await TheLightningStreamShipper_Initializer.Initialize_TheLightningStreamShipperAsync(this).ConfigureAwait(false);
            await TheLoggerSQLiteStreamShipper_Initializer.Initialize_TheLoggerSQLiteStreamShipperAsync(this).ConfigureAwait(false);
            await TheObservationStreamShipper_Initializer.Initialize_TheObservationStreamShipperAsync(this).ConfigureAwait(false);
            await ThePrecipitationStreamShipper_Initializer.Initialize_ThePrecipitationStreamShipperAsync(this).ConfigureAwait(false);
            await TheWindStreamShipper_Initializer.Initialize_TheWindStreamShipperAsync(this).ConfigureAwait(false);
            await TheMetricsSamplerService_Initializer.Initialize_TheMetricsSamplerServiceAsync(this).ConfigureAwait(false);
            await TheObservationRollupWorker_Initializer.Initialize_TheObservationRollupWorkerAsync(this).ConfigureAwait(false);
            await TheTempestRestClient_Initializer.Initialize_TheTempestRestClientAsync(this).ConfigureAwait(false);
            await TheSQLiteStationMetadataIngestor_Initializer.Initialize_TheSQLiteStationMetadataIngestorAsync(this).ConfigureAwait(false);
            await TheStationMetadataProvider_Initializer.Initialize_TheStationMetadataProviderAsync(this).ConfigureAwait(false);
            await TheUnitsOfMeasureInitializer_Initializer.Initialize_TheUnitsOfMeasureInitializerAsync(this).ConfigureAwait(false);
            await TheSensorReadingTransformer_Initializer.Initialize_TheSensorReadingTransformerAsync(this).ConfigureAwait(false);
            await TheUdpListener_Initializer.Initialize_TheUdpListenerAsync(this).ConfigureAwait(false);
            await TheSQLiteRawPacketIngestor_Initializer.Initialize_TheSQLiteRawPacketIngestorAsync(this).ConfigureAwait(false);
        }

        // Phase 3: Disposal (optional).
        // Emitted only for instances that require cleanup.
        public void DisposeAll()
        {
        }
    }
}
