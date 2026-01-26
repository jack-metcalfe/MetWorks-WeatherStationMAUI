namespace MetWorks.Interfaces;
public interface ISettingProvider
{
    Dictionary<string, ISettingDefinition> ISettingDefinitionDictionary { get; }
    Dictionary<string, ISettingValue> ISettingValueDictionary { get; }
    bool SaveValueOverride(string path, string value);
}