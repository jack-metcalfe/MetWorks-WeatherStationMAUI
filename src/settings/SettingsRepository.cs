using System.Collections.Concurrent;

namespace Settings;

public class SettingsRepository : ISettingsRepository
{
    private static List<ISettingOverridesProvider> _overridesProviders = new();

    // ========================================
    // NEW: Event Infrastructure
    // ========================================
    /// <summary>
    /// Thread-safe storage for setting change event handlers.
    /// Key: Setting path (exact match or wildcard prefix)
    /// Value: List of handlers (path, oldValue, newValue) => void
    /// </summary>
    private static readonly ConcurrentDictionary<string, List<Action<string, string?, string?>>>
        _settingChangeHandlers = new();

    IFileLogger? IFileLogger { get; set; }
    IFileLogger IFileLoggerSafe => NullPropertyGuard.GetSafeClass(
        IFileLogger, "Listener not initialized. Call InitializeAsync before using.");

    static Dictionary<string, ISettingDescriptor> ISettingDescriptorsDictionarySingletonSafe { get; set; } = new();

    enum InitializationStateEnum
    {
        NotReady,
        Ready
    }

    InitializationStateEnum InitializationState = InitializationStateEnum.NotReady;

    public SettingsRepository()
    {
    }

    public async Task<bool> InitializeAsync(
        IFileLogger iFileLogger,
        SettingConfiguration[] settingConfigurations,
        ISettingOverridesProvider iSettingOverridesProvider
    )
    {
        if (InitializationState == InitializationStateEnum.Ready)
            throw new InvalidOperationException("SettingsRepository is already initialized.");

        if (iFileLogger is null)
            throw new ArgumentNullException(nameof(iFileLogger), "File logger cannot be null.");

        if (settingConfigurations is null)
            throw new ArgumentNullException(nameof(settingConfigurations), "Setting configurations cannot be null.");

        if (settingConfigurations is null || settingConfigurations.Length == 0)
            throw new ArgumentNullException(nameof(settingConfigurations), "Setting configurations cannot be null or empty.");

        if (iSettingOverridesProvider is null)
            throw new ArgumentNullException(nameof(iSettingOverridesProvider), "Overrides provider cannot be null.");

        _overridesProviders.Add(iSettingOverridesProvider);

        IFileLogger = iFileLogger;
        try
        {
            foreach (var settingConfiguration in settingConfigurations)
            {
                ISettingDescriptorsDictionarySingletonSafe[settingConfiguration.Path]
                    = await SettingDescriptor.RegisterAsync(settingConfiguration);
            }
            InitializationState = InitializationStateEnum.Ready;

            // Do not buffer so can change while running
            // ToDo: Will need a locking mechanism to stop reading until edit is finished
            return await ApplyOverrides();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Failed to read settings configuration from the provided path.", exception);
        }
    }

    // ========================================
    // NEW: Event Subscription Methods
    // ========================================

    /// <summary>
    /// Subscribe to changes for a specific setting path (exact match).
    /// </summary>
    /// <param name="settingPath">Exact setting path (e.g., "/services/udp/unitOverrides/temperature")</param>
    /// <param name="handler">Callback: (path, oldValue, newValue) => void</param>
    /// <example>
    /// repo.OnSettingChanged("/services/udp/unitOverrides/temperature", (path, old, new) => 
    /// {
    ///     Console.WriteLine($"Temperature unit changed: {old} -> {new}");
    /// });
    /// </example>
    public void OnSettingChanged(string settingPath, Action<string, string?, string?> handler)
    {
        if (string.IsNullOrWhiteSpace(settingPath))
            throw new ArgumentException("Setting path cannot be null or whitespace.", nameof(settingPath));

        if (handler is null)
            throw new ArgumentNullException(nameof(handler), "Handler cannot be null.");

        _settingChangeHandlers.AddOrUpdate(
            settingPath,
            _ => new List<Action<string, string?, string?>> { handler },
            (_, existingList) =>
            {
                lock (existingList)
                {
                    existingList.Add(handler);
                }
                return existingList;
            });

        IFileLoggerSafe.Debug($"Registered handler for setting: {settingPath}");
    }

    /// <summary>
    /// Subscribe to changes for all settings matching a path prefix (wildcard).
    /// </summary>
    /// <param name="pathPrefix">Setting path prefix (e.g., "/services/udp/unitOverrides")</param>
    /// <param name="handler">Callback: (path, oldValue, newValue) => void</param>
    /// <example>
    /// // Subscribe to all unit override changes
    /// repo.OnSettingsChanged("/services/udp/unitOverrides", (path, old, new) => 
    /// {
    ///     Console.WriteLine($"Any unit override changed: {path}");
    /// });
    /// </example>
    public void OnSettingsChanged(string pathPrefix, Action<string, string?, string?> handler)
    {
        if (string.IsNullOrWhiteSpace(pathPrefix))
            throw new ArgumentException("Path prefix cannot be null or whitespace.", nameof(pathPrefix));

        if (handler is null)
            throw new ArgumentNullException(nameof(handler), "Handler cannot be null.");

        // Use a wildcard marker to distinguish prefix subscriptions from exact matches
        var wildcardKey = $"{pathPrefix}/*";

        _settingChangeHandlers.AddOrUpdate(
            wildcardKey,
            _ => new List<Action<string, string?, string?>> { handler },
            (_, existingList) =>
            {
                lock (existingList)
                {
                    existingList.Add(handler);
                }
                return existingList;
            });

        IFileLoggerSafe.Debug($"Registered wildcard handler for settings: {pathPrefix}/*");
    }

    // ========================================
    // ENHANCED: ApplyOverrides with Change Tracking
    // ========================================

    async Task<bool> ApplyOverrides()
    {
        if (InitializationState != InitializationStateEnum.Ready)
            throw new InvalidOperationException("SettingsRepository must be initialized before applying overrides.");

        if (_overridesProviders.IsNullOrEmpty())
            return await Task.FromResult(true);

        try
        {
            // Track all changes: path -> (oldValue, newValue)
            var changedSettings = new Dictionary<string, (string? oldValue, string? newValue)>();

            foreach (var overrideProvider in _overridesProviders)
            {
                var overridesModel = await overrideProvider.LoadAsync();
                var overrideSettingsDictionary = overridesModel.Settings;

                foreach (var overrideSettingsEntry in overrideSettingsDictionary)
                {
                    if (ISettingDescriptorsDictionarySingletonSafe.ContainsKey(overrideSettingsEntry.Key))
                    {
                        // Existing setting - update if changed
                        var descriptor = ISettingDescriptorsDictionarySingletonSafe[overrideSettingsEntry.Key];
                        var oldValue = descriptor.Value;
                        var newValue = overrideSettingsEntry.Value;

                        // Only track if value actually changed
                        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                        {
                            changedSettings[overrideSettingsEntry.Key] = (oldValue, newValue);
                            descriptor.Value = newValue;
                        }
                    }
                    else
                    {
                        // New setting - add to dictionary
                        var newConfiguration = new SettingConfiguration(
                            path: overrideSettingsEntry.Key,
                            defaultValue: overrideSettingsEntry.Value,
                            description: $"Dynamic setting added from overrides: {overrideSettingsEntry.Key}",
                            expectedValueType: "String",
                            group: "DynamicOverrides",
                            isEditable: true,
                            enumValues: Array.Empty<string>(),
                            isSecret: false
                        );

                        var newDescriptor = await SettingDescriptor.RegisterAsync(newConfiguration);
                        ISettingDescriptorsDictionarySingletonSafe[overrideSettingsEntry.Key] = newDescriptor;

                        // Track as a change (from null to new value)
                        changedSettings[overrideSettingsEntry.Key] = (null, overrideSettingsEntry.Value);

                        IFileLoggerSafe.Information($"Added new dynamic setting: {overrideSettingsEntry.Key} = '{overrideSettingsEntry.Value}'");
                    }
                }
            }

            // Notify subscribers of changes (if any)
            if (changedSettings.Count > 0)
            {
                NotifySettingChanges(changedSettings);
            }

            return await Task.FromResult(true);
        }
        catch (Exception exception)
        {
            throw IFileLoggerSafe.LogExceptionAndReturn(exception, "Failed to apply overrides to settings.");
        }
    }

    // ========================================
    // NEW: Notification Method
    // ========================================

    /// <summary>
    /// Notifies all registered handlers of setting changes.
    /// Supports both exact match and wildcard (prefix/*) subscriptions.
    /// </summary>
    /// <param name="changedSettings">Dictionary of path -> (oldValue, newValue)</param>
    private void NotifySettingChanges(Dictionary<string, (string? oldValue, string? newValue)> changedSettings)
    {
        foreach (var change in changedSettings)
        {
            var path = change.Key;
            var (oldValue, newValue) = change.Value;

            IFileLoggerSafe.Debug($"Setting changed: {path} = '{oldValue}' -> '{newValue}'");

            // Find all matching handlers (exact + wildcard)
            var handlersToInvoke = new List<Action<string, string?, string?>>();

            // 1. Exact match handlers
            if (_settingChangeHandlers.TryGetValue(path, out var exactHandlers))
            {
                lock (exactHandlers)
                {
                    handlersToInvoke.AddRange(exactHandlers);
                }
            }

            // 2. Wildcard handlers (check all wildcard keys)
            foreach (var kvp in _settingChangeHandlers)
            {
                if (kvp.Key.EndsWith("/*"))
                {
                    var prefix = kvp.Key.Substring(0, kvp.Key.Length - 2); // Remove "/*"
                    if (path.StartsWith(prefix))
                    {
                        lock (kvp.Value)
                        {
                            handlersToInvoke.AddRange(kvp.Value);
                        }
                    }
                }
            }

            // 3. Invoke all matching handlers (exception isolation)
            foreach (var handler in handlersToInvoke)
            {
                try
                {
                    handler(path, oldValue, newValue);
                }
                catch (Exception ex)
                {
                    // Isolate handler exceptions - don't let one handler break others
                    IFileLoggerSafe.Error($"Exception in settings change handler for '{path}': {ex.Message}");
                    IFileLoggerSafe.Debug($"Handler exception stack trace: {ex.StackTrace}");
                }
            }
        }
    }

    // ========================================
    // Existing Methods (unchanged)
    // ========================================

    public ISettingDescriptor? Get(string path) => ISettingDescriptorsDictionarySingletonSafe?[path];

    public bool TryGet(string path, out ISettingDescriptor? iSetting)
    {
        iSetting = Get(path);
        return iSetting is not null;
    }

    public string? GetValueOrDefault(string path)
    {
        var iSetting = Get(path);
        return iSetting is null ? null : iSetting.Value ?? iSetting.Default;
    }

    public bool TryGetValueOrDefault(string path, out string? value)
    {
        value = GetValueOrDefault(path);
        return value is not null;
    }
}