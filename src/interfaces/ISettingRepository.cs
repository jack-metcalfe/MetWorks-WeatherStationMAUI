namespace MetWorks.Interfaces;
public interface ISettingRepository
{
    string? GetValueOrDefault(string path);
    T GetValueOrDefault<T>(string path);
    IEnumerable<ISettingDefinition> GetAllDefinitions();
    IEnumerable<ISettingValue> GetAllValues();
    void RegisterForSettingChangeMessages(string path, Action<ISettingValue> handler);
    IEventRelayPath IEventRelayPath { get; }
}
