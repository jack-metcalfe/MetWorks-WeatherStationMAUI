namespace Interfaces;
public interface ISettingModel
{
    List<ISettingValue> Values { get; set; }
    List<ISettingDefinition> Definitions { get; set; }
}