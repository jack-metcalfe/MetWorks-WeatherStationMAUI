namespace MetWorks.Persistence.SQLite.Paths;
public sealed class DefaultPlatformPaths : IPlatformPaths
{
    public string AppDataDirectory { get; }

    public DefaultPlatformPaths()
    {
        AppDataDirectory = ResolveAppDataDirectory();
    }

    static string ResolveAppDataDirectory()
    {
#if MAUI
        try
        {
            return FileSystem.AppDataDirectory;
        }
        catch
        {
        }
#endif

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(local))
        {
            return Path.Combine(local, "MetWorks-WeatherStationMAUI");
        }

        return Path.Combine(Path.GetTempPath(), "MetWorks-WeatherStationMAUI");
    }
}
