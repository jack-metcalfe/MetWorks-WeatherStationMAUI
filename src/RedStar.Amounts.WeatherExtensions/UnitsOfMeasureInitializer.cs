namespace RedStar.Amounts.WeatherExtensions;

public class UnitsOfMeasureInitializer
{
    bool isInitialized = false;

    ILogger? _iLogger = null;
    ILogger ILogger
    {
        get => NullPropertyGuard.Get(isInitialized, _iLogger, nameof(ILogger));
        set => _iLogger = value;
    }
    public UnitsOfMeasureInitializer()
    {
    }
    public async Task<bool> InitializeAsync(
        ILogger iLogger
    )
    {
        bool result;
        try
        {
            ILogger = iLogger;
            Debug.WriteLine("📐 Registering RedStar.Amounts units...");
            UnitManager.RegisterByAssembly(typeof(TemperatureUnits).Assembly);
            Debug.WriteLine("✅ RedStar.Amounts units registered");

            Debug.WriteLine("🌤️ Registering weather unit aliases...");
            WeatherUnitAliases.Register();
            Debug.WriteLine("✅ Weather unit aliases registered");

            result = true;
        }

        catch (Exception exception)
        {
            Debug.WriteLine($"❌ Error during units of measure initialization: {exception}");
            result = false;
        }

        return await Task.FromResult(result);
    }
}