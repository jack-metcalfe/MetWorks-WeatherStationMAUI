namespace StaticDataStore;
public static class StaticData
{
    public static Stream? GetStream(string path)
    {
        var assembly = typeof(StaticData).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(path.Replace("/", "."), StringComparison.OrdinalIgnoreCase));

        return resourceName != null ? assembly.GetManifestResourceStream(resourceName) : null;
    }
    public static string? GetString(string path)
    {
        using var stream = GetStream(path);
        if (stream == null) return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
