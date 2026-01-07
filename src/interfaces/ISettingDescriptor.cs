namespace InterfaceDefinition;
public interface ISettingDescriptor
{
    public string Group { get; }
    public string Path { get; }
    public string? Value { get; set; }
    public string? Default { get; }
}
