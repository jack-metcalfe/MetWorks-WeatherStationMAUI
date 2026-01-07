namespace Utility;
public class ResourceFileProvider : IStringProvider
{
    private readonly Assembly _assembly;

    public ResourceFileProvider(Assembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    public async Task<string?> GetAsync(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return null;

        using Stream? stream = _assembly.GetManifestResourceStream(location);
        if (stream == null)
            return null;

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
