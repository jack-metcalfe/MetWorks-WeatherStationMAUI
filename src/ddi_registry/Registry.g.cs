// Template:            Registry
// Version:             1.1
// Template Requested:  Registry
// Template:            File.Header
// Version:             1.1
// Template Requested:  Registry
// Generated On:        2026-01-07T05:31:08.6249926Z
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
            TheFileLogger_InstanceFactory.Create(this);
            TheProvenanceTracker_InstanceFactory.Create(this);
            TheUdpPortSetting_InstanceFactory.Create(this);
            TheUdpPortSettings_InstanceFactory.Create(this);
            TemperatureOptions_InstanceFactory.Create(this);
            UdpTemperatureSetting_InstanceFactory.Create(this);
            WindspeedOptions_InstanceFactory.Create(this);
            UdpWindspeedSetting_InstanceFactory.Create(this);
            PressureOptions_InstanceFactory.Create(this);
            UdpPressureSetting_InstanceFactory.Create(this);
            PrecipitationOptions_InstanceFactory.Create(this);
            UdpPrecipitationSetting_InstanceFactory.Create(this);
            DistanceOptions_InstanceFactory.Create(this);
            UdpDistanceSetting_InstanceFactory.Create(this);
            TheUdpListenerSettings_InstanceFactory.Create(this);
            TheAppDataSettingOverridesProvider_InstanceFactory.Create(this);
            TheUDPSettingsRepository_InstanceFactory.Create(this);
            TheWeatherDataTransformer_InstanceFactory.Create(this);
            TheUdpListener_InstanceFactory.Create(this);
            ThePostgresConnection_InstanceFactory.Create(this);
            ThePostgresSettings_InstanceFactory.Create(this);
            ThePostgresSettingsRepository_InstanceFactory.Create(this);
            TheRawPacketRecordTypedInPostgresOut_InstanceFactory.Create(this);
        }

        // Phase 2: Initialization (async, potentially slow).
        // Only instances with assignments require async initialization.
        // Element-driven instances are fully constructed during creation.
        public async Task InitializeAllAsync()
        {
            await TheFileLogger_Initializer.Initialize_TheFileLoggerAsync(this);
            await TheProvenanceTracker_Initializer.Initialize_TheProvenanceTrackerAsync(this);
            await TheUdpPortSetting_Initializer.Initialize_TheUdpPortSettingAsync(this);
            await UdpTemperatureSetting_Initializer.Initialize_UdpTemperatureSettingAsync(this);
            await UdpWindspeedSetting_Initializer.Initialize_UdpWindspeedSettingAsync(this);
            await UdpPressureSetting_Initializer.Initialize_UdpPressureSettingAsync(this);
            await UdpPrecipitationSetting_Initializer.Initialize_UdpPrecipitationSettingAsync(this);
            await UdpDistanceSetting_Initializer.Initialize_UdpDistanceSettingAsync(this);
            await TheAppDataSettingOverridesProvider_Initializer.Initialize_TheAppDataSettingOverridesProviderAsync(this);
            await TheUDPSettingsRepository_Initializer.Initialize_TheUDPSettingsRepositoryAsync(this);
            await TheWeatherDataTransformer_Initializer.Initialize_TheWeatherDataTransformerAsync(this);
            await TheUdpListener_Initializer.Initialize_TheUdpListenerAsync(this);
            await ThePostgresConnection_Initializer.Initialize_ThePostgresConnectionAsync(this);
            await ThePostgresSettingsRepository_Initializer.Initialize_ThePostgresSettingsRepositoryAsync(this);
            await TheRawPacketRecordTypedInPostgresOut_Initializer.Initialize_TheRawPacketRecordTypedInPostgresOutAsync(this);
        }

        // Phase 3: Disposal (optional).
        // Emitted only for instances that require cleanup.
        public void DisposeAll()
        {
        }
    }
}