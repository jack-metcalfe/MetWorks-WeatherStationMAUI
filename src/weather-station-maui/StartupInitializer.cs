namespace MetWorksWeather;
public class StartupInitializer
{
    private static Registry? _appRegistry;
    public static Registry Registry => _appRegistry 
        ?? throw new InvalidOperationException("Registry is not initialized.");
    //NullPropertyGuard.Get(
    //IsInitialized, _appRegistry, nameof(Registry));

    private static ILogger? _fileLogger;
    private static bool _isInitialized = false;
    private static bool _isDatabaseAvailable = false;
    
    // Expose registry for dependency access
    
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
                _fileLogger = _appRegistry.GetTheLoggerFile();
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
        if (_appRegistry is null)
            throw new InvalidOperationException("Registry is null after initialization");
        
        try
        {
            // Verify logger is available (CRITICAL - must have)
            var logger = _appRegistry.GetTheLoggerFile();
            if (logger is null)
                throw new InvalidOperationException("File logger failed to initialize");
            
            // Verify UDP settings repository (CRITICAL - must have)
            var udpRepo = _appRegistry.GetTheUdpListener();
            if (udpRepo is null)
                throw new InvalidOperationException("UDP settings repository failed to initialize");
            
            // Verify UDP listener (CRITICAL - must have)
            var udpListener = _appRegistry.GetTheUdpListener();
            if (udpListener is null)
                throw new InvalidOperationException("UDP listener failed to initialize");
            
            // Postgres is now OPTIONAL - don't fail if it's not available
            var pgRepo = _appRegistry.GetTheSettingRepository();
            if (pgRepo is null)
                logger.Warning("⚠️ PostgreSQL settings repository not available - database features disabled");
            
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
