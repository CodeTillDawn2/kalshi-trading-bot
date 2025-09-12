using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using BacklashDTOs.Configuration;
using BacklashBot.Hubs;
using BacklashBot.Services.Interfaces;
using BacklashBot.State;
using BacklashDTOs.Data;
using TradingStrategies.Configuration;
using TradingStrategies.Helpers.Interfaces;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service for managing dependency injection scopes in the Kalshi trading bot system.
    /// This service handles the creation, initialization, and disposal of service scopes,
    /// ensuring proper resource management and access to scoped services throughout the application.
    /// </summary>
    public class KaslhiBotScopeManagerService : IScopeManagerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IScopeManagerService> _logger;
        private IServiceScope? _scope;

        /// <summary>
        /// Gets the current active service scope, or null if no scope has been initialized.
        /// </summary>
        public IServiceScope? Scope => _scope;


        /// <summary>
        /// Initializes a new instance of the <see cref="KaslhiBotScopeManagerService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The root service provider for creating scopes.</param>
        /// <param name="logger">The logger instance for recording service operations.</param>
        public KaslhiBotScopeManagerService(IServiceProvider serviceProvider, ILogger<IScopeManagerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
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

            // Resolve critical services to validate scope initialization
            var configuration = sp.GetRequiredService<IConfiguration>();
            var executionConfig = sp.GetRequiredService<IOptions<ExecutionConfig>>();
            var kalshiConfig = sp.GetRequiredService<IOptions<KalshiConfig>>();
            var snapshotConfig = sp.GetRequiredService<IOptions<SnapshotConfig>>();
            var tradingConfig = sp.GetRequiredService<IOptions<TradingConfig>>();
            var calculationConfig = sp.GetRequiredService<IOptions<CalculationConfig>>();
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
            var chartHub = sp.GetRequiredService<IHubContext<ChartHub>>();
            var marketFactory = sp.GetRequiredService<Func<MarketDTO, MarketData>>();
            var marketDataService = sp.GetRequiredService<IMarketDataService>();
            var orderBookService = sp.GetRequiredService<IOrderBookService>();
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
            return _serviceProvider.CreateScope();
        }

        /// <summary>
        /// Disposes the current service scope and releases all associated resources.
        /// This method implements the IDisposable pattern for proper cleanup.
        /// </summary>
        public void Dispose()
        {
            _scope?.Dispose();
            _scope = null;
        }
    }
}
