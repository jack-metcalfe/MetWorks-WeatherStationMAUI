namespace InterfaceDefinition;
public interface ISettingsRepository : IRegistryExport
{
    string? GetValueOrDefault(string path);
    public void OnSettingsChanged(string pathPrefix, Action<string, string?, string?> handler);
}
