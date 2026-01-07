namespace InterfaceDefinition;
public interface IBackgroundService : IRegistryExport
{
    Task<bool> StartAsync();
}
