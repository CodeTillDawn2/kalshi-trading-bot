using BacklashBot.Management.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashInterfaces.SmokehouseBot.Services;
using KalshiBotAPI.WebSockets.Interfaces;
using TradingStrategies.Helpers.Interfaces;

namespace BacklashBot.Services.Interfaces
{
    public interface IServiceFactory : IDisposable
    {
        IKalshiWebSocketClient? GetKalshiWebSocketClient();
        IInterestScoreService? GetInterestScoreService();
        ISqlDataService? GetSqlDataService();
        ITradingSnapshotService? GetTradingSnapshotService();
        IMarketDataService? GetMarketDataService();
        ITradingCalculator? GetTradingCalculator();
        IOrderBookService? GetOrderBookService();
        ICentralPerformanceMonitor? GetPerformanceMonitor();
        IDataCache? GetDataCache();
        IMarketDataInitializer? GetMarketDataInitializer();
        ICandlestickService? GetCandlestickService();
        IBroadcastService? GetBroadcastService();
        IMarketRefreshService? GetMarketRefreshService();
        IWebSocketMonitorService? GetWebSocketHostedService();
        ICentralErrorHandler? GetBacklashErrorHandler();
        IOverseerClientService? GetOverseerClientService();
        IScopeManagerService GetScopeManager();
        void ResetAll();
        void InitializeServices(Guid _brainLock);
    }
}
