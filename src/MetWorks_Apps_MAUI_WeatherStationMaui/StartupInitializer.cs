using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace MetWorks.Apps.MAUI.WeatherStationMaui;
public class StartupInitializer
{
    private static int _initGuard = 0;
    // Initialization events for UI to observe progress and failures
    public static event Action<string>? StatusChanged;
    public static event Action? Initialized;
    public static event Action<Exception>? InitializationFailed;

    private static Registry? _appRegistry;
    private static readonly object _registryLock = new();
    /// <summary>
    /// Create the registry (create phase) and register the uninitialized instances into the provided
    /// IServiceCollection. This performs only the create phase so registrations can occur before
    /// the async initialization phase runs. This method is idempotent and safe to call multiple times.
    /// </summary>
    public static void CreateRegistryAndRegisterServices(IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        lock (_registryLock)
        {
            if (_appRegistry is null)
            {
                _appRegistry = new Registry();
                _appRegistry.CreateAll();
            }

            try
            {
                // Register concrete instances into MAUI DI. This calls generated code that expects
                // the create phase to have been run so GetTheXyz() returns valid objects.
                CancellationToken token = CancellationToken.None;
                try { _appRegistry.RegisterSingletonsInMauiAsync(services, token).GetAwaiter().GetResult(); }
                catch (Exception ex)
                {
                    try { Debug.WriteLine($"Failed to register DDI singletons into MAUI DI: {ex.Message}"); } catch { }
                    throw;
                }
            }
            catch
            {
                // propagate to caller
                throw;
            }
        }
    }
    public static Registry Registry => _appRegistry 
        ?? throw new InvalidOperationException("Registry is not initialized.");
    //NullPropertyGuard.Get(
    //IsInitialized, _appRegistry, nameof(Registry));

    private static ILoggerResilient? _iLoggerResilient;
    private static bool _isInitialized = false;
    private static bool _isDatabaseAvailable = false;
    
    // Expose registry for dependency access
    
    // Check if services are ready
    public static bool IsInitialized => _isInitialized;
    
    // Check database availability
    public static bool IsDatabaseAvailable => _isDatabaseAvailable;
    
    public static async Task InitializeAsync()
    {
        // Prevent concurrent initialization
        if (Interlocked.CompareExchange(ref _initGuard, 1, 0) != 0)
        {
            return;
        }

        try
        {
            Debug.WriteLine("🚀 Starting application services initialization...");
            StatusChanged?.Invoke("Starting initialization...");
            await RegisterServices().ConfigureAwait(false);
            _isInitialized = true;
            StatusChanged?.Invoke("Initialization complete");
            try { Initialized?.Invoke(); } catch { }
            Debug.WriteLine("✅ Application services initialized successfully");
        }
        catch (Exception exception)
        {
            // Always log to Debug output as fallback
            Debug.WriteLine($"❌ FATAL: Startup initialization failed: {exception}");
            StatusChanged?.Invoke("Initialization failed");
            try { InitializationFailed?.Invoke(exception); } catch { }
            
            // Try to log with file logger if available
            _iLoggerResilient?.Error($"Startup initialization failed: {exception}");
            
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
            // Ensure registry is created (create phase only)
            Debug.WriteLine("📦 Ensuring service registry is created...");
            if (_appRegistry is null)
            {
                _appRegistry = new Registry();
                _appRegistry.CreateAll();
                Debug.WriteLine("✅ Service registry created");
            }

            try
            {
                // Step 2: Initialize all services (initialization phase)
                Debug.WriteLine("🔧 Initializing services...");
                await _appRegistry!.InitializeAllAsync().ConfigureAwait(false);

                // Step 3: Cache logger after initialization
                _iLoggerResilient = _appRegistry.GetTheLoggerResilient();
                _iLoggerResilient?.Information("✅ All services initialized");

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
                _iLoggerResilient?.Warning("⚠️ PostgreSQL unavailable at startup. App running in degraded mode.");
                _iLoggerResilient?.Information("🔄 Auto-reconnection enabled - database will connect automatically when available");

                _isDatabaseAvailable = false;
                // Don't throw - app can continue without database
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"❌ Service initialization failed: {exception.Message}");
                Debug.WriteLine($"   Stack trace: {exception.StackTrace}");

                _iLoggerResilient?.Error($"Service initialization failed: {exception}");

                throw;
            }
        }
        catch (Exception exception)
        {
            // Always log to Debug output as fallback
            Debug.WriteLine($"❌ FATAL: Startup initialization failed: {exception}");

            // Try to log with file logger if available
            _iLoggerResilient?.Error($"Startup initialization failed: {exception}");

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
            if (_appRegistry.GetTheSettingRepository() is null)
                throw new InvalidOperationException("UDP settings repository failed to initialize");

            if (_appRegistry.GetTheUdpListener() is null)
                throw new InvalidOperationException("UDP listener failed to initialize");

            if (_appRegistry.GetTheLoggerPostgreSQL() is null)
                _iLoggerResilient?.Warning("⚠️ PostgreSQL settings repository not available");
            
            Debug.WriteLine("✅ All critical services verified");
            _iLoggerResilient.Information("Critical services verification completed successfully");
            
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
            _iLoggerResilient?.Information("🛑 Shutting down application services...");
            Debug.WriteLine("🛑 Shutting down application services...");
            
            if (_appRegistry != null)
            {
                _appRegistry.DisposeAll();
            }
            
            _isInitialized = false;
            _isDatabaseAvailable = false;
            
            _iLoggerResilient?.Information("✅ Application services shut down successfully");
            Debug.WriteLine("✅ Application services shut down successfully");
            
            await Task.CompletedTask;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"⚠️ Error during shutdown: {exception}");
            _iLoggerResilient?.Warning($"Error during shutdown: {exception}");
        }
    }
}
