namespace StaticDataStore;
public interface IStaticDataStore
{
    static Stream? GetStream(string path) => StaticData.GetStream(path);
    static string? GetString(string path) => StaticData.GetString(path);
}
