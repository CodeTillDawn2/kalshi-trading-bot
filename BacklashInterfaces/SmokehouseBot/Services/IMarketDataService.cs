
using BacklashBot.State.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;

namespace BacklashBot.Services.Interfaces
{
    public interface IMarketDataService
    {
        event EventHandler<string> MarketDataUpdated;
        event EventHandler<string> PositionDataUpdated;
        event EventHandler WatchListChanged;
        event EventHandler<string> TickerAdded;
        event EventHandler<string> AccountBalanceUpdated;

        List<string> MarketsToRefresh { get; set; }
        Task AddMarketToWatchList(string marketTicker, double? interestScore = null);
        DateTime? GetLatestOrderbookTimestamp(string marketTicker);
        bool GetExchangeStatus();
        bool GetTradingStatus();
        Task UnwatchMarket(string marketTicker);
        Task UpdateMarketSubscriptionAsync(string action, string[] marketTickers);
        void StopServicesAsync();
        Task SyncMarketDataAsync(string marketTicker);
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
