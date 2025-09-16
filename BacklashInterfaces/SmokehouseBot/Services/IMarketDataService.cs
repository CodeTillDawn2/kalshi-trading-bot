
using BacklashBot.State.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;

namespace BacklashBot.Services.Interfaces
{
/// <summary>IMarketDataService</summary>
/// <summary>IMarketDataService</summary>
    public interface IMarketDataService
/// <summary>Gets or sets the PositionDataUpdated.</summary>
/// <summary>Gets or sets the MarketDataUpdated.</summary>
    {
/// <summary>Gets or sets the AccountBalanceUpdated.</summary>
/// <summary>Gets or sets the WatchListChanged.</summary>
        event EventHandler<string> MarketDataUpdated;
/// <summary>AddMarketToWatchList</summary>
/// <summary>Gets or sets the AccountBalanceUpdated.</summary>
        event EventHandler<string> PositionDataUpdated;
/// <summary>GetTradingStatus</summary>
/// <summary>Gets or sets the MarketsToRefresh.</summary>
        event EventHandler WatchListChanged;
/// <summary>StopServicesAsync</summary>
/// <summary>GetLatestOrderbookTimestamp</summary>
        event EventHandler<string> TickerAdded;
/// <summary>UpdateWatchedMarketsAsync</summary>
/// <summary>GetTradingStatus</summary>
        event EventHandler<string> AccountBalanceUpdated;
/// <summary>GetMarketDetails</summary>
/// <summary>UpdateMarketSubscriptionAsync</summary>

/// <summary>GetPortfolioValue</summary>
/// <summary>SyncMarketDataAsync</summary>
        List<string> MarketsToRefresh { get; set; }
/// <summary>ProcessTickerUpdate</summary>
/// <summary>UpdateWatchedMarketsAsync</summary>
        Task AddMarketToWatchList(string marketTicker, double? interestScore = null);
/// <summary>GetCurrentOrderBook</summary>
        DateTime? GetLatestOrderbookTimestamp(string marketTicker);
/// <summary>GetMarketDetailsBatchAsync</summary>
        bool GetExchangeStatus();
/// <summary>RetrieveAndUpdatePositionsAsync</summary>
/// <summary>GetPortfolioValue</summary>
        bool GetTradingStatus();
/// <summary>UpdateAccountBalanceAsync</summary>
        Task UnwatchMarket(string marketTicker);
/// <summary>NotifyMarketDataUpdated</summary>
        Task UpdateMarketSubscriptionAsync(string action, string[] marketTickers);
/// <summary>NotifyTickerAdded</summary>
        void StopServicesAsync();
/// <summary>ForwardFillCandlesticks</summary>
        Task SyncMarketDataAsync(string marketTicker);
/// <summary>RetrieveAndUpdatePositionsAsync</summary>
        Task<MarketDTO?> EnsureMarketDataAsync(string marketTicker);
        Task UpdateWatchedMarketsAsync();
        Task<List<string>> FetchWatchedMarketsAsync();
        List<OrderbookData> GetCurrentOrderBook(string marketTicker);
        IMarketData GetMarketDetails(string marketTicker);
        Task<Dictionary<string, IMarketData>> GetMarketDetailsBatchAsync(IEnumerable<string> marketTickers, CancellationToken cancellationToken = default);
        double GetAccountBalance();
        double GetPortfolioValue();
        DateTime GetLatestWebSocketTimestamp();
        Task UpdateAccountBalanceAsync();
        void ProcessTickerUpdate(string marketTicker, Guid marketId, int price, int yesBid, int yesAsk, int volume, int openInterest, int dollarVolume, int dollarOpenInterest, long ts, DateTime loggedDate, DateTime? processedDate = null);
        void NotifyMarketDataUpdated(string marketTicker);
        void NotifyPositionDataUpdated(string marketTicker);
        void NotifyTickerAdded(string marketTicker);
        void NotifyAccountBalanceUpdated(string marketTicker);
        List<CandlestickData> ForwardFillCandlesticks(List<CandlestickData> candlesticks, string marketTicker);
        void ConfigureWebSocketEventHandlers();
        Task RetrieveAndUpdatePositionsAsync();
        Task SubscribeToMarketChannelsAsync(string marketTicker);
    }
}
