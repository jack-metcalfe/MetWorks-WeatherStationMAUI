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
    private static Exception? _createPhaseException;
    private static bool _createPhaseCompleted;
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
            if (_createPhaseException is not null)
                throw new InvalidOperationException("DDI create phase previously failed.", _createPhaseException);

            if (_appRegistry is null)
            {
                try
                {
                    _appRegistry = new Registry();
                    _appRegistry.CreateAll();
                    _createPhaseCompleted = true;
                }
                catch (Exception ex)
                {
                    _createPhaseException = ex;
                    throw;
                }
            }

            try
            {
                // Register concrete instances into MAUI DI. This calls generated code that expects
                // the create phase to have been run so GetTheXyz() returns valid objects.
                try { _appRegistry.RegisterSingletonsInMaui(services); }
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

    private static MetWorks.Interfaces.ILogger? _iLogger;
    private static bool _isInitialized = false;
    private static bool _isDatabaseAvailable = false;
    
    // Expose registry for dependency access
    
    // Check if services are ready
    public static bool IsInitialized => _isInitialized;
    
    // Check database availability
    public static bool IsDatabaseAvailable => _isDatabaseAvailable;
    
    public static Task InitializeAsync()
        => InitializeAsync(CancellationToken.None);

    public static Task InitializeWithTimeoutAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        return InitializeAsync(cts.Token);
    }

    public static async Task InitializeAsync(CancellationToken cancellationToken)
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
            await RegisterServices(cancellationToken).ConfigureAwait(false);
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
            _iLogger?.Error($"Startup initialization failed: {exception}");
            
            // Re-throw with clear context for UI
            throw new InvalidOperationException(
                "Failed to initialize application services. Check debug output for details.", 
                exception);
        }
    }
    
    private static async Task RegisterServices(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            Registry registry;
            lock (_registryLock)
            {
                if (_createPhaseException is not null)
                    throw new InvalidOperationException("DDI create phase failed earlier; see inner exception.", _createPhaseException);

                if (_appRegistry is null || !_createPhaseCompleted)
                    throw new InvalidOperationException("DDI registry was not created during MAUI startup. Ensure CreateRegistryAndRegisterServices() is called from CreateMauiApp() before App construction.");

                registry = _appRegistry;
            }

            try
            {
                // Step 2: Initialize all services (initialization phase)
                Debug.WriteLine("🔧 Initializing services...");
                await registry.InitializeAllAsync(cancellationToken).ConfigureAwait(false);

                // Step 3: Cache logger after initialization
                await registry.WhenTheLoggerResilientInitializedAsync(cancellationToken);
                _iLogger = registry.GetTheLoggerResilient();
                _iLogger?.Information("✅ All services initialized");

                // Log settings source diagnostics (non-secret)
                try
                {
                    var sp = registry.GetTheSettingProvider() as SettingProvider;
                    if (sp is not null)
                    {
                        _iLogger?.Information(
                            $"Settings loaded. templateResource='{sp.SettingsTemplateResourceName}', overrideFile='{sp.SettingsOverrideFilePath}', overrideExists={sp.SettingsOverrideFileExistsAtLoad}"
                        );
                    }
                }
                catch { }

                // Step 4: Verify critical services
                await VerifyCriticalServicesAsync(cancellationToken).ConfigureAwait(false);

                // All services initialized successfully, including database
                _isDatabaseAvailable = true;
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"❌ Service initialization failed: {exception.Message}");
                Debug.WriteLine($"   Stack trace: {exception.StackTrace}");

                _iLogger?.Error($"Service initialization failed: {exception}");

                throw;
            }
        }
        catch (Exception exception)
        {
            // Always log to Debug output as fallback
            Debug.WriteLine($"❌ FATAL: Startup initialization failed: {exception}");

            // Try to log with file logger if available
            _iLogger?.Error($"Startup initialization failed: {exception}");

            // Re-throw with clear context for UI
            throw new InvalidOperationException(
                "Failed to initialize application services. Check debug output for details.",
                exception);
        }
    }
    
    private static async Task VerifyCriticalServicesAsync(CancellationToken cancellationToken)
    {
        if (_appRegistry is null)
            throw new InvalidOperationException("Registry is null after initialization");
        
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _appRegistry.WhenTheSettingRepositoryInitializedAsync().ConfigureAwait(false);
            if (_appRegistry.GetTheSettingRepository() is null)
                throw new InvalidOperationException("UDP settings repository failed to initialize");

            await _appRegistry.WhenTheUdpListenerInitializedAsync().ConfigureAwait(false);
            if (_appRegistry.GetTheUdpListener() is null)
                throw new InvalidOperationException("UDP listener failed to initialize");

            Debug.WriteLine("✅ All critical services verified");
            _iLogger.Information("Critical services verification completed successfully");
            
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
            _iLogger?.Information("🛑 Shutting down application services...");
            Debug.WriteLine("🛑 Shutting down application services...");
            
            if (_appRegistry != null)
            {
                _appRegistry.DisposeAll();
            }
            
            _isInitialized = false;
            _isDatabaseAvailable = false;
            
            _iLogger?.Information("✅ Application services shut down successfully");
            Debug.WriteLine("✅ Application services shut down successfully");
            
            await Task.CompletedTask;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"⚠️ Error during shutdown: {exception}");
            _iLogger?.Warning($"Error during shutdown: {exception}");
        }
    }
}
