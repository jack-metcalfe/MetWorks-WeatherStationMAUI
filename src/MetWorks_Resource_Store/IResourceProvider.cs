namespace MetWorks.Resource.Store;
public interface IResourceProvider
{
    static Stream? GetStream(string path) => ResourceProvider.GetStream(path);
    static string? GetString(string path) => ResourceProvider.GetString(path);
}
