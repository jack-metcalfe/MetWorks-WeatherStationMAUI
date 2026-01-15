// Template:            Registry
// Version:             1.1
// Template Requested:  Registry
// Template:            File.Header
// Version:             1.1
// Template Requested:  Registry
// Generated On:        2026-01-15T03:06:48.6676207Z
#nullable enable
using System.Threading.Tasks;

namespace ServiceRegistry
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
            TheLoggerFile_InstanceFactory.Create(this);
            TheProvenanceTracker_InstanceFactory.Create(this);
            TheUnitsOfMeasureInitializer_InstanceFactory.Create(this);
            TheWeatherDataTransformer_InstanceFactory.Create(this);
            TheUdpListener_InstanceFactory.Create(this);
            TheRawPacketRecordTypedInPostgresOut_InstanceFactory.Create(this);
        }

        // Phase 2: Initialization (async, potentially slow).
        // Only instances with assignments require async initialization.
        // Element-driven instances are fully constructed during creation.
        public async Task InitializeAllAsync()
        {
            await TheSettingProvider_Initializer.Initialize_TheSettingProviderAsync(this);
            await TheSettingRepository_Initializer.Initialize_TheSettingRepositoryAsync(this);
            await TheLoggerFile_Initializer.Initialize_TheLoggerFileAsync(this);
            await TheProvenanceTracker_Initializer.Initialize_TheProvenanceTrackerAsync(this);
            await TheUnitsOfMeasureInitializer_Initializer.Initialize_TheUnitsOfMeasureInitializerAsync(this);
            await TheWeatherDataTransformer_Initializer.Initialize_TheWeatherDataTransformerAsync(this);
            await TheUdpListener_Initializer.Initialize_TheUdpListenerAsync(this);
            await TheRawPacketRecordTypedInPostgresOut_Initializer.Initialize_TheRawPacketRecordTypedInPostgresOutAsync(this);
        }

        // Phase 3: Disposal (optional).
        // Emitted only for instances that require cleanup.
        public void DisposeAll()
        {
        }
    }
}