namespace SettingsOverrideStructures;
public record OverridesMetadata : IOverridesMetadata
{
    public bool BufferingAllowed { get; init; } = true;
    public bool LockingSupported { get; init; } = false;
}
