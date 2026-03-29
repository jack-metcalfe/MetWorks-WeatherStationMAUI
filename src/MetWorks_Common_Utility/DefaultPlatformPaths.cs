namespace MetWorks.Common.Utility;
public sealed class DefaultPlatformPaths : IPlatformPaths
{
    public string AppDataDirectory
    {
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MetWorks-WeatherStationMAUI"
            );
        }
    }
}
