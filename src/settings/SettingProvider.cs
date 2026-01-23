namespace MetWorks.Common.Settings;
using static Constants.Settings.SettingProvider;
using ISettingDefinitionDictionary = Dictionary<string, ISettingDefinition>;
using ISettingValueDictionary = Dictionary<string, ISettingValue>;

public class SettingProvider : ISettingProvider
{
    bool _isInitialized = false;
    ILogger? _iLogger = null;
    ILogger ILogger
    {
        get => NullPropertyGuard.Get(_isInitialized, _iLogger, nameof(ILogger));
        set => _iLogger = value;
    }
    ISettingDefinitionDictionary? _iSettingDefinitionDictionary;
    public ISettingDefinitionDictionary ISettingDefinitionDictionary
    {
        get => NullPropertyGuard.Get(
            _isInitialized,
            _iSettingDefinitionDictionary,
            nameof(ISettingDefinitionDictionary)
        );
    }
    ISettingValueDictionary? _iSettingValueDictionary;
    public ISettingValueDictionary ISettingValueDictionary
    {
        get => NullPropertyGuard.Get(
            _isInitialized,
            _iSettingValueDictionary,
            nameof(ISettingValueDictionary)
        );
    }
    public SettingProvider() {}
    public async Task<bool> InitializeAsync(
        ILogger iLogger
    )
    {
        try
        {
            ILogger = iLogger;
            var settingModel = Load();
            if (settingModel is not null)
            {
                _iSettingDefinitionDictionary = settingModel.Definitions.ToDictionary(def => def.Path, def => (ISettingDefinition)def);
                _iSettingValueDictionary = settingModel.Values.ToDictionary(val => val.Path, val => (ISettingValue)val);

                foreach (var def in _iSettingDefinitionDictionary.Values)
                {
                    if (!_iSettingValueDictionary.ContainsKey(def.Path))
                    {
                        _iSettingValueDictionary[def.Path] = new SettingValue
                        {
                            Path = def.Path,
                            Value = def.DefaultValue
                        };
                    }
                }
                _isInitialized = true;
            }
        }
        catch(Exception exception)
        {
            ILogger.Error("Failed to initialize SettingProvider.", exception);
        }
        return _isInitialized;
    }
    SettingModel? Load()
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var settingModelString = NullPropertyGuard
                .Get(true, ResourceProvider.GetString(Filename), Filename);

            return deserializer.Deserialize<SettingModel>(settingModelString);
        }
        catch (Exception exception)
        {
            ILogger.Error("Failed to load override from AppData.", exception); 
            return null;
        }
    }
}