// Template:            Registry
// Version:             1.1
// Template Requested:  Registry
// Template:            File.Header
// Version:             1.1
// Template Requested:  Registry
// Generated On:        2026-01-26T23:47:15.2298447Z
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
            TheEventRelayBasic_InstanceFactory.Create(this);
            TheEventRelayPath_InstanceFactory.Create(this);
            TheLoggerStub_InstanceFactory.Create(this);
            TheSettingProvider_InstanceFactory.Create(this);
            TheSettingRepository_InstanceFactory.Create(this);
            TheInstanceIdentifier_InstanceFactory.Create(this);
            TheLoggerFile_InstanceFactory.Create(this);
            TheLoggerPostgreSQL_InstanceFactory.Create(this);
            TheLoggerResilient_InstanceFactory.Create(this);
            TheProvenanceTracker_InstanceFactory.Create(this);
            TheUnitsOfMeasureInitializer_InstanceFactory.Create(this);
            TheSensorReadingTransformer_InstanceFactory.Create(this);
            TheUdpListener_InstanceFactory.Create(this);
            ThePostgresRawPacketIngestor_InstanceFactory.Create(this);
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
            await TheLoggerPostgreSQL_Initializer.Initialize_TheLoggerPostgreSQLAsync(this).ConfigureAwait(false);
            await TheLoggerResilient_Initializer.Initialize_TheLoggerResilientAsync(this).ConfigureAwait(false);
            await TheProvenanceTracker_Initializer.Initialize_TheProvenanceTrackerAsync(this).ConfigureAwait(false);
            await TheUnitsOfMeasureInitializer_Initializer.Initialize_TheUnitsOfMeasureInitializerAsync(this).ConfigureAwait(false);
            await TheSensorReadingTransformer_Initializer.Initialize_TheSensorReadingTransformerAsync(this).ConfigureAwait(false);
            await TheUdpListener_Initializer.Initialize_TheUdpListenerAsync(this).ConfigureAwait(false);
            await ThePostgresRawPacketIngestor_Initializer.Initialize_ThePostgresRawPacketIngestorAsync(this).ConfigureAwait(false);
        }

        // Phase 3: Disposal (optional).
        // Emitted only for instances that require cleanup.
        public void DisposeAll()
        {
        }
    }
}
