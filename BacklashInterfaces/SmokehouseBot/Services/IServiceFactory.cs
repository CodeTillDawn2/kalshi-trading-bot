using BacklashBot.Management.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashInterfaces.SmokehouseBot.Services;
using KalshiBotAPI.WebSockets.Interfaces;
using TradingStrategies.Helpers.Interfaces;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service factory that provides centralized access
    /// to various services within the trading bot application through dependency injection.
    /// </summary>
    public interface IServiceFactory : IDisposable
    {
        /// <summary>
        /// Gets the Kalshi WebSocket client service instance.
        /// </summary>
        /// <returns>The WebSocket client service, or null if not available.</returns>
        IKalshiWebSocketClient? GetKalshiWebSocketClient();

        /// <summary>
        /// Gets the interest score service instance.
        /// </summary>
        /// <returns>The interest score service, or null if not available.</returns>
        IInterestScoreService? GetInterestScoreService();

        /// <summary>
        /// Gets the SQL data service instance.
        /// </summary>
        /// <returns>The SQL data service, or null if not available.</returns>
        ISqlDataService? GetSqlDataService();

        /// <summary>
        /// Gets the trading snapshot service instance.
        /// </summary>
        /// <returns>The trading snapshot service, or null if not available.</returns>
        ITradingSnapshotService? GetTradingSnapshotService();

        /// <summary>
        /// Gets the market data service instance.
        /// </summary>
        /// <returns>The market data service, or null if not available.</returns>
        IMarketDataService? GetMarketDataService();

        /// <summary>
        /// Gets the trading calculator service instance.
        /// </summary>
        /// <returns>The trading calculator service, or null if not available.</returns>
        ITradingCalculator? GetTradingCalculator();

        /// <summary>
        /// Gets the order book service instance.
        /// </summary>
        /// <returns>The order book service, or null if not available.</returns>
        IOrderBookService? GetOrderBookService();

        /// <summary>
        /// Gets the central performance monitor service instance.
        /// </summary>
        /// <returns>The performance monitor service, or null if not available.</returns>
        ICentralPerformanceMonitor? GetPerformanceMonitor();

        /// <summary>
        /// Gets the data cache service instance.
        /// </summary>
        /// <returns>The data cache service, or null if not available.</returns>
        IDataCache? GetDataCache();

        /// <summary>
        /// Gets the market data initializer service instance.
        /// </summary>
        /// <returns>The market data initializer service, or null if not available.</returns>
        IMarketDataInitializer? GetMarketDataInitializer();

        /// <summary>
        /// Gets the candlestick service instance.
        /// </summary>
        /// <returns>The candlestick service, or null if not available.</returns>
        ICandlestickService? GetCandlestickService();

        /// <summary>
        /// Gets the broadcast service instance.
        /// </summary>
        /// <returns>The broadcast service, or null if not available.</returns>
        IBroadcastService? GetBroadcastService();

        /// <summary>
        /// Gets the market refresh service instance.
        /// </summary>
        /// <returns>The market refresh service, or null if not available.</returns>
        IMarketRefreshService? GetMarketRefreshService();

        /// <summary>
        /// Gets the WebSocket monitor hosted service instance.
        /// </summary>
        /// <returns>The WebSocket monitor service, or null if not available.</returns>
        IWebSocketMonitorService? GetWebSocketHostedService();

        /// <summary>
        /// Gets the central error handler service instance.
        /// </summary>
        /// <returns>The error handler service, or null if not available.</returns>
        ICentralErrorHandler? GetBacklashErrorHandler();

        /// <summary>
        /// Gets the overseer client service instance.
        /// </summary>
        /// <returns>The overseer client service, or null if not available.</returns>
        IOverseerClientService? GetOverseerClientService();

        /// <summary>
        /// Gets the scope manager service instance.
        /// </summary>
        /// <returns>The scope manager service.</returns>
        IScopeManagerService GetScopeManager();

        /// <summary>
        /// Resets all managed services to their initial state.
        /// </summary>
        void ResetAll();

        /// <summary>
        /// Initializes all services with the specified brain lock identifier.
        /// </summary>
        /// <param name="brainLock">The brain lock GUID for service initialization.</param>
        void InitializeServices(Guid brainLock);
    }
}
