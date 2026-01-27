using System.Reflection;
namespace MetWorks.RedStar.Amounts.WeatherExtensions;
public class UnitsOfMeasureInitializer
{
    bool isInitialized = false;

    ILogger? _iLogger = null;
    ILogger ILogger => NullPropertyGuard.Get(isInitialized, _iLogger, nameof(ILogger));
    public UnitsOfMeasureInitializer()
    {
    }
    public async Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient
    )
    {
        _iLogger = iLoggerResilient ?? throw new ArgumentNullException(nameof(iLoggerResilient));
        try
        {
            _iLogger.Information("📐 Registering RedStar.Amounts units...");
            var asm = typeof(TemperatureUnits).Assembly;
            try
            {
                var exported = asm.GetExportedTypes();
                _iLogger.Debug($"Unit assembly: {asm.FullName}, exported types: {exported.Length}");
            }
            catch (ReflectionTypeLoadException rtle)
            {
                _iLogger.Error("Failed to enumerate exported types for unit assembly.", rtle);
                foreach (var le in rtle.LoaderExceptions ?? Array.Empty<Exception>())
                {
                    _iLogger.Error("Loader exception enumerating unit assembly types.", le);
                }
                // Rethrow to be handled by outer handler
                throw;
            }

            try
            {
                UnitManager.RegisterByAssembly(asm);
                _iLogger.Information("✅ RedStar.Amounts units registered");
            }
            catch (ReflectionTypeLoadException rtle)
            {
                // Log loader exceptions with details to aid Android linker diagnostics
                _iLogger.Error("ReflectionTypeLoadException while registering unit assembly.", rtle);
                foreach (var le in rtle.LoaderExceptions ?? Array.Empty<Exception>())
                {
                    try { _iLogger.Error("Loader exception during UnitManager.RegisterByAssembly:", le); } catch { }
                }
                // Rethrow so outer handler can handle flow (degraded startup)
                throw;
            }

            _iLogger.Information("🌤️ Registering weather unit aliases...");
            WeatherUnitAliases.Register();
            _iLogger.Information("✅ Weather unit aliases registered");

            isInitialized = true;
            return await Task.FromResult(true).ConfigureAwait(false);
        }
        catch (ReflectionTypeLoadException rtle)
        {
            // ReflectionTypeLoadException contains loader exceptions with useful details
            try { _iLogger.Error("Failed to register unit types - reflection load error.", rtle); } catch { }
            foreach (var le in rtle.LoaderExceptions ?? Array.Empty<Exception>())
            {
                try { _iLogger.Error("LoaderException during unit registration.", le); } catch { }
            }
            return await Task.FromResult(false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try { _iLogger.Error("Error during units of measure initialization.", ex); } catch { }
            return await Task.FromResult(false).ConfigureAwait(false);
        }
    }
}