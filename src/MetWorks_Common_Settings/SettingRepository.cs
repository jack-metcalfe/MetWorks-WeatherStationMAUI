namespace MetWorks.Common.Settings;
using ISettingDefinitionDictionary = System.Collections.Generic.Dictionary<string, ISettingDefinition>;
using ISettingValueDictionary = System.Collections.Generic.Dictionary<string, ISettingValue>;
/// <summary>
/// Central repository for settings definition, value, and override.
/// Implements ISettingsRepository for DI integration.
/// </summary>
public class SettingRepository : ISettingRepository, MetWorks.Interfaces.IServiceReady
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

    // Readiness
    readonly TaskCompletionSource<bool> _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Ready => _readyTcs.Task;
    public bool IsReady => _readyTcs.Task.IsCompletedSuccessfully;

    // Internal thread-safe stores to allow concurrent reads while updates are applied atomically
    readonly ReaderWriterLockSlim _rw = new(LockRecursionPolicy.NoRecursion);
    readonly ConcurrentDictionary<string, ISettingDefinition> _definitions = new();
    readonly ConcurrentDictionary<string, ISettingValue> _values = new();

    ISettingDefinitionDictionary ISettingDefinitionDictionary =>
        // keep referencing provider for legacy callers; primary consumers should use repository methods
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
        ILoggerStub iLoggerStub,
        ISettingProvider iSettingProvider
    )
    {
        try
        {
            _iLogger = iLoggerStub;
            _iSettingProvider = iSettingProvider;
            _iEventRelayPath = new EventRelayPath();
        }
        catch (Exception exception)
        {
            ILogger.Error("Failed to initialize", exception);
        }

        // Populate internal thread-safe stores from provider snapshot
        try
        {
            _rw.EnterWriteLock();
            _definitions.Clear();
            _values.Clear();
            if (_iSettingProvider?.ISettingDefinitionDictionary is not null)
            {
                foreach (var kvp in _iSettingProvider.ISettingDefinitionDictionary)
                    _definitions[kvp.Key] = kvp.Value;
            }
            if (_iSettingProvider?.ISettingValueDictionary is not null)
            {
                foreach (var kvp in _iSettingProvider.ISettingValueDictionary)
                    _values[kvp.Key] = kvp.Value;
            }
        }
        finally
        {
            try { _rw.ExitWriteLock(); } catch { }
        }

        _isInitialized = true;
        // Signal readiness to consumers
        try { _readyTcs.TrySetResult(true); } catch { }
        return _isInitialized;
    }
    public void RegisterForSettingChangeMessages(string path, Action<ISettingValue> handler)
        => IEventRelayPath.Register(path, handler);
    public string? GetValueOrDefault(string path)
    {
        // Try read from internal store first
        if (string.IsNullOrEmpty(path)) return null;
        if (_values.TryGetValue(path, out var val)) return val.Value;

        if (_definitions.TryGetValue(path, out var def)) return def.DefaultValue;

        // Fallback to provider as a last resort
        try
        {
            if (ISettingValueDictionary.ContainsKey(path)) return ISettingValueDictionary[path].Value;
            if (ISettingDefinitionDictionary.ContainsKey(path)) return ISettingDefinitionDictionary[path].DefaultValue;
        }
        catch { }

        return null;
    }
    public IEnumerable<ISettingDefinition> GetAllDefinitions() => ISettingDefinitionDictionary.Values;
    public IEnumerable<ISettingValue> GetAllValues() => _values.Values.ToList();
    public T GetValueOrDefault<T>(string path)
    {
        var settingStringValue = GetValueOrDefault(path);
        if (string.IsNullOrEmpty(settingStringValue))
        {
            if (_definitions.TryGetValue(path, out var def)) settingStringValue = def.DefaultValue;
            else settingStringValue = NullPropertyGuard.Get(_isInitialized, ISettingDefinitionDictionary[path].DefaultValue, nameof(ISettingDefinitionDictionary));
        }
        return (T)Convert.ChangeType(settingStringValue, typeof(T));
    }

    /// <summary>
    /// Atomically apply overrides (persist via provider if available) and publish change messages for each updated value.
    /// </summary>
    public bool ApplyOverrides(IEnumerable<ISettingValue> overrides)
    {
        if (overrides == null) return false;
        var changed = new List<ISettingValue>();
        try
        {
            _rw.EnterWriteLock();
            foreach (var o in overrides)
            {
                if (o == null || string.IsNullOrEmpty(o.Path)) continue;
                _values[o.Path] = o;
                changed.Add(o);
                try { _iSettingProvider?.SaveValueOverride(o.Path, o.Value); } catch { }
            }
        }
        finally
        {
            try { _rw.ExitWriteLock(); } catch { }
        }

        // Publish events for changed values
        foreach (var c in changed)
        {
            try { IEventRelayPath.Send(c); } catch { }
        }

        return true;
    }
}