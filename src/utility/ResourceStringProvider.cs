namespace Utility;
public class ResourceStringProvider : IStringProvider
{
    Assembly? Assembly { get; set; }
    Assembly AssemblySafe => NullPropertyGuard.GetSafeClass(
        Assembly, "ResourceStringProvider not initialized. Call InitializeAsync before using.");
    ResourceStringProvider() { }
    async Task<bool> InitializeAsync(Assembly assembly)
    {
        Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        return await Task.FromResult(true);
    }
    public async Task<string?> GetAsync(string location)
    {
        using var stream = AssemblySafe.GetManifestResourceStream(location);
        if (stream == null) return null;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

