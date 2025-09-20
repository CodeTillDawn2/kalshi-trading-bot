using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Options;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs.Exceptions;
using BacklashInterfaces.SmokehouseBot.Services;
using TradingStrategies.Helpers.Interfaces;
using System.Collections.Concurrent;
using BacklashCommon.Configuration;

namespace BacklashBot.Services
{
    /// <summary>
    /// Provides centralized access to various services within the Kalshi trading bot system through dependency injection scopes.
    /// This factory implements the service locator pattern to manage service initialization, configuration validation,
    /// and thread-safe access to scoped services. It acts as a bridge between the application's dependency injection
    /// container and service consumers, ensuring proper scope management and resource cleanup.
    /// Services are cached per scope to improve performance by avoiding repeated dependency injection resolution.
    /// </summary>
    public class ServiceFactory : IServiceFactory
    {

        private readonly object _lock = new();
        private readonly ILogger<IServiceFactory> _logger;
        private readonly IOptions<SecretsConfig> _secretsConfig;
        private IScopeManagerService _scopeManager;
        private Guid _brainLock;
        private readonly ConcurrentDictionary<Type, object> _serviceCache = new();

        /// <summary>
        /// Initializes a new instance of the ServiceFactory with required dependencies.
        /// </summary>
        /// <param name="logger">Logger instance for recording service factory operations and errors.</param>
        /// <param name="scopeManagerService">Service responsible for managing dependency injection scopes.</param>
        /// <param name="secretsConfig">Configuration for secrets paths.</param>
        public ServiceFactory(ILogger<IServiceFactory> logger, IScopeManagerService scopeManagerService, IOptions<SecretsConfig> secretsConfig)
        {
            _scopeManager = scopeManagerService;
            _logger = logger;
            _secretsConfig = secretsConfig;
        }

        /// <summary>
        /// Retrieves a service from the dependency injection container with caching to improve performance.
        /// Services are cached per scope to avoid repeated resolution calls.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>The service instance, or null if the service scope is not initialized.</returns>
        private T? GetService<T>() where T : class
        {
            if (_scopeManager.Scope?.ServiceProvider == null)
                return null;

            try
            {
                _logger.LogDebug("SF: Attempting to resolve service: {ServiceType}", typeof(T).Name);
                var service = (T)_serviceCache.GetOrAdd(typeof(T), _ =>
                {
                    _logger.LogDebug("SF: Resolving {ServiceType} from DI container", typeof(T).Name);
                    return _scopeManager.Scope.ServiceProvider.GetRequiredService<T>();
                });
                _logger.LogDebug("SF: Successfully resolved service: {ServiceType}", typeof(T).Name);
                return service;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SF: Failed to resolve service {ServiceType}: {Message}", typeof(T).Name, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Initializes the service factory with the specified brain lock identifier and performs critical setup operations.
        /// This method validates configuration, initializes the service scope, and configures WebSocket event handlers
        /// for market data and order book services. Thread-safe execution is ensured through locking.
        /// </summary>
        /// <param name="brainLock">Unique identifier for the brain instance to prevent concurrent access conflicts.</param>
        /// <exception cref="FileNotFoundException">Thrown when the Kalshi key file specified in configuration is not found.</exception>
        public void InitializeServices(Guid brainLock)
        {
            lock (_lock)
            {
                _logger.LogInformation("SF: InitializeServices called with brainLock: {BrainLock}", brainLock);

                _scopeManager.InitializeScope();
                _logger.LogInformation("SF: Scope initialized");

                var kalshiConfig = _scopeManager.Scope?.ServiceProvider.GetRequiredService<IOptions<KalshiConfig>>();


                _brainLock = brainLock;


                _logger.LogInformation("SF: About to resolve key file path");
                var resolvedKeyFile = ConfigurationHelper.ResolveSecretsFilePath(kalshiConfig.Value.KeyFile, _secretsConfig.Value, AppDomain.CurrentDomain.BaseDirectory);
                _logger.LogInformation("SF: Key file resolved to: {ResolvedKeyFile}", resolvedKeyFile);

                if (string.IsNullOrEmpty(resolvedKeyFile) || !File.Exists(resolvedKeyFile))
                {
                    var errorMessage = $"Kalshi Key file not found at {resolvedKeyFile}";
                    _logger.LogError(errorMessage);
                    throw new FileNotFoundException(errorMessage, resolvedKeyFile);
                }
                _logger.LogInformation("SF: Key file validation passed");

                var marketDataService = _scopeManager.Scope?.ServiceProvider.GetRequiredService<IMarketDataService>();
                var orderBookService = _scopeManager.Scope?.ServiceProvider.GetRequiredService<IOrderBookService>();

                _logger.LogInformation("SF: About to call ConfigureWebSocketEventHandlers on MarketDataService");
                marketDataService.ConfigureWebSocketEventHandlers();
                _logger.LogInformation("SF: ConfigureWebSocketEventHandlers completed on MarketDataService");

                _logger.LogInformation("SF: About to call ConfigureWebSocketEventHandlers on OrderBookService");
                orderBookService.ConfigureWebSocketEventHandlers();
                _logger.LogInformation("SF: ConfigureWebSocketEventHandlers completed on OrderBookService");
            }
        }

        /// <summary>
        /// Retrieves the Kalshi WebSocket client service for real-time market data communication.
        /// </summary>
        /// <returns>The WebSocket client instance, or null if the service scope is not initialized.</returns>
        public IKalshiWebSocketClient? GetKalshiWebSocketClient() => GetService<IKalshiWebSocketClient>();

        /// <summary>
        /// Retrieves the interest score service used for calculating market interest metrics.
        /// </summary>
        /// <returns>The interest score service instance, or null if the service scope is not initialized.</returns>
        public IInterestScoreService? GetInterestScoreService() => GetService<IInterestScoreService>();

        /// <summary>
        /// Retrieves the SQL data service for database operations and data persistence.
        /// </summary>
        /// <returns>The SQL data service instance, or null if the service scope is not initialized.</returns>
        public ISqlDataService? GetSqlDataService() => GetService<ISqlDataService>();

        /// <summary>
        /// Retrieves the trading snapshot service for capturing and managing market snapshots.
        /// </summary>
        /// <returns>The trading snapshot service instance, or null if the service scope is not initialized.</returns>
        public ITradingSnapshotService? GetTradingSnapshotService() => GetService<ITradingSnapshotService>();

        /// <summary>
        /// Retrieves the market data service for managing market information and real-time updates.
        /// </summary>
        /// <returns>The market data service instance, or null if the service scope is not initialized.</returns>
        public IMarketDataService? GetMarketDataService() => GetService<IMarketDataService>();

        /// <summary>
        /// Retrieves the trading calculator service for performing technical analysis calculations.
        /// </summary>
        /// <returns>The trading calculator service instance, or null if the service scope is not initialized.</returns>
        public ITradingCalculator? GetTradingCalculator() => GetService<ITradingCalculator>();

        /// <summary>
        /// Retrieves the order book service for managing order book data and updates.
        /// </summary>
        /// <returns>The order book service instance, or null if the service scope is not initialized.</returns>
        public IOrderBookService? GetOrderBookService() => GetService<IOrderBookService>();

        /// <summary>
        /// Retrieves the central performance monitor for tracking system performance metrics.
        /// </summary>
        /// <returns>The performance monitor instance, or null if the service scope is not initialized.</returns>
        public ICentralPerformanceMonitor? GetPerformanceMonitor() => GetService<ICentralPerformanceMonitor>();

        /// <summary>
        /// Retrieves the data cache service for caching frequently accessed data.
        /// </summary>
        /// <returns>The data cache service instance, or null if the service scope is not initialized.</returns>
        public IDataCache? GetDataCache() => GetService<IDataCache>();

        /// <summary>
        /// Retrieves the market data initializer service for setting up initial market data.
        /// </summary>
        /// <returns>The market data initializer instance, or null if the service scope is not initialized.</returns>
        public IMarketDataInitializer? GetMarketDataInitializer() => GetService<IMarketDataInitializer>();

        /// <summary>
        /// Retrieves the candlestick service for managing candlestick chart data.
        /// </summary>
        /// <returns>The candlestick service instance, or null if the service scope is not initialized.</returns>
        public ICandlestickService? GetCandlestickService() => GetService<ICandlestickService>();

        /// <summary>
        /// Retrieves the broadcast service for sending real-time updates to connected clients.
        /// </summary>
        /// <returns>The broadcast service instance, or null if the service scope is not initialized.</returns>
        public IBroadcastService? GetBroadcastService() => GetService<IBroadcastService>();

        /// <summary>
        /// Retrieves the market refresh service for periodically updating market information.
        /// </summary>
        /// <returns>The market refresh service instance, or null if the service scope is not initialized.</returns>
        public IMarketRefreshService? GetMarketRefreshService() => GetService<IMarketRefreshService>();

        /// <summary>
        /// Retrieves the WebSocket monitor service for overseeing WebSocket connection health.
        /// </summary>
        /// <returns>The WebSocket monitor service instance, or null if the service scope is not initialized.</returns>
        public IWebSocketMonitorService? GetWebSocketHostedService() => GetService<IWebSocketMonitorService>();

        /// <summary>
        /// Retrieves the central error handler for managing system-wide error processing and recovery.
        /// </summary>
        /// <returns>The central error handler instance, or null if the service scope is not initialized.</returns>
        public ICentralErrorHandler? GetBacklashErrorHandler() => GetService<ICentralErrorHandler>();

        /// <summary>
        /// Retrieves the overseer client service for communication with the overseer system.
        /// </summary>
        /// <returns>The overseer client service instance, or null if the service scope is not initialized.</returns>
        public IOverseerClientService? GetOverseerClientService() => GetService<IOverseerClientService>();

        /// <summary>
        /// Retrieves the scope manager service for managing dependency injection scopes.
        /// </summary>
        /// <returns>The scope manager service instance.</returns>
        public IScopeManagerService GetScopeManager() => _scopeManager;



        /// <summary>
        /// Resets all services by disposing of the current service scope and clearing resources.
        /// This method ensures thread-safe disposal of scoped services and prepares the factory
        /// for potential reinitialization. Used for system resets and cleanup operations.
        /// Also clears the service cache to ensure fresh service resolution on next initialization.
        /// </summary>
        public void ResetAll()
        {
            lock (_lock)
            {
                _scopeManager.Dispose();
                _serviceCache.Clear();
            }
        }

        /// <summary>
        /// Disposes of the service factory and releases all managed resources.
        /// This method ensures proper cleanup of the scope manager and any associated services.
        /// Thread-safe execution is maintained through locking to prevent concurrent access issues.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                _scopeManager.Dispose();
            }
        }
    }
}
