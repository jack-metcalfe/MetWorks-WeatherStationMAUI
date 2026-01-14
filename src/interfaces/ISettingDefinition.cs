namespace Interfaces;

public interface ISettingDefinition
{
    string Path { get; }
    string DefaultValue { get; }
    string Description { get; }
    string ExpectedValueType { get; }
    string[]? AllowableValues { get; }
    bool IsEditable { get; }
    bool IsSecret { get; }
}