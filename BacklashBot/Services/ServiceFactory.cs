using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Options;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs.Exceptions;
using BacklashInterfaces.SmokehouseBot.Services;
using TradingStrategies.Helpers.Interfaces;

namespace BacklashBot.Services
{
    public class ServiceFactory : IServiceFactory
    {

        private readonly object _lock = new();
        private readonly ILogger<IServiceFactory> _logger;
        private IScopeManagerService _scopeManager;
        private Guid _brainLock;

        public ServiceFactory(ILogger<IServiceFactory> logger, IScopeManagerService scopeManagerService)
        {
            _scopeManager = scopeManagerService;
            _logger = logger;
        }

        public void InitializeServices(Guid brainLock)
        {
            lock (_lock)
            {

                _scopeManager.InitializeScope();

                var kalshiConfig = _scopeManager.Scope?.ServiceProvider.GetRequiredService<IOptions<KalshiConfig>>();


                _brainLock = brainLock;


                if (kalshiConfig.Value.KeyFile == null || !File.Exists(kalshiConfig.Value.KeyFile))
                {
                    _logger.LogWarning(new KalshiKeyFileNotFoundException($"Kalshi key file not found: {kalshiConfig.Value.KeyFile}")
                        , "Kalshi key file not found: {KeyFile}", kalshiConfig.Value.KeyFile);
                    throw new FileNotFoundException("Kalshi key file not found", kalshiConfig.Value.KeyFile);
                }

                var marketDataService = _scopeManager.Scope?.ServiceProvider.GetRequiredService<IMarketDataService>();
                var orderBookService = _scopeManager.Scope?.ServiceProvider.GetRequiredService<IOrderBookService>();

                marketDataService.AssignWebSocketHandlers();
                orderBookService.AssignWebSocketHandlers();
            }
        }

        public IKalshiWebSocketClient? GetKalshiWebSocketClient() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<IKalshiWebSocketClient>() ?? null;
        public IInterestScoreService? GetMarketInterestScoreHelper() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<IInterestScoreService>() ?? null;
        public ISqlDataService? GetSqlDataService() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<ISqlDataService>() ?? null;
        public ITradingSnapshotService? GetTradingSnapshotService() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<ITradingSnapshotService>() ?? null;
        public IMarketDataService? GetMarketDataService() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<IMarketDataService>() ?? null;
        public ITradingCalculator? GetTradingCalculator() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<ITradingCalculator>() ?? null;
        public IOrderBookService? GetOrderBookService() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<IOrderBookService>() ?? null;
        public IDataCache? GetDataCache() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<IDataCache>() ?? null;
        public IMarketDataInitializer? GetMarketDataInitializer() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<IMarketDataInitializer>() ?? null;
        public ICandlestickService? GetCandlestickService() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<ICandlestickService>() ?? null;
        public IBroadcastService? GetBroadcastService() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<IBroadcastService>() ?? null;
        public IMarketRefreshService? GetMarketRefreshService() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<IMarketRefreshService>() ?? null;
        public IWebSocketMonitorService? GetWebSocketHostedService() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<IWebSocketMonitorService>() ?? null;
        public ICentralErrorHandler? GetBacklashErrorHandler() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<ICentralErrorHandler>() ?? null;
        public IOverseerClientService? GetOverseerClientService() => _scopeManager.Scope?.ServiceProvider.GetRequiredService<IOverseerClientService>() ?? null;
        public IScopeManagerService GetScopeManager() => _scopeManager;



        public void ResetAll()
        {
            lock (_lock)
            {
                var broadcastService = GetBroadcastService();
                if (broadcastService != null)
                    broadcastService.UnsubscribeFromEvents();
                _scopeManager.Dispose();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _scopeManager.Dispose();
            }
        }
    }
}
