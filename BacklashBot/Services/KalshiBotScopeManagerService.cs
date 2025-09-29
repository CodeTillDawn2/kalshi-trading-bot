using BacklashBot.Configuration;
using BacklashBot.Hubs;
using BacklashBot.Services.Interfaces;
using BacklashBot.State;
using BacklashDTOs.Data;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using TradingStrategies.Helpers.Interfaces;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service for managing dependency injection scopes in the Kalshi trading bot system.
    /// This service handles the creation, initialization, and disposal of service scopes,
    /// ensuring proper resource management and access to scoped services throughout the application.
    /// Includes configurable performance metrics for monitoring scope efficiency.
    /// </summary>
    public class KalshiBotScopeManagerService : IScopeManagerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IScopeManagerService> _logger;
        private IServiceScope _scope;

        // Performance metrics fields
        private int _initializeScopeCallCount = 0;
        private int _createScopeCallCount = 0;
        private DateTime? _scopeCreationTime;
        private System.Timers.Timer? _metricsTimer;
        private readonly IPerformanceMonitor _monitor;
        private readonly bool _enableMetrics;

        /// <summary>
        /// Gets the current active service scope, or null if no scope has been initialized.
        /// </summary>
        public IServiceScope Scope => _scope;


        /// <summary>
        /// Initializes a new instance of the <see cref="KalshiBotScopeManagerService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The root service provider for creating scopes.</param>
        /// <param name="logger">The logger instance for recording service operations.</param>
        /// <param name="monitor">The central performance monitor for posting metrics.</param>
        /// <param name="configOptions">The configuration options for KalshiBotScopeManagerService settings.</param>
        public KalshiBotScopeManagerService(IServiceProvider serviceProvider, ILogger<IScopeManagerService> logger, IPerformanceMonitor monitor, IOptions<KalshiBotScopeManagerServiceConfig> configOptions)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _monitor = monitor;
            _enableMetrics = configOptions.Value.EnablePerformanceMetrics;
            if (_enableMetrics)
            {
                _metricsTimer = new System.Timers.Timer(60000); // 1 minute
                _metricsTimer.Elapsed += (sender, e) => LogMetrics();
                _metricsTimer.Start();
            }
        }

        /// <summary>
        /// Initializes a new service scope and resolves critical services to ensure they are available.
        /// This method creates a scope from the root service provider and validates that essential
        /// services can be resolved, preventing runtime errors due to missing dependencies.
        /// </summary>
        public void InitializeScope()
        {
            if (_scope != null)
            {
                _logger.LogWarning("Service scope already initialized, skipping reinitialization.");
                return;
            }

            _scope = _serviceProvider.CreateScope();
            var sp = _scope.ServiceProvider;
            Stopwatch? stopwatch = null;
            if (_enableMetrics)
            {
                stopwatch = Stopwatch.StartNew();
            }

            // Resolve critical services to validate scope initialization
            var configuration = sp.GetRequiredService<IConfiguration>();
            var kalshiConfig = sp.GetRequiredService<IOptions<KalshiConfig>>();
            var calculationConfig = sp.GetRequiredService<IOptions<CalculationsConfig>>();
            var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var marketDataLogger = sp.GetRequiredService<ILogger<IMarketDataService>>();
            var marketDataInitializerLogger = sp.GetRequiredService<ILogger<IMarketDataInitializer>>();
            var tradingCalculatorLogger = sp.GetRequiredService<ILogger<ITradingCalculator>>();
            var orderBookLogger = sp.GetRequiredService<ILogger<IOrderBookService>>();
            var candlestickLogger = sp.GetRequiredService<ILogger<ICandlestickService>>();
            var marketRefreshLogger = sp.GetRequiredService<ILogger<IMarketRefreshService>>();
            var webSocketLogger = sp.GetRequiredService<ILogger<IWebSocketMonitorService>>();
            var broadcastLogger = sp.GetRequiredService<ILogger<IBroadcastService>>();
            var kalshiWebSocketLogger = sp.GetRequiredService<ILogger<IKalshiWebSocketClient>>();
            var tradingSnapshotLogger = sp.GetRequiredService<ILogger<ITradingSnapshotService>>();
            var sqlDataServiceLogger = sp.GetRequiredService<ILogger<ISqlDataService>>();
            var chartHub = sp.GetRequiredService<IHubContext<BacklashBotHub>>();
            var marketFactory = sp.GetRequiredService<Func<MarketDTO, MarketData>>();
            var marketDataService = sp.GetRequiredService<IMarketDataService>();
            var orderBookService = sp.GetRequiredService<IOrderBookService>();

            if (_enableMetrics)
            {
                stopwatch?.Stop();
                _logger.LogInformation($"InitializeScope execution time: {stopwatch?.ElapsedMilliseconds} ms");
                RecordExecutionTimePrivate("InitializeScope", stopwatch?.ElapsedMilliseconds ?? 0, _enableMetrics);
                _initializeScopeCallCount++;
                _scopeCreationTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Resets the current service scope by disposing it and clearing the reference.
        /// This method should be called when the scope needs to be recreated or when
        /// the service is being shut down.
        /// </summary>
        public void ResetAll()
        {
            _scope?.Dispose();
            _scope = null;
        }

        /// <summary>
        /// Creates a new service scope from the root service provider.
        /// This method provides direct access to create scopes without initializing
        /// the managed scope used by this service.
        /// </summary>
        /// <returns>A new service scope instance.</returns>
        public IServiceScope CreateScope()
        {
            if (_enableMetrics)
            {
                _createScopeCallCount++;
            }
            return _serviceProvider.CreateScope();
        }

        /// <summary>
        /// Disposes the current service scope and releases all associated resources.
        /// This method implements the IDisposable pattern for proper cleanup.
        /// </summary>
        public void Dispose()
        {
            if (_enableMetrics && _scopeCreationTime.HasValue)
            {
                var lifetime = DateTime.UtcNow - _scopeCreationTime.Value;
                _logger.LogInformation($"Managed scope lifetime: {lifetime.TotalMilliseconds} ms");
                RecordExecutionTimePrivate("ScopeLifetime", (long)lifetime.TotalMilliseconds, _enableMetrics);
                _scopeCreationTime = null;
            }
            _scope?.Dispose();
            _scope = null;
            if (_enableMetrics)
            {
                _metricsTimer?.Dispose();
            }
        }

        /// <summary>
        /// Records execution time metrics using the IPerformanceMonitor interface.
        /// </summary>
        /// <param name="operationName">Name of the operation being timed</param>
        /// <param name="executionTimeMs">Execution time in milliseconds</param>
        /// <param name="enableMetrics">Whether performance metrics are enabled</param>
        private void RecordExecutionTimePrivate(string operationName, long executionTimeMs, bool enableMetrics)
        {
            string className = "KalshiBotScopeManagerService";
            string category = "ScopeManagement";

            if (!enableMetrics)
            {
                // Send disabled metric
                _monitor.RecordDisabledMetric(className, operationName, $"{operationName} Execution Time", $"Execution time for {operationName}", executionTimeMs, "ms", category, false);
            }
            else
            {
                // Record actual metric
                _monitor.RecordSpeedDialMetric(className, operationName, $"{operationName} Execution Time", $"Execution time for {operationName}", executionTimeMs, "ms", category, null, null, null, true);
            }
        }

        /// <summary>
        /// Logs the current performance metrics if enabled.
        /// </summary>
        public void LogMetrics()
        {
            if (_enableMetrics)
            {
                _logger.LogInformation($"Total InitializeScope calls: {_initializeScopeCallCount}");
                _logger.LogInformation($"Total CreateScope calls: {_createScopeCallCount}");
            }
        }
    }
}
