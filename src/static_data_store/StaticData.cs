namespace StaticDataStore;
internal static class StaticData
{
    public static Stream? GetResourceAsStream(string path)
    {
        var assembly = typeof(StaticData).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(path.Replace("/", "."), StringComparison.OrdinalIgnoreCase));

        return resourceName != null ? assembly.GetManifestResourceStream(resourceName) : null;
    }
    public static string? GetResourceAsString(string path)
    {
        using var stream = GetResourceAsStream(path);
        if (stream == null) return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
