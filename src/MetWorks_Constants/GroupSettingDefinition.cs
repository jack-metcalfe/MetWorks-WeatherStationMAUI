namespace MetWorks.Constants;
public record GroupSettingDefinition
{
    public string BasePath { get; }
    public string[] SettingNames { get; }
    public string BuildPath(string settingName)
        => $"/services/{BasePath}/{settingName}";
    /// <summary>
    /// Build the canonical group prefix path for this setting group (e.g. "/services/unitOfMeasure").
    /// Use this when registering prefix-based handlers.
    /// </summary>
    public string BuildGroupPath()
        => $"/services/{BasePath}";
    public GroupSettingDefinition(
        string basePath,
        string[] settingNames
    )
    {
        BasePath = basePath;
        SettingNames = settingNames;
    }
}
