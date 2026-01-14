namespace Interfaces;
public interface IEventRelayPath
{
    void Register(string path, Action<ISettingValue> handler);
    void Unregister(string path, Action<ISettingValue> handler);
    void Send(ISettingValue settingValue);
}
