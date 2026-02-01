namespace MetWorks.Common;
public sealed class DefaultPlatformPaths : IPlatformPaths
{
    public string AppDataDirectory
    {
        get
        {
#if MAUI
            try { return FileSystem.AppDataDirectory; } catch { }
#endif
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MetWorks-WeatherStationMAUI"
            );
        }
    }
}
