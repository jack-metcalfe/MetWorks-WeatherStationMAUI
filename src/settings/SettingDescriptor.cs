using System.ComponentModel;
using Utility;
namespace Settings;

/// <summary>
/// Represents a declarative configuration setting with structured metadata for registration, discovery,
/// and override-aware resolution. Each descriptor defines:
/// - <c>Default</c>: The fallback value if no override is present.
/// - <c>Description</c>: Human-readable explanation.
/// - <c>Editable</c> / <c>Immutable</c>: Controls mutability and override behavior.
/// - <c>Enum</c>: Optional list of allowed values for enum-like settings.
/// - <c>EnumType</c>: Type hint for enum values.
/// - <c>ExpectedValueType</c>: The expected data type (e.g., "bool", "string", "int").
/// - <c>Group</c>: Optional grouping label.
/// - <c>IsSecret</c>: Indicates whether the setting contains sensitive data.
/// - <c>Path</c>: A unique identifier for the setting, often hierarchical.
/// - <c>Value</c>: The user-specified override, if any.
/// 
/// Supports layered configuration, dynamic discovery, and integration with UI, validation, and telemetry.
/// Implements <see cref="INotifyPropertyChanged"/> for reactive binding.
/// </summary>
internal class SettingDescriptor : ISettingDescriptor, INotifyPropertyChanged
{
    public string? Default { get; init; }
    public string? Description { get; init; }
    public bool? IsEditable { get; init; }
    public string[]? EnumValues { get; init; }
    public string? ExpectedValueType { get; init; } = "string";
    public string? Group { get; set; }
    public bool? IsSecret { get; init; } = false;
    public string? Path { get; init; }

    private string? _value;

    /// <summary>
    /// User-specified override value. Triggers change notifications and override status updates.
    /// </summary>
    public string? Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises a property change notification for reactive UI or binding systems.
    /// </summary>
    /// <param name="name">The name of the property that changed.</param>
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static async Task<ISettingDescriptor> RegisterAsync(SettingConfiguration settingConfiguration)
    {
        try
        {
            return new SettingDescriptor()
            {
                Path = settingConfiguration.Path,
                Default = settingConfiguration.Default,
                Description = settingConfiguration.Description,
                IsEditable = settingConfiguration.IsEditable,
                EnumValues = settingConfiguration.EnumValues,
                ExpectedValueType = settingConfiguration.ExpectedValueType ?? "string",
                Group = settingConfiguration.Group,
                IsSecret = settingConfiguration.IsSecret,
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to register setting from the provided configuration.", ex);
        }
    }
}
