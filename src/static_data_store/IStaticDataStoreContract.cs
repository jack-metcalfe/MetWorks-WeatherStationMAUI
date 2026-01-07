namespace StaticDataStore;
public interface IStaticDataStoreContract
{
    static Stream? GetResourceAsStream(string path) => StaticData.GetResourceAsStream(path);
    static string? GetResourceAsString(string path) => StaticData.GetResourceAsString(path);
}
