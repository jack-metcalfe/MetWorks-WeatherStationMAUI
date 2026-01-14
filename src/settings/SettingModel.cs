using Interfaces;
namespace Settings;

public record SettingModel
{
    [YamlMember(Alias = "values")]
    public List<SettingValue> Values { get; set; } = new();
    public List<SettingDefinition> Definitions { get; set; } = new();
}