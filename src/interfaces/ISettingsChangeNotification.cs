namespace Interfaces.Settings;

/// <summary>
/// Allows components to be notified when settings values change.
/// </summary>
public interface ISettingsChangeNotification
{
    /// <summary>
    /// Subscribe to changes for a specific setting path.
    /// </summary>
    void SubscribeToSetting(string settingPath, Action<string, string?> onChanged);
    
    /// <summary>
    /// Subscribe to changes for multiple setting paths.
    /// </summary>
    void SubscribeToSettings(string[] settingPaths, Action<string, string?> onChanged);
    
    /// <summary>
    /// Subscribe to all settings changes (use sparingly).
    /// </summary>
    void SubscribeToAll(Action<string, string?> onChanged);
    
    /// <summary>
    /// Unsubscribe from setting change notifications.
    /// </summary>
    void Unsubscribe(Action<string, string?> callback);
}