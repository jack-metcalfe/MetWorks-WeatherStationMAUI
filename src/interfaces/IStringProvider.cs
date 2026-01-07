namespace InterfaceDefinition;
public interface IStringProvider
{
    Task<string?> GetAsync(string key);
}

