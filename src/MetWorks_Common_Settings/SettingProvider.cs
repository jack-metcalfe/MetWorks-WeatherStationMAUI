namespace MetWorks.Common.Settings;
using ISettingDefinitionDictionary = Dictionary<string, ISettingDefinition>;
using ISettingValueDictionary = Dictionary<string, ISettingValue>;
public class SettingProvider : ISettingProvider
{
    private readonly string? _overridesBaseDirectory;
    public string? SettingsTemplateResourceName { get; private set; }
    public string? SettingsOverrideFilePath { get; private set; }
    public bool SettingsOverrideFileExistsAtLoad { get; private set; }
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

    // Constructor used for testing to inject a specific base directory for overrides
    public SettingProvider(string? overridesBaseDirectory)
    {
        _overridesBaseDirectory = overridesBaseDirectory;
    }
    public async Task<bool> InitializeAsync(
        ILoggerStub iLoggerStub
    )
    {
        try
        {
            ILogger = iLoggerStub;
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
            // Prefer an embedded template resource as the canonical source of definitions,
            // then overlay any local overrides found in AppData so overrides only replace values.
            var localDir = _overridesBaseDirectory ?? GetAppDataDirectory();
            var overridePath = Path.Combine(localDir ?? string.Empty, SettingConstants.ProviderFilename);
            SettingsOverrideFilePath = overridePath;
            SettingsOverrideFileExistsAtLoad = !string.IsNullOrWhiteSpace(localDir) && File.Exists(overridePath);

            // Load template from embedded resources first (preferred).
            SettingsTemplateResourceName = SettingConstants.ProviderFilename;
            var templateString = ResourceProvider.GetString(SettingsTemplateResourceName);

            SettingModel templateModel = templateString is null
                ? new SettingModel()
                : deserializer.Deserialize<SettingModel>(templateString) ?? new SettingModel();

            // If overrides exist, read and merge values over the template model
            if (!string.IsNullOrWhiteSpace(localDir) && File.Exists(overridePath))
            {
                try
                {
                    var existing = File.ReadAllText(overridePath);
                    var overrideModel = deserializer.Deserialize<SettingModel>(existing) ?? new SettingModel();

                    // Merge definitions: add any missing definitions from overrides
                    foreach (var def in overrideModel.Definitions)
                    {
                        if (!templateModel.Definitions.Any(d => d.Path == def.Path))
                            templateModel.Definitions.Add(def);
                    }

                    // Merge values: overrides replace or add values
                    foreach (var val in overrideModel.Values)
                    {
                        var found = templateModel.Values.FirstOrDefault(v => v.Path == val.Path);
                        if (found is not null)
                        {
                            found.Value = val.Value;
                        }
                        else
                        {
                            templateModel.Values.Add(new SettingValue { Path = val.Path, Value = val.Value });
                        }
                    }

                    return templateModel;
                }
                catch (Exception ex)
                {
                    // If override file can't be read/deserialized, log and fall back to template
                    ILogger?.Warning($"Failed to read overrides file '{overridePath}', using embedded template: {ex.Message}");
                    return templateModel;
                }
            }

            // No overrides - return template (may be empty if no embedded resource found)
            return templateModel;
        }
        catch (Exception exception)
        {
            ILogger.Error("Failed to load settings model from overrides or embedded resource.", exception);
            return null;
        }
    }

    static string? GetAppDataDirectory()
    {
        // Prefer platform-specific AppData where available.
#if MAUI
        try
        {
            return FileSystem.AppDataDirectory;
        }
        catch
        {
            // fall through to other options
        }
#endif
        try
        {
            var p = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(p)) return Path.Combine(p, "MetWorks-WeatherStationMAUI");
        }
        catch { /* ignore */ }

        try
        {
            var p = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrWhiteSpace(p)) return Path.Combine(p, "MetWorks-WeatherStationMAUI");
        }
        catch { /* ignore */ }

        // Last resort: temp directory
        try { return Path.Combine(Path.GetTempPath(), "MetWorks-WeatherStationMAUI"); } catch { return null; }
    }

    /// <summary>
    /// Persist a single value override to the LocalApplicationData overrides file.
    /// Creates the overrides file if it does not exist. This method is idempotent for the same path/value.
    /// </summary>
    public bool SaveValueOverride(string path, string value)
    {
        try
        {
            var localDir = _overridesBaseDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetWorks-WeatherStationMAUI");
            Directory.CreateDirectory(localDir);
            var overridePath = Path.Combine(localDir, SettingConstants.ProviderFilename);

            SettingModel model;
            if (File.Exists(overridePath))
            {
                var existing = File.ReadAllText(overridePath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                model = deserializer.Deserialize<SettingModel>(existing) ?? new SettingModel();
            }
            else
            {
                model = new SettingModel();
            }

            var existingVal = model.Values.FirstOrDefault(v => v.Path == path);
            if (existingVal is not null)
            {
                existingVal.Value = value;
            }
            else
            {
                model.Values.Add(new SettingValue { Path = path, Value = value });
            }

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(model);

            var tmp = overridePath + ".tmp";
            File.WriteAllText(tmp, yaml);
            File.Move(tmp, overridePath, true);
            return true;
        }
        catch (Exception ex)
        {
            ILogger.Error($"Failed to save settings override.", ex);
            return false;
        }
    }
}