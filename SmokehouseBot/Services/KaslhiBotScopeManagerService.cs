using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SmokehouseBot.Configuration;
using SmokehouseBot.Hubs;
using SmokehouseBot.Services.Interfaces;
using SmokehouseBot.State;
using SmokehouseDTOs.Data;
using TradingStrategies.Configuration;
using TradingStrategies.Helpers.Interfaces;

namespace SmokehouseBot.Services
{
    public class KaslhiBotScopeManagerService : IScopeManagerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IScopeManagerService> _logger;
        private IServiceScope? _scope;
        public IServiceScope? Scope => _scope;


        public KaslhiBotScopeManagerService(IServiceProvider serviceProvider, ILogger<IScopeManagerService> logger)
        {
            _serviceProvider = serviceProvider;

            _logger = logger;
        }

        public void InitializeScope()
        {

            if (_scope != null)
            {
                _logger.LogWarning("ServiceFactory already initialized, skipping reinitialization.");
                return;
            }

            _scope = _serviceProvider.CreateScope();
            var sp = _scope.ServiceProvider;
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

        public void ResetAll()
        {
            _scope?.Dispose();
            _scope = null;
        }

        public void Dispose()
        {
            _scope?.Dispose();
            _scope = null;
        }
    }
}