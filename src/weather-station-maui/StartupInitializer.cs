using System.Diagnostics;
using RedStar.Amounts;
using RedStar.Amounts.StandardUnits;
using RedStar.Amounts.WeatherExtensions;
using MetWorksWeather.Services;

namespace MetWorksWeather;

public class StartupInitializer
{
    private static Registry? _appRegistry;
    private static IFileLogger? _fileLogger;
    private static bool _isInitialized = false;
    private static bool _isDatabaseAvailable = false;
    private static MockWeatherReadingService? _mockWeatherService; // NEW: Mock service
    
    // Expose registry for dependency access
    public static Registry? Registry => _appRegistry;
    
    // Check if services are ready
    public static bool IsInitialized => _isInitialized;
    
    // Check database availability
    public static bool IsDatabaseAvailable => _isDatabaseAvailable;
    
    public static async Task InitializeAsync()
    {
        try
        {
            Debug.WriteLine("🚀 Starting application services initialization...");
            await RegisterServices().ConfigureAwait(false);
            _isInitialized = true;
            Debug.WriteLine("✅ Application services initialized successfully");
        }
        catch (Exception exception)
        {
            // Always log to Debug output as fallback
            Debug.WriteLine($"❌ FATAL: Startup initialization failed: {exception}");
            
            // Try to log with file logger if available
            _fileLogger?.Error($"Startup initialization failed: {exception}");
            
            // Re-throw with clear context for UI
            throw new InvalidOperationException(
                "Failed to initialize application services. Check debug output for details.", 
                exception);
        }
    }
    
    private static async Task RegisterServices()
    {
        try
        {
            // ========================================
            // Register RedStar.Amounts FIRST
            // ========================================
            Debug.WriteLine("📐 Registering RedStar.Amounts units...");
            UnitManager.RegisterByAssembly(typeof(TemperatureUnits).Assembly);
            Debug.WriteLine("✅ RedStar.Amounts units registered");
            
            // ========================================
            // Register weather unit aliases
            // ========================================
            Debug.WriteLine("🌤️ Registering weather unit aliases...");
            WeatherUnitAliases.Register();
            Debug.WriteLine("✅ Weather unit aliases registered");
            
            // ========================================
            // Existing service registry creation
            // ========================================
            Debug.WriteLine("📦 Creating service registry...");
            _appRegistry = new Registry();
            _appRegistry.CreateAll();
            Debug.WriteLine("✅ Service registry created");
            
            try
            {
                // Step 2: Initialize all services
                Debug.WriteLine("🔧 Initializing services...");
                await _appRegistry.InitializeAllAsync().ConfigureAwait(false);
                
                // Step 3: Cache logger after initialization
                _fileLogger = _appRegistry.GetTheFileLogger();
                _fileLogger?.Information("✅ All services initialized successfully");
                
                // Step 4: Verify critical services
                await VerifyCriticalServicesAsync().ConfigureAwait(false);
                
                // All services initialized successfully, including database
                _isDatabaseAvailable = true;
            }
            catch (InvalidOperationException exception) when (
                exception.Message.Contains("PostgreSQL") || 
                exception.InnerException is Npgsql.NpgsqlException)
            {
                Debug.WriteLine($"⚠️ PostgreSQL initialization failed: {exception.Message}");
                _fileLogger?.Warning("⚠️ PostgreSQL unavailable at startup. App running in degraded mode.");
                _fileLogger?.Information("🔄 Auto-reconnection enabled - database will connect automatically when available");
                
                _isDatabaseAvailable = false;
                // Don't throw - app can continue without database
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"❌ Service initialization failed: {exception.Message}");
                Debug.WriteLine($"   Stack trace: {exception.StackTrace}");
                
                _fileLogger?.Error($"Service initialization failed: {exception}");
                
                throw;
            }
            
            // ========================================
            // NEW: Start Mock Weather Service for Development
            // ========================================
            // TODO: Remove this in production or add conditional compilation
            //#if DEBUG
            //Debug.WriteLine("🎭 Starting mock weather service for development...");
            //_mockWeatherService = new MockWeatherReadingService();
            //_mockWeatherService.Start();
            //_fileLogger?.Information("🎭 Mock weather service started - publishing fake data every 2 seconds");
            //Debug.WriteLine("✅ Mock weather service running");
            //#endif
        }
        catch (Exception exception)
        {
            // Always log to Debug output as fallback
            Debug.WriteLine($"❌ FATAL: Startup initialization failed: {exception}");
            
            // Try to log with file logger if available
            _fileLogger?.Error($"Startup initialization failed: {exception}");
            
            // Re-throw with clear context for UI
            throw new InvalidOperationException(
                "Failed to initialize application services. Check debug output for details.", 
                exception);
        }
    }
    
    private static async Task VerifyCriticalServicesAsync()
    {
        if (_appRegistry == null)
        {
            throw new InvalidOperationException("Registry is null after initialization");
        }
        
        try
        {
            // Verify logger is available (CRITICAL - must have)
            var logger = _appRegistry.GetTheFileLogger();
            if (logger == null)
            {
                throw new InvalidOperationException("File logger failed to initialize");
            }
            
            // Verify UDP settings repository (CRITICAL - must have)
            var udpRepo = _appRegistry.GetTheUDPSettingsRepository();
            if (udpRepo == null)
            {
                throw new InvalidOperationException("UDP settings repository failed to initialize");
            }
            
            // Verify UDP listener (CRITICAL - must have)
            var udpListener = _appRegistry.GetTheUdpListener();
            if (udpListener == null)
            {
                throw new InvalidOperationException("UDP listener failed to initialize");
            }
            
            // Postgres is now OPTIONAL - don't fail if it's not available
            var pgRepo = _appRegistry.GetThePostgresSettingsRepository();
            if (pgRepo == null)
            {
                logger.Warning("⚠️ PostgreSQL settings repository not available - database features disabled");
            }
            
            Debug.WriteLine("✅ All critical services verified");
            logger.Information("Critical services verification completed successfully");
            
            await Task.CompletedTask;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"⚠️ Critical service verification failed: {exception.Message}");
            throw new InvalidOperationException("One or more critical services failed verification", exception);
        }
    }
    
    // Graceful shutdown
    public static async Task ShutdownAsync()
    {
        try
        {
            _fileLogger?.Information("🛑 Shutting down application services...");
            Debug.WriteLine("🛑 Shutting down application services...");
            
            // Stop mock service if running
            if (_mockWeatherService != null)
            {
                Debug.WriteLine("🎭 Stopping mock weather service...");
                _mockWeatherService.Stop();
                _mockWeatherService.Dispose();
                _mockWeatherService = null;
                Debug.WriteLine("✅ Mock weather service stopped");
            }
            
            if (_appRegistry != null)
            {
                _appRegistry.DisposeAll();
            }
            
            _isInitialized = false;
            _isDatabaseAvailable = false;
            
            _fileLogger?.Information("✅ Application services shut down successfully");
            Debug.WriteLine("✅ Application services shut down successfully");
            
            await Task.CompletedTask;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"⚠️ Error during shutdown: {exception}");
            _fileLogger?.Warning($"Error during shutdown: {exception}");
        }
    }
}
