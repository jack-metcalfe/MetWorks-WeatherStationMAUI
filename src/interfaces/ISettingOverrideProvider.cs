namespace InterfaceDefinition;
public interface ISettingOverrideProvider
{
    bool TryGetOverride(string path, out string? value);
}

