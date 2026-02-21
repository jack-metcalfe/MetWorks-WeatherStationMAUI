using MetWorks.EventRelay;
using MetWorks.Interfaces;

namespace MetWorks.Ingest.Transformer.Tests.Fakes;

sealed class InMemorySettingRepository : ISettingRepository
{
    readonly Dictionary<string, string> _values;
    readonly EventRelayPath _path = new();

    public InMemorySettingRepository(Dictionary<string, string>? initial = null)
    {
        _values = initial is not null
            ? new Dictionary<string, string>(initial, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public string? GetValueOrDefault(string path)
        => _values.TryGetValue(path, out var v) ? v : null;

    public T GetValueOrDefault<T>(string path)
    {
        var s = GetValueOrDefault(path);
        if (s is null)
            return default!;

        return (T)Convert.ChangeType(s, typeof(T));
    }

    public IEnumerable<ISettingDefinition> GetAllDefinitions()
        => Enumerable.Empty<ISettingDefinition>();

    public IEnumerable<ISettingValue> GetAllValues()
        => Enumerable.Empty<ISettingValue>();

    public bool ApplyOverrides(IEnumerable<ISettingValue> overrides)
        => throw new NotSupportedException("Not used in this test fake.");

    public void RegisterForSettingChangeMessages(string path, Action<ISettingValue> handler)
        => _path.Register(path, handler);

    public IEventRelayPath IEventRelayPath => _path;
}
