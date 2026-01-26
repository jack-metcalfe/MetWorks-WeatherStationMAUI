namespace MetWorks.Constants;
public record GroupSettingDefinition
{
    public string GroupName { get; }
    public string GroupBasePath { get; }
    public string[] SettingNames { get; }
    public string BuildSettingPath(string settingName)
        => $"/services/{GroupBasePath}/{settingName}";
    /// <summary>
    /// Build the canonical group prefix path for this setting group (e.g. "/services/unitOfMeasure").
    /// Use this when registering prefix-based handlers.
    /// </summary>
    public string BuildGroupPath()
        => $"/services/{GroupBasePath}";
    public GroupSettingDefinition(
        string groupName,
        string groupBasePath,
        string[] settingNames
    )
    {
        GroupName = groupName;
        GroupBasePath = groupBasePath;
        SettingNames = settingNames;
    }
}
