namespace MetWorks.Common.Logging;
/// <summary>
/// Abstraction for platform-specific paths used by components that need
/// an application data directory or other platform locations.
/// </summary>
public interface IPlatformPaths
{
    string AppDataDirectory { get; }
}

/// <summary>
/// Default implementation that delegates to MAUI/Essentials FileSystem.
/// </summary>
public sealed class DefaultPlatformPaths : IPlatformPaths
{
    // Fallback implementation that uses the standard .NET special folder.
    // Avoids referencing MAUI types from this library so the logging project
    // can remain a plain .NET library and be testable. Consumers that run
    // in MAUI can inject a platform-specific implementation if desired.
    public string AppDataDirectory =>
#if MAUI
        FileSystem.AppDataDirectory;
#else
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#endif
}
