namespace MetWorks.Resource.Store;
public static class ResourceProvider
{
    public static Stream? GetStream(string path)
    {
        var assembly = typeof(ResourceProvider).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(path.Replace("/", "."), StringComparison.OrdinalIgnoreCase));

        return resourceName != null ? assembly.GetManifestResourceStream(resourceName) : null;
    }
    public static string? GetString(string path)
    {
        using var stream = GetStream(path);
        if (stream is null) return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
