namespace InterfaceDefinition;
public interface IExperimental
{
    Task<bool> AddSettingsOverrideProvider_DebugHack(ISettingOverridesProvider? iSettingsOverrideProvider = null);
}
