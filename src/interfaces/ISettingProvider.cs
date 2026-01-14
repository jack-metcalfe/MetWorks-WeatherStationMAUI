namespace Interfaces;
public interface ISettingProvider
{
    Dictionary<string, ISettingDefinition> SettingDefinitions { get; }
    Dictionary<string, ISettingValue> SettingValues { get; }
}