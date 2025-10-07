using BacklashBot.State.Interfaces;
using BacklashDTOs;

namespace BacklashBot.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that tracks and analyzes order book changes,
    /// providing metrics and insights into market dynamics and trading activity.
    /// </summary>
    public interface IOrderbookChangeTracker : IDisposable
    {
        /// <summary>
        /// Occurs when a market is determined to be invalid.
        /// </summary>
        event EventHandler<string> MarketInvalid;

        /// <summary>
        /// Gets the market data associated with this tracker.
        /// </summary>
        IMarketData Market { get; }

        /// <summary>
        /// Gets a value indicating whether the tracker has sufficient data to provide mature metrics.
        /// </summary>
        bool IsMature { get; }

        /// <summary>
        /// Gets the duration of the change tracking window.
        /// </summary>
        TimeSpan ChangeWindowDuration { get; }

        /// <summary>
        /// Gets the window duration for matching trades.
        /// </summary>
        TimeSpan TradeMatchingWindow { get; }

        /// <summary>
        /// Gets the window duration for order book cancellations.
        /// </summary>
        TimeSpan OrderbookCancelWindow { get; }

        /// <summary>
        /// Gets or sets the timestamp of the last market open time.
        /// </summary>
        DateTime LastMarketOpenTime { get; set; }

        /// <summary>
        /// Updates the market status with exchange and trading activity information.
        /// </summary>
        /// <param name="isExchangeActive">Whether the exchange is currently active.</param>
        /// <param name="isTradingActive">Whether trading is currently active.</param>
        void UpdateMarketStatus(bool isExchangeActive, bool isTradingActive);

        /// <summary>
        /// Recalculates all metrics based on current order book data.
        /// </summary>
        void RecalculateAllMetrics();

        /// <summary>
        /// Stops the order book change tracking operations.
        /// </summary>
        void Stop();

        /// <summary>
        /// Processes the order book snapshot to detect changes and update metrics.
        /// </summary>
        /// <param name="originalOrderbook">The original order book data.</param>
        /// <param name="newOrderbook">The new order book data.</param>
        void ProcessOrderbookSnapshot(List<OrderbookData> originalOrderbook, List<OrderbookData> newOrderbook);

        /// <summary>
        /// Records a change in the order book for a specific side and price.
        /// </summary>
        /// <param name="side">The side of the order book (buy/sell).</param>
        /// <param name="price">The price level of the change.</param>
        /// <param name="deltaContracts">The change in number of contracts.</param>
        void RecordOrderbookChange(string side, int price, int deltaContracts);

        /// <summary>
        /// Records a trade event with details.
        /// </summary>
        /// <param name="takerSide">The side of the taker in the trade.</param>
        /// <param name="yesPrice">The yes price of the trade.</param>
        /// <param name="noPrice">The no price of the trade.</param>
        /// <param name="count">The number of contracts traded.</param>
        /// <param name="timestamp">The timestamp of the trade.</param>
        void RecordTrade(string takerSide, int yesPrice, int noPrice, int count, DateTime timestamp);

        /// <summary>
        /// Gets the bottom no velocity per minute metrics.
        /// </summary>
        /// <param name="noBids">The list of no bids.</param>
        /// <param name="orderbookChanges">The list of order book changes.</param>
        /// <param name="threshold">The threshold for calculation.</param>
        /// <returns>A tuple containing volume and levels.</returns>
        (double Volume, int Levels) GetBottomNoVelocityPerMinute(List<OrderbookData> noBids, List<OrderbookChange> orderbookChanges, int threshold);

        /// <summary>
        /// Shuts down the order book change tracker and releases resources.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Gets the bottom yes velocity per minute metrics.
        /// </summary>
        /// <param name="yesBids">The list of yes bids.</param>
        /// <param name="orderbookChanges">The list of order book changes.</param>
        /// <param name="threshold">The threshold for calculation.</param>
        /// <returns>A tuple containing volume and levels.</returns>
        (double Volume, int Levels) GetBottomYesVelocityPerMinute(List<OrderbookData> yesBids, List<OrderbookChange> orderbookChanges, int threshold);

        /// <summary>
        /// Gets the top no velocity per minute metrics.
        /// </summary>
        /// <param name="noBids">The list of no bids.</param>
        /// <param name="orderbookChanges">The list of order book changes.</param>
        /// <param name="threshold">The threshold for calculation.</param>
        /// <returns>A tuple containing volume and levels.</returns>
        (double Volume, int Levels) GetTopNoVelocityPerMinute(List<OrderbookData> noBids, List<OrderbookChange> orderbookChanges, int threshold);

        /// <summary>
        /// Gets the top yes velocity per minute metrics.
        /// </summary>
        /// <param name="yesBids">The list of yes bids.</param>
        /// <param name="orderbookChanges">The list of order book changes.</param>
        /// <param name="threshold">The threshold for calculation.</param>
        /// <returns>A tuple containing volume and levels.</returns>
        (double Volume, int Levels) GetTopYesVelocityPerMinute(List<OrderbookData> yesBids, List<OrderbookChange> orderbookChanges, int threshold);

        /// <summary>
        /// Gets the net order volume per minute for yes bids.
        /// </summary>
        /// <param name="yesBidChanges">The list of yes bid changes.</param>
        /// <returns>A tuple containing volume and count.</returns>
        (double Volume, int Count) GetYesNetOrderVolumePerMinute(List<OrderbookChange> yesBidChanges);

        /// <summary>
        /// Gets the net order volume per minute for no bids.
        /// </summary>
        /// <param name="noBidChanges">The list of no bid changes.</param>
        /// <returns>A tuple containing volume and count.</returns>
        (double Volume, int Count) GetNoNetOrderVolumePerMinute(List<OrderbookChange> noBidChanges);

        /// <summary>
        /// Gets the trade rate per minute for maker yes trades.
        /// </summary>
        /// <param name="yesTradeRelatedChanges">The list of yes trade related changes.</param>
        /// <returns>A tuple containing rate and volume.</returns>
        (double rate, double volume) GetTradeRatePerMinute_MakerYes(List<OrderbookChange> yesTradeRelatedChanges);

        /// <summary>
        /// Gets the trade rate per minute for maker no trades.
        /// </summary>
        /// <param name="noTradeRelatedChanges">The list of no trade related changes.</param>
        /// <returns>A tuple containing rate and volume.</returns>
        (double rate, double volume) GetTradeRatePerMinute_MakerNo(List<OrderbookChange> noTradeRelatedChanges);

        /// <summary>
        /// Gets the average trade size for maker yes trades.
        /// </summary>
        /// <param name="tradeEvents">The list of trade events.</param>
        /// <returns>The average trade size.</returns>
        double GetAverageTradeSize_MakerYes(List<OrderbookChange> tradeEvents);

        /// <summary>
        /// Gets the average trade size for maker no trades.
        /// </summary>
        /// <param name="tradeEvents">The list of trade events.</param>
        /// <returns>The average trade size.</returns>
        double GetAverageTradeSize_MakerNo(List<OrderbookChange> tradeEvents);

        /// <summary>
        /// Gets the trade count for maker yes trades.
        /// </summary>
        /// <returns>The number of maker yes trades.</returns>
        int GetTradeCount_MakerYes();

        /// <summary>
        /// Gets the trade count for maker no trades.
        /// </summary>
        /// <returns>The number of maker no trades.</returns>
        int GetTradeCount_MakerNo();
        /// <summary>
        /// Gets the number of top levels in the bids above a lower bound.
        /// </summary>
        /// <param name="Bids">The list of bid data.</param>
        /// <param name="lowerBound">The lower bound for counting levels.</param>
        /// <returns>The number of top levels.</returns>
        int GetTopLevels(List<OrderbookData> Bids, int lowerBound);

        /// <summary>
        /// Gets the number of bottom levels in the bids below an upper bound.
        /// </summary>
        /// <param name="Bids">The list of bid data.</param>
        /// <param name="upperBound">The upper bound for counting levels.</param>
        /// <returns>The number of bottom levels.</returns>
        int GetBottomLevels(List<OrderbookData> Bids, int upperBound);
    }
}
