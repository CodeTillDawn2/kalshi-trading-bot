using KalshiBotAPI.WebSockets.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.State.Interfaces;
using TradingStrategies.Helpers.Interfaces;

namespace BacklashBot.Services.Interfaces
{
    public interface IServiceFactory : IDisposable
    {
        IKalshiWebSocketClient? GetKalshiWebSocketClient();
        IInterestScoreService? GetMarketInterestScoreHelper();
        ISqlDataService? GetSqlDataService();
        ITradingSnapshotService? GetTradingSnapshotService();
        IMarketDataService? GetMarketDataService();
        ITradingCalculator? GetTradingCalculator();
        IOrderBookService? GetOrderBookService();
        IDataCache? GetDataCache();
        IMarketDataInitializer? GetMarketDataInitializer();
        ICandlestickService? GetCandlestickService();
        IBroadcastService? GetBroadcastService();
        IMarketRefreshService? GetMarketRefreshService();
        IWebSocketMonitorService? GetWebSocketHostedService();
        ICentralErrorHandler? GetSmokehouseErrorHandler();
        void ResetAll();
        void InitializeServices(Guid _brainLock);
    }
}
