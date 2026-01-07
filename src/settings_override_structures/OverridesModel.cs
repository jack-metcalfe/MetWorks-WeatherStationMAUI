namespace SettingsOverrideStructures;
public record OverridesModel : IOverridesModel
{
    public Dictionary<string, string> Settings { get; init; } = new();
    public Dictionary<string, string> Secrets { get; init; } = new();
    public Dictionary<string, string> Routing { get; init; } = new();
    public OverridesMetadata Metadata { get; init; } = new();
}