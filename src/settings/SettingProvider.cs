namespace Settings;

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
    ISettingDefinitionDictionary? _iSettingDefinitions;
    public ISettingDefinitionDictionary SettingDefinitions
    {
        get => NullPropertyGuard.Get(
            _isInitialized,
            _iSettingDefinitions,
            nameof(SettingDefinitions)
        );
        private set => _iSettingDefinitions = value;
    }

    ISettingValueDictionary? _iSettingValues;
    public ISettingValueDictionary SettingValues
    {
        get => NullPropertyGuard.Get(
            _isInitialized,
            _iSettingValues,
            nameof(SettingValues)
        );
        private set => _iSettingValues = value;
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
            if (settingModel != null)
            {
                SettingDefinitions = settingModel.Definitions.ToDictionary(def => def.Path, def => (ISettingDefinition)def);
                SettingValues = settingModel.Values.ToDictionary(val => val.Path, val => (ISettingValue)val);
                _isInitialized = true;
            }
        }

        catch(Exception exception)
        {
            ILogger.Error("Failed to initialize SettingProvider.", exception);
        }

        return _isInitialized;
    }
    public SettingModel? Load()
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var settingModelString = NullPropertyGuard
                .Get(true, StaticData.GetString(Filename), Filename);

            var settingModel = deserializer.Deserialize<SettingModel>(settingModelString);
            return settingModel;
        }
        catch (Exception exception)
        {
            ILogger.Error("Failed to load override from AppData.", exception); 
            return null;
        }
    }
}