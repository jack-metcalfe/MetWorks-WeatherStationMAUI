namespace Settings;

/// <summary>
/// Immutable schema/metadata for a setting.
/// Used for validation, UI generation, and documentation.
/// </summary>
public record SettingDefinition : ISettingDefinition
{
    public required string Path { get; init; }
    public required string DefaultValue { get; init; }
    public required string Description { get; init; }
    public required string ExpectedValueType { get; init; }
    public string[]? AllowableValues { get; init; }
    public bool IsEditable { get; init; } = true;
    public bool IsSecret { get; init; } = false;
}