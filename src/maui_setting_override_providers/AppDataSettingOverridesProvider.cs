namespace MauiSettingOverridesProviders;
public class AppDataSettingOverridesProvider : ISettingOverridesProvider
{
    private string? FileName { get; set; }
    private string FileNameSafe => NullPropertyGuard.GetSafeClass(
        FileName, @"FileName not initialized. Call InitializeAsync before using."
    );
    public AppDataSettingOverridesProvider()
    {
    }
    public async Task<bool> InitializeAsync(string? filename)
    {
        FileName = filename ?? @"overrides.yaml";
        return await Task.FromResult(true);
    }
    public async Task<IOverridesModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, FileNameSafe);
            if (!File.Exists(path))
                throw new FileNotFoundException("Overrides file not found", path);

            var yaml = File.ReadAllText(path);
            // Ensure we have access to the file
            // Was hanging here so went non-async for now because could be debugging context issue
            // = await File.ReadAllTextAsync(path, cancellationToken);
            return await Task.FromResult(ParseYaml(yaml));
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Failed to load overrides from AppData.", exception);
        }
    }
    public static IOverridesModel ParseYaml(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<OverridesModel>(yaml);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Failed to parse overrides YAML.", exception);
        }
    }
}