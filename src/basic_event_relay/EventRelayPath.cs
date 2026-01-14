namespace EventRelay;

public class EventRelayPath : IEventRelayPath
{
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly ConcurrentDictionary<string, List<Action<ISettingValue>>> _pathHandlers = new();
    public EventRelayPath() {}
    /// <summary>
    /// Register a handler for a specific path (full or partial).
    /// </summary>
    public void Register(string path, Action<ISettingValue> handler)
    {
        if (!_pathHandlers.TryGetValue(path, out var handlers))
        {
            handlers = new List<Action<ISettingValue>>();
            _pathHandlers[path] = handlers;
        }
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }

    /// <summary>
    /// Unregister a handler for a specific path.
    /// </summary>
    public void Unregister(string path, Action<ISettingValue> handler)
    {
        if (_pathHandlers.TryGetValue(path, out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }
    }

    /// <summary>
    /// Send an ISettingValue message to all handlers whose registered path is a prefix of the message's path.
    /// </summary>
    public void Send(ISettingValue settingValue)
    {
        var path = settingValue.Path;
        foreach (var kvp in _pathHandlers)
        {
            if (path.StartsWith(kvp.Key, StringComparison.Ordinal))
            {
                List<Action<ISettingValue>> handlersCopy;
                lock (kvp.Value)
                {
                    handlersCopy = new List<Action<ISettingValue>>(kvp.Value);
                }
                foreach (var handler in handlersCopy)
                {
                    handler(settingValue);
                }
            }
        }
    }
}