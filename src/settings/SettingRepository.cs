namespace MetWorks.Common.Settings;
using ISettingDefinitionDictionary = Dictionary<string, ISettingDefinition>;
using ISettingValueDictionary = Dictionary<string, ISettingValue>;
/// <summary>
/// Central repository for settings definition, value, and override.
/// Implements ISettingsRepository for DI integration.
/// </summary>
public class SettingRepository : ISettingRepository
{
    bool _isInitialized = false;

    ILogger? _iLogger = null;
    ILogger ILogger
    {
        get => NullPropertyGuard.Get(
            _isInitialized, _iLogger, nameof(ILogger)
        );
        set => _iLogger = value;
    }

    IEventRelayPath? _iEventRelayPath = null;
    public IEventRelayPath IEventRelayPath =>
        NullPropertyGuard.Get(
            _isInitialized, _iEventRelayPath, nameof(IEventRelayPath)
        );

    ISettingDefinitionDictionary ISettingDefinitionDictionary =>
        NullPropertyGuard.Get(
            _isInitialized, ISettingProvider.ISettingDefinitionDictionary, nameof(ISettingDefinitionDictionary)
        );

    ISettingValueDictionary ISettingValueDictionary =>
        NullPropertyGuard.Get(
            _isInitialized, ISettingProvider.ISettingValueDictionary, nameof(ISettingValueDictionary)
        );

    ISettingProvider? _iSettingProvider;
    ISettingProvider ISettingProvider
    {
        get => NullPropertyGuard.Get(
            _isInitialized,
            _iSettingProvider,
            nameof(ISettingProvider)
        );
    }

    public SettingRepository() { }

    /// <summary>
    /// Async initialization for DI. Accepts logger, definitions, and override provider.
    /// </summary>
    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingProvider iSettingProvider
    )
    {
        try
        {
            _iLogger = iLogger;
            _iSettingProvider = iSettingProvider;
            _iEventRelayPath = new EventRelayPath();
        }
        catch (Exception exception)
        {
            ILogger.Error("Failed to initialize", exception);
        }

        _isInitialized = true;
        return _isInitialized;
    }
    public void RegisterForSettingChangeMessages(string path, Action<ISettingValue> handler)
        => IEventRelayPath.Register(path, handler);
    public string? GetValueOrDefault(string path)
    {
        if (ISettingValueDictionary.ContainsKey(path)) return ISettingValueDictionary[path].Value;

        if (ISettingDefinitionDictionary.ContainsKey(path)) return ISettingDefinitionDictionary[path].DefaultValue;

        return null;
    }
    public IEnumerable<ISettingDefinition> GetAllDefinitions() => ISettingDefinitionDictionary.Values;
    public IEnumerable<ISettingValue> GetAllValues() => ISettingValueDictionary.Values;
    public T GetValueOrDefault<T>(string path)
    {
        var settingStringValue = GetValueOrDefault(path);
        if (string.IsNullOrEmpty(settingStringValue))
        {
            settingStringValue = NullPropertyGuard.Get(
                _isInitialized,
                ISettingDefinitionDictionary[path].DefaultValue,
                nameof(ISettingDefinitionDictionary)
            );
        }
        return (T)Convert.ChangeType(settingStringValue, typeof(T));
    }
}