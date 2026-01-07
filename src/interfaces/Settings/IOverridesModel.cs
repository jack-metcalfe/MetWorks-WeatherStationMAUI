namespace InterfaceDefinition.Settings;
public interface IOverridesModel
{
    Dictionary<string, string> Settings { get; }
    Dictionary<string, string> Secrets { get; }
    Dictionary<string, string> Routing { get; }
}
