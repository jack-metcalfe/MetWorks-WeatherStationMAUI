using YamlDotNet.Serialization;
namespace Settings;
internal class SettingsConfiguration
{
    [YamlMember(Alias = "settings")]
    public List<SettingConfiguration> SettingConfigurations { get; init; }
}

