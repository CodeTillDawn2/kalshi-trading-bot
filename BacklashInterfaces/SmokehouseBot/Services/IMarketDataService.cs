
using BacklashBot.State.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a comprehensive market data service that manages market information,
    /// watch lists, positions, account data, and real-time updates through WebSocket connections.
    /// </summary>
    public interface IMarketDataService
    {
        /// <summary>
        /// Occurs when market data is updated for a specific market ticker.
        /// </summary>
        event EventHandler<string> MarketDataUpdated;

        /// <summary>
        /// Occurs when position data is updated for a specific market ticker.
        /// </summary>
        event EventHandler<string> PositionDataUpdated;

        /// <summary>
        /// Occurs when the watch list changes.
        /// </summary>
        event EventHandler WatchListChanged;

        /// <summary>
        /// Occurs when a new ticker is added to the system.
        /// </summary>
        event EventHandler<string> TickerAdded;

        /// <summary>
        /// Occurs when the account balance is updated.
        /// </summary>
        event EventHandler<string> AccountBalanceUpdated;

        /// <summary>
        /// Gets or sets the list of market tickers that need to be refreshed.
        /// </summary>
        List<string> MarketsToRefresh { get; set; }

        /// <summary>
        /// Adds a market to the watch list with an optional interest score.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to add.</param>
        /// <param name="interestScore">Optional interest score for the market.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddMarketToWatchList(string marketTicker, double? interestScore = null);

        /// <summary>
        /// Gets the latest orderbook timestamp for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market.</param>
        /// <returns>The latest orderbook timestamp, or null if not available.</returns>
        DateTime? GetLatestOrderbookTimestamp(string marketTicker);

        /// <summary>
        /// Gets the current exchange status.
        /// </summary>
        /// <returns><c>true</c> if the exchange is operational; otherwise, <c>false</c>.</returns>
        bool GetExchangeStatus();

        /// <summary>
        /// Gets the current trading status.
        /// </summary>
        /// <returns><c>true</c> if trading is active; otherwise, <c>false</c>.</returns>
        bool GetTradingStatus();

        /// <summary>
        /// Removes a market from the watch list.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnwatchMarket(string marketTicker);

        /// <summary>
        /// Marks a market as unhealthy, indicating it should not be traded.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the unhealthy market.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MarkMarketAsUnhealthyAsync(string marketTicker);

        /// <summary>
        /// Marks a market as healthy, indicating it can be traded again.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the healthy market.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MarkMarketAsHealthyAsync(string marketTicker);

        /// <summary>
        /// Updates market subscription for the specified action and market tickers.
        /// </summary>
        /// <param name="action">The subscription action (e.g., "subscribe" or "unsubscribe").</param>
        /// <param name="marketTickers">Array of market tickers to update subscription for.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateMarketSubscriptionAsync(string action, string[] marketTickers);

        /// <summary>
        /// Stops all market data services asynchronously.
        /// </summary>
        void StopServicesAsync();

        /// <summary>
        /// Synchronizes market data for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to synchronize.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SyncMarketDataAsync(string marketTicker);

        /// <summary>
        /// Ensures market data is available for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market.</param>
        /// <returns>The market DTO if successful, null otherwise.</returns>
        Task<MarketDTO?> EnsureMarketDataAsync(string marketTicker);

        /// <summary>
        /// Updates all watched markets with the latest data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateWatchedMarketsAsync();

        /// <summary>
        /// Fetches the list of currently watched market tickers.
        /// </summary>
        /// <returns>A list of watched market tickers.</returns>
        Task<List<string>> FetchWatchedMarketsAsync();

        /// <summary>
        /// Gets the current order book data for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market.</param>
        /// <returns>A list of order book data entries.</returns>
        List<OrderbookData> GetCurrentOrderBook(string marketTicker);

        /// <summary>
        /// Gets detailed market data for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market.</param>
        /// <returns>The market data interface for the specified market.</returns>
        IMarketData GetMarketDetails(string marketTicker);

        /// <summary>
        /// Gets detailed market data for multiple market tickers in batch.
        /// </summary>
        /// <param name="marketTickers">Collection of market tickers to retrieve data for.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary mapping market tickers to their market data.</returns>
        Task<Dictionary<string, IMarketData>> GetMarketDetailsBatchAsync(IEnumerable<string> marketTickers, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current account balance.
        /// </summary>
        /// <returns>The account balance as a double value.</returns>
        double GetAccountBalance();

        /// <summary>
        /// Gets the current portfolio value.
        /// </summary>
        /// <returns>The portfolio value as a double.</returns>
        double GetPortfolioValue();

        /// <summary>
        /// Gets the latest WebSocket timestamp.
        /// </summary>
        /// <returns>The latest WebSocket timestamp.</returns>
        DateTime GetLatestWebSocketTimestamp();

        /// <summary>
        /// Updates the account balance asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAccountBalanceAsync();

        /// <summary>
        /// Processes a ticker update with detailed market information.
        /// </summary>
        /// <param name="marketTicker">The market ticker symbol.</param>
        /// <param name="marketId">The unique market identifier.</param>
        /// <param name="price">The current price.</param>
        /// <param name="yesBid">The yes bid price.</param>
        /// <param name="yesAsk">The yes ask price.</param>
        /// <param name="volume">The trading volume.</param>
        /// <param name="openInterest">The open interest.</param>
        /// <param name="dollarVolume">The dollar volume.</param>
        /// <param name="dollarOpenInterest">The dollar open interest.</param>
        /// <param name="ts">The timestamp.</param>
        /// <param name="loggedDate">The logged date.</param>
        /// <param name="processedDate">Optional processed date.</param>
        void ProcessTickerUpdate(string marketTicker, Guid marketId, int price, int yesBid, int yesAsk, int volume, int openInterest, int dollarVolume, int dollarOpenInterest, long ts, DateTime loggedDate, DateTime? processedDate = null);

        /// <summary>
        /// Notifies listeners that market data has been updated for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker that was updated.</param>
        void NotifyMarketDataUpdated(string marketTicker);

        /// <summary>
        /// Notifies listeners that position data has been updated for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker that was updated.</param>
        void NotifyPositionDataUpdated(string marketTicker);

        /// <summary>
        /// Notifies listeners that a new ticker has been added.
        /// </summary>
        /// <param name="marketTicker">The market ticker that was added.</param>
        void NotifyTickerAdded(string marketTicker);

        /// <summary>
        /// Notifies listeners that the account balance has been updated.
        /// </summary>
        /// <param name="marketTicker">The market ticker associated with the update.</param>
        void NotifyAccountBalanceUpdated(string marketTicker);

        /// <summary>
        /// Performs forward fill operation on candlestick data for the specified market.
        /// </summary>
        /// <param name="candlesticks">The list of candlestick data to forward fill.</param>
        /// <param name="marketTicker">The market ticker for the candlesticks.</param>
        /// <returns>The forward-filled list of candlestick data.</returns>
        List<CandlestickData> ForwardFillCandlesticks(List<CandlestickData> candlesticks, string marketTicker);

        /// <summary>
        /// Configures WebSocket event handlers for real-time data updates.
        /// </summary>
        void ConfigureWebSocketEventHandlers();

        /// <summary>
        /// Retrieves and updates position data asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RetrieveAndUpdatePositionsAsync();

        /// <summary>
        /// Subscribes to market channels for the specified market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to subscribe to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SubscribeToMarketChannelsAsync(string marketTicker);
    }
}
