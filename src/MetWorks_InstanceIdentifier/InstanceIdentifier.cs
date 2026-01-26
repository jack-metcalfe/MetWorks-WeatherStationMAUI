namespace MetWorks.InstanceIdentifier;
public partial class InstanceIdentifier : IInstanceIdentifier
{
    // Build canonical setting path from the GroupSettingDefinition to avoid hard-coded strings.
    private static string Path => LookupDictionaries.InstanceGroupSettingsDefinition.BuildSettingPath(SettingConstants.Instance_installationId);
    private ISettingProvider? _settingProvider;
    private ILogger? _logger;
    private string? _cached;

    // Parameterless constructor for DDI creation
    public InstanceIdentifier() { }

    // Declarative DI initialize signature
    public async Task<bool> InitializeAsync(
        ILogger iLogger, 
        ISettingProvider iSettingProvider
    )
    {
        _logger = iLogger ?? throw new ArgumentNullException(nameof(iLogger));
        _settingProvider = iSettingProvider ?? throw new ArgumentNullException(nameof(iSettingProvider));

        // Ensure provider is initialized - if not, we can't read values; just return true if set
        try
        {
            // Attempt to warm read; provider may be already initialized by generated code
            if (_settingProvider.ISettingValueDictionary.TryGetValue(Path, out var val) && !string.IsNullOrWhiteSpace(val.Value))
            {
                _cached = val.Value;
            }
            else if (_settingProvider.ISettingDefinitionDictionary.TryGetValue(Path, out var def) && !string.IsNullOrWhiteSpace(def.DefaultValue))
            {
                _cached = def.DefaultValue;
            }
            return await Task.FromResult(true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error("InstanceIdentifier failed to initialize.", ex);
            return false;
        }
    }
    public string GetOrCreateInstallationId()
    {
        if (!string.IsNullOrEmpty(_cached)) return _cached!;
        if (_settingProvider is null) throw new InvalidOperationException("InstanceIdentifier not initialized");
        if (_logger is null) throw new InvalidOperationException("InstanceIdentifier not initialized");

        try
        {
            if (_settingProvider.ISettingValueDictionary.TryGetValue(Path, out var val) && !string.IsNullOrWhiteSpace(val.Value))
            {
                _cached = val.Value;
                return _cached!;
            }

            if (_settingProvider.ISettingDefinitionDictionary.TryGetValue(Path, out var def) && !string.IsNullOrWhiteSpace(def.DefaultValue))
            {
                _cached = def.DefaultValue;
                return _cached!;
            }

            var guid = Guid.NewGuid().ToString();
            var saved = _settingProvider.SaveValueOverride(Path, guid);
            if (!saved)
            {
                _logger.Warning("Failed to persist installationId override, continuing with transient value.");
            }

            _cached = guid;
            return _cached!;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to obtain or create installation id.", ex);
            _cached = Guid.NewGuid().ToString();
            return _cached!;
        }
    }

    public bool SetInstallationId(string installationId)
    {
        if (string.IsNullOrWhiteSpace(installationId)) throw new ArgumentException("installationId must be non-empty", nameof(installationId));
        if (_settingProvider is null) throw new InvalidOperationException("InstanceIdentifier not initialized");
        var saved = _settingProvider.SaveValueOverride(Path, installationId);
        if (saved) _cached = installationId;
        return saved;
    }

    public bool ResetInstallationId()
    {
        if (_settingProvider is null) throw new InvalidOperationException("InstanceIdentifier not initialized");
        _cached = null;
        return _settingProvider.SaveValueOverride(Path, "");
    }
}
