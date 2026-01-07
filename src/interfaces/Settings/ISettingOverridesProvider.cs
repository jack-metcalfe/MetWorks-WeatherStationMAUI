namespace InterfaceDefinition.Settings;
public interface ISettingOverridesProvider
{
    Task<IOverridesModel> LoadAsync(CancellationToken cancellationToken = default);
}
