namespace Utility;
public class FileStringProvider : IStringProvider
{
    private readonly string _baseFolder;

    public FileStringProvider(string baseFolder) => _baseFolder = baseFolder;

    public async Task<string?> GetAsync(string location)
    {
        string path = Path.Combine(_baseFolder, location);
        return File.Exists(path) ? await File.ReadAllTextAsync(path) : null;
    }
}

