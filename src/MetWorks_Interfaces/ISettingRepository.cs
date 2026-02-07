namespace MetWorks.Interfaces;
public interface ISettingRepository
{
    string? GetValueOrDefault(string path);
    T GetValueOrDefault<T>(string path);
    IEnumerable<ISettingDefinition> GetAllDefinitions();
    IEnumerable<ISettingValue> GetAllValues();
    /// <summary>
    /// Apply one or more setting value overrides.
    /// Implementations should persist overrides to local storage when available and may emit change notifications.
    /// </summary>
    bool ApplyOverrides(IEnumerable<ISettingValue> overrides);
    void RegisterForSettingChangeMessages(string path, Action<ISettingValue> handler);
    IEventRelayPath IEventRelayPath { get; }
}
