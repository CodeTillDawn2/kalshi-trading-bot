using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using System.Collections.Concurrent;

namespace BacklashBot.State.Interfaces
{
    /// <summary>
    /// Defines the comprehensive contract for market data representation, containing
    /// all trading-related information, metrics, and analysis data for a specific market.
    /// This interface provides access to real-time and historical market data, positions,
    /// technical indicators, and trading metrics.
    /// </summary>
    public interface IMarketData
    {
        /// <summary>
        /// Gets or sets the timestamp of the last successful data synchronization.
        /// </summary>
        DateTime LastSuccessfulSync { get; set; }

        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        string MarketTicker { get; set; }

        /// <summary>
        /// Gets or sets the type of market (e.g., binary, multi-outcome).
        /// </summary>
        string MarketType { get; set; }

        /// <summary>
        /// Gets or sets the detailed market information DTO.
        /// </summary>
        MarketDTO MarketInfo { get; set; }

        /// <summary>
        /// Gets the dictionary of candlestick data organized by timeframe.
        /// </summary>
        Dictionary<string, List<CandlestickData>> Candlesticks { get; }

        /// <summary>
        /// Gets or sets the concurrent bag of ticker data updates.
        /// </summary>
        ConcurrentBag<TickerDTO> Tickers { get; set; }

        /// <summary>
        /// Gets or sets the list of order book data entries.
        /// </summary>
        List<OrderbookData> OrderbookData { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last WebSocket message received.
        /// </summary>
        DateTime LastWebSocketMessageReceived { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last order book event.
        /// </summary>
        DateTime LastOrderbookEventTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the last snapshot was taken.
        /// </summary>
        DateTime LastSnapshotTaken { get; set; }

        /// <summary>
        /// Gets the source of the current price data.
        /// </summary>
        string CurrentPriceSource { get; }

        /// <summary>
        /// Gets the current ticker price information for the "Yes" side.
        /// </summary>
        (int Ask, int Bid, DateTime When) TickerPriceYes { get; }

        /// <summary>
        /// Gets the current ticker price information for the "No" side.
        /// </summary>
        (int Ask, int Bid, DateTime When) TickerPriceNo { get; }

        /// <summary>
        /// Gets or sets the all-time high bid price for the "Yes" side with timestamp.
        /// </summary>
        (int Bid, DateTime When) AllTimeHighYes_Bid { get; set; }

        /// <summary>
        /// Gets the all-time high bid price for the "No" side with timestamp.
        /// </summary>
        (int Bid, DateTime When) AllTimeHighNo_Bid { get; }

        /// <summary>
        /// Gets or sets the all-time low bid price for the "Yes" side with timestamp.
        /// </summary>
        (int Bid, DateTime When) AllTimeLowYes_Bid { get; set; }

        /// <summary>
        /// Gets the all-time low bid price for the "No" side with timestamp.
        /// </summary>
        (int Bid, DateTime When) AllTimeLowNo_Bid { get; }

        /// <summary>
        /// Gets or sets the recent high bid price for the "Yes" side with timestamp.
        /// </summary>
        (int Bid, DateTime When) RecentHighYes_Bid { get; set; }

        /// <summary>
        /// Gets the recent high bid price for the "No" side with timestamp.
        /// </summary>
        (int Bid, DateTime When) RecentHighNo_Bid { get; }

        /// <summary>
        /// Gets or sets the recent low bid price for the "Yes" side with timestamp.
        /// </summary>
        (int Bid, DateTime When) RecentLowYes_Bid { get; set; }

        /// <summary>
        /// Gets the recent low bid price for the "No" side with timestamp.
        /// </summary>
        (int Bid, DateTime When) RecentLowNo_Bid { get; }

        /// <summary>
        /// Gets or sets the good/bad price indicator for the "Yes" side.
        /// </summary>
        string GoodBadPriceYes { get; set; }

        /// <summary>
        /// Gets or sets the good/bad price indicator for the "No" side.
        /// </summary>
        string GoodBadPriceNo { get; set; }

        /// <summary>
        /// Gets or sets the market behavior indicator for the "Yes" side.
        /// </summary>
        string MarketBehaviorYes { get; set; }

        /// <summary>
        /// Gets or sets the market behavior indicator for the "No" side.
        /// </summary>
        string MarketBehaviorNo { get; set; }

        /// <summary>
        /// Gets or sets the list of market positions.
        /// </summary>
        List<MarketPositionDTO> Positions { get; set; }

        /// <summary>
        /// Gets the total size of all positions.
        /// </summary>
        int PositionSize { get; }

        /// <summary>
        /// Gets the side of the position ("Yes", "No", or "Both").
        /// </summary>
        string PositionSide { get; }

        /// <summary>
        /// Gets the market exposure amount.
        /// </summary>
        double MarketExposure { get; }

        /// <summary>
        /// Gets the average buy-in price for positions.
        /// </summary>
        double BuyinPrice { get; }

        /// <summary>
        /// Gets the potential upside of the position.
        /// </summary>
        double PositionUpside { get; }

        /// <summary>
        /// Gets the potential downside of the position.
        /// </summary>
        double PositionDownside { get; }

        /// <summary>
        /// Gets the return on investment amount for the position.
        /// </summary>
        double PositionROIAmt { get; }

        /// <summary>
        /// Gets the total volume traded for all positions.
        /// </summary>
        long TotalPositionTraded { get; }

        /// <summary>
        /// Gets the realized profit and loss for positions.
        /// </summary>
        double RealizedPnl { get; }

        /// <summary>
        /// Gets the total fees paid for positions.
        /// </summary>
        double FeesPaid { get; }

        /// <summary>
        /// Gets or sets the list of resting orders.
        /// </summary>
        List<OrderDTO> RestingOrders { get; set; }

        /// <summary>
        /// Gets the return on investment percentage for the position.
        /// </summary>
        double PositionROI { get; }

        /// <summary>
        /// Gets or sets the expected fees for future trades.
        /// </summary>
        double ExpectedFees { get; set; }

        /// <summary>
        /// Gets or sets the tolerance percentage for price movements.
        /// </summary>
        double TolerancePercentage { get; set; }

        /// <summary>
        /// Gets the order book change tracker.
        /// </summary>
        IOrderbookChangeTracker ChangeTracker { get; }

        /// <summary>
        /// Gets whether the change metrics are mature and reliable.
        /// </summary>
        bool ChangeMetricsMature { get; }

        /// <summary>
        /// Gets or sets the market category.
        /// </summary>
        string MarketCategory { get; set; }

        /// <summary>
        /// Gets or sets the market status.
        /// </summary>
        string MarketStatus { get; set; }

        /// <summary>
        /// Gets or sets the average trade size for the "Yes" side.
        /// </summary>
        double AverageTradeSize_Yes { get; set; }

        /// <summary>
        /// Gets or sets the average trade size for the "No" side.
        /// </summary>
        double AverageTradeSize_No { get; set; }

        /// <summary>
        /// Gets or sets the trade count for the "Yes" side.
        /// </summary>
        int TradeCount_Yes { get; set; }

        /// <summary>
        /// Gets or sets the trade count for the "No" side.
        /// </summary>
        int TradeCount_No { get; set; }

        /// <summary>
        /// Gets or sets the count of non-trade related orders for the "Yes" side.
        /// </summary>
        int NonTradeRelatedOrderCount_Yes { get; set; }

        /// <summary>
        /// Gets or sets the count of non-trade related orders for the "No" side.
        /// </summary>
        int NonTradeRelatedOrderCount_No { get; set; }

        /// <summary>
        /// Gets or sets the level count at the top of the "Yes" bid.
        /// </summary>
        int LevelCount_Top_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the level count at the top of the "No" bid.
        /// </summary>
        int LevelCount_Top_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the level count at the bottom of the "Yes" bid.
        /// </summary>
        int LevelCount_Bottom_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the level count at the bottom of the "No" bid.
        /// </summary>
        int LevelCount_Bottom_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity per minute at the top of the "Yes" bid.
        /// </summary>
        double VelocityPerMinute_Top_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity per minute at the top of the "No" bid.
        /// </summary>
        double VelocityPerMinute_Top_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity per minute at the bottom of the "Yes" bid.
        /// </summary>
        double VelocityPerMinute_Bottom_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity per minute at the bottom of the "No" bid.
        /// </summary>
        double VelocityPerMinute_Bottom_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the trade volume per minute for the "Yes" side.
        /// </summary>
        double TradeVolumePerMinute_Yes { get; set; }

        /// <summary>
        /// Gets or sets the trade volume per minute for the "No" side.
        /// </summary>
        double TradeVolumePerMinute_No { get; set; }

        /// <summary>
        /// Gets or sets the trade rate per minute for the "Yes" side.
        /// </summary>
        double TradeRatePerMinute_Yes { get; set; }

        /// <summary>
        /// Gets or sets the trade rate per minute for the "No" side.
        /// </summary>
        double TradeRatePerMinute_No { get; set; }

        /// <summary>
        /// Gets or sets the order volume per minute for "Yes" bids.
        /// </summary>
        double OrderVolumePerMinute_YesBid { get; set; }

        /// <summary>
        /// Gets or sets the order volume per minute for "No" bids.
        /// </summary>
        double OrderVolumePerMinute_NoBid { get; set; }

        /// <summary>
        /// Gets the best bid price for the "Yes" side.
        /// </summary>
        int BestYesBid { get; }

        /// <summary>
        /// Gets the best bid price for the "No" side.
        /// </summary>
        int BestNoBid { get; }

        /// <summary>
        /// Gets the best ask price for the "Yes" side.
        /// </summary>
        int BestYesAsk { get; }

        /// <summary>
        /// Gets the best ask price for the "No" side.
        /// </summary>
        int BestNoAsk { get; }

        /// <summary>
        /// Gets the spread for the "Yes" side.
        /// </summary>
        int YesSpread { get; }

        /// <summary>
        /// Gets the spread for the "No" side.
        /// </summary>
        int NoSpread { get; }

        /// <summary>
        /// Gets the depth at the best "Yes" bid.
        /// </summary>
        int DepthAtBestYesBid { get; }

        /// <summary>
        /// Gets the depth at the best "No" bid.
        /// </summary>
        int DepthAtBestNoBid { get; }

        /// <summary>
        /// Gets the depth at the top 10% level for the "Yes" side.
        /// </summary>
        int TopTenPercentLevelDepth_Yes { get; }

        /// <summary>
        /// Gets the depth at the top 10% level for the "No" side.
        /// </summary>
        int TopTenPercentLevelDepth_No { get; }

        /// <summary>
        /// Gets the bid range for the "Yes" side.
        /// </summary>
        int BidRange_Yes { get; }

        /// <summary>
        /// Gets the bid range for the "No" side.
        /// </summary>
        int BidRange_No { get; }

        /// <summary>
        /// Gets the total bid contracts for the "Yes" side.
        /// </summary>
        int TotalBidContracts_Yes { get; }

        /// <summary>
        /// Gets the total bid contracts for the "No" side.
        /// </summary>
        int TotalBidContracts_No { get; }

        /// <summary>
        /// Gets the bid count imbalance between sides.
        /// </summary>
        int BidCountImbalance { get; }

        /// <summary>
        /// Gets the total bid volume for the "Yes" side.
        /// </summary>
        double TotalBidVolume_Yes { get; }

        /// <summary>
        /// Gets the total bid volume for the "No" side.
        /// </summary>
        double TotalBidVolume_No { get; }

        /// <summary>
        /// Gets the bid volume imbalance between sides.
        /// </summary>
        double BidVolumeImbalance { get; }

        /// <summary>
        /// Gets or sets the highest volume for the day.
        /// </summary>
        double HighestVolume_Day { get; set; }

        /// <summary>
        /// Gets or sets the highest volume for the hour.
        /// </summary>
        double HighestVolume_Hour { get; set; }

        /// <summary>
        /// Gets or sets the highest volume for the minute.
        /// </summary>
        double HighestVolume_Minute { get; set; }

        /// <summary>
        /// Gets or sets the recent volume for the last hour.
        /// </summary>
        double RecentVolume_LastHour { get; set; }

        /// <summary>
        /// Gets or sets the recent volume for the last three hours.
        /// </summary>
        double RecentVolume_LastThreeHours { get; set; }

        /// <summary>
        /// Gets or sets the recent volume for the last month.
        /// </summary>
        double RecentVolume_LastMonth { get; set; }

        /// <summary>
        /// Gets the depth at the top 4 "Yes" bids.
        /// </summary>
        int DepthAtTop4YesBids { get; }

        /// <summary>
        /// Gets the depth at the top 4 "No" bids.
        /// </summary>
        int DepthAtTop4NoBids { get; }

        /// <summary>
        /// Gets the center of mass for "Yes" bids.
        /// </summary>
        double YesBidCenterOfMass { get; }

        /// <summary>
        /// Gets the center of mass for "No" bids.
        /// </summary>
        double NoBidCenterOfMass { get; }

        /// <summary>
        /// Gets or sets the Relative Strength Index (RSI) for short timeframe.
        /// </summary>
        double? RSI_Short { get; set; }

        /// <summary>
        /// Gets or sets the Relative Strength Index (RSI) for medium timeframe.
        /// </summary>
        double? RSI_Medium { get; set; }

        /// <summary>
        /// Gets or sets the Relative Strength Index (RSI) for long timeframe.
        /// </summary>
        double? RSI_Long { get; set; }

        /// <summary>
        /// Gets or sets the Moving Average Convergence Divergence (MACD) for medium timeframe.
        /// </summary>
        (double? MACD, double? Signal, double? Histogram) MACD_Medium { get; set; }

        /// <summary>
        /// Gets or sets the Moving Average Convergence Divergence (MACD) for long timeframe.
        /// </summary>
        (double? MACD, double? Signal, double? Histogram) MACD_Long { get; set; }

        /// <summary>
        /// Gets or sets the Exponential Moving Average (EMA) for medium timeframe.
        /// </summary>
        double? EMA_Medium { get; set; }

        /// <summary>
        /// Gets or sets the Exponential Moving Average (EMA) for long timeframe.
        /// </summary>
        double? EMA_Long { get; set; }

        /// <summary>
        /// Gets or sets the Bollinger Bands for medium timeframe.
        /// </summary>
        (double? lower, double? middle, double? upper) BollingerBands_Medium { get; set; }

        /// <summary>
        /// Gets or sets the Bollinger Bands for long timeframe.
        /// </summary>
        (double? lower, double? middle, double? upper) BollingerBands_Long { get; set; }

        /// <summary>
        /// Gets or sets the Average True Range (ATR) for medium timeframe.
        /// </summary>
        double? ATR_Medium { get; set; }

        /// <summary>
        /// Gets or sets the Average True Range (ATR) for long timeframe.
        /// </summary>
        double? ATR_Long { get; set; }

        /// <summary>
        /// Gets or sets the Volume Weighted Average Price (VWAP) for short timeframe.
        /// </summary>
        double? VWAP_Short { get; set; }

        /// <summary>
        /// Gets or sets the Volume Weighted Average Price (VWAP) for medium timeframe.
        /// </summary>
        double? VWAP_Medium { get; set; }

        /// <summary>
        /// Gets or sets the Stochastic Oscillator for short timeframe.
        /// </summary>
        (double? K, double? D) StochasticOscillator_Short { get; set; }

        /// <summary>
        /// Gets or sets the Stochastic Oscillator for medium timeframe.
        /// </summary>
        (double? K, double? D) StochasticOscillator_Medium { get; set; }

        /// <summary>
        /// Gets or sets the Stochastic Oscillator for long timeframe.
        /// </summary>
        (double? K, double? D) StochasticOscillator_Long { get; set; }

        /// <summary>
        /// Gets or sets the On Balance Volume (OBV) for medium timeframe.
        /// </summary>
        long OBV_Medium { get; set; }

        /// <summary>
        /// Gets or sets the On Balance Volume (OBV) for long timeframe.
        /// </summary>
        long OBV_Long { get; set; }

        /// <summary>
        /// Gets or sets whether the first snapshot has been received.
        /// </summary>
        bool ReceivedFirstSnapshot { get; set; }

        /// <summary>
        /// Gets or sets whether the WebSocket connection is healthy.
        /// </summary>
        bool WebSocketHealthy { get; set; }

        /// <summary>
        /// Gets the hold time for positions.
        /// </summary>
        TimeSpan? HoldTime { get; }

        /// <summary>
        /// Gets the age of the market.
        /// </summary>
        TimeSpan? MarketAge { get; }

        /// <summary>
        /// Gets the time left until market expiration.
        /// </summary>
        TimeSpan? TimeLeft { get; }

        /// <summary>
        /// Gets whether the market can be closed early.
        /// </summary>
        bool CanCloseEarly { get; }

        /// <summary>
        /// Gets or sets the list of all support and resistance levels for the market.
        /// </summary>
        List<SupportResistanceLevel> AllSupportResistanceLevels { get; set; }

        /// <summary>
        /// Gets the bid order book data for the specified side.
        /// </summary>
        /// <param name="side">The side to get bids for ("Yes" or "No"). Defaults to empty string for all.</param>
        /// <returns>A list of order book data entries for bids.</returns>
        List<OrderbookData> GetBids(string side = "");

        /// <summary>
        /// Updates the current price with the specified values.
        /// </summary>
        /// <param name="yesAsk">The ask price for the "Yes" side.</param>
        /// <param name="yesBid">The bid price for the "Yes" side.</param>
        /// <param name="timestamp">The timestamp of the price update.</param>
        /// <param name="source">The source of the price data.</param>
        void UpdateCurrentPrice(int yesAsk, int yesBid, DateTime timestamp, string source);

        /// <summary>
        /// Refreshes all metadata for the market asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RefreshAllMetadata();

        /// <summary>
        /// Refreshes candlestick metadata.
        /// </summary>
        void RefreshCandlestickMetadata();

        /// <summary>
        /// Refreshes ticker metadata asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RefreshTickerMetadata();

        /// <summary>
        /// Refreshes position metadata.
        /// </summary>
        void RefreshPositionMetadata();

        /// <summary>
        /// Recalculates order book change metrics.
        /// </summary>
        void RecalculateOrderbookChangeMetrics();

        /// <summary>
        /// Builds pseudo candlesticks for the specified period and lookback.
        /// </summary>
        /// <param name="period">The period for the pseudo candlesticks.</param>
        /// <param name="lookbackPeriods">The number of lookback periods (default 34).</param>
        /// <returns>A list of pseudo candlesticks.</returns>
        Task<List<PseudoCandlestick>> BuildPseudoCandlesticks(string period, int lookbackPeriods = 34);

        /// <summary>
        /// Gets the filtered list of support and resistance levels.
        /// </summary>
        /// <returns>A list of filtered support and resistance levels.</returns>
        List<SupportResistanceLevel> GetFilteredSupportResistanceLevels();

        /// <summary>
        /// Updates trading metrics asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateTradingMetrics();

        /// <summary>
        /// Gets or sets the slope per minute for "Yes" bids (short timeframe).
        /// </summary>
        double YesBidSlopePerMinute_Short { get; set; }

        /// <summary>
        /// Gets or sets the slope per minute for "No" bids (short timeframe).
        /// </summary>
        double NoBidSlopePerMinute_Short { get; set; }

        /// <summary>
        /// Gets or sets the slope per minute for "Yes" bids (medium timeframe).
        /// </summary>
        double YesBidSlopePerMinute_Medium { get; set; }

        /// <summary>
        /// Gets or sets the slope per minute for "No" bids (medium timeframe).
        /// </summary>
        double NoBidSlopePerMinute_Medium { get; set; }

        /// <summary>
        /// Gets or sets the Parabolic SAR indicator value.
        /// </summary>
        public double? PSAR { get; set; }

        /// <summary>
        /// Gets or sets the Average Directional Index (ADX) value.
        /// </summary>
        public double? ADX { get; set; }

        /// <summary>
        /// Gets or sets the Plus Directional Indicator (+DI) value.
        /// </summary>
        public double? PlusDI { get; set; }

        /// <summary>
        /// Gets or sets the Minus Directional Indicator (-DI) value.
        /// </summary>
        public double? MinusDI { get; set; }

        /// <summary>
        /// Gets or sets the current trade rate per minute for the "Yes" side.
        /// </summary>
        double CurrentTradeRatePerMinute_Yes { get; set; }

        /// <summary>
        /// Gets or sets the current trade rate per minute for the "No" side.
        /// </summary>
        double CurrentTradeRatePerMinute_No { get; set; }

        /// <summary>
        /// Gets or sets the current trade volume per minute for the "No" side.
        /// </summary>
        double CurrentTradeVolumePerMinute_No { get; set; }

        /// <summary>
        /// Gets or sets the current trade volume per minute for the "Yes" side.
        /// </summary>
        double CurrentTradeVolumePerMinute_Yes { get; set; }

        /// <summary>
        /// Gets or sets the current trade count for the "Yes" side.
        /// </summary>
        double CurrentTradeCount_Yes { get; set; }

        /// <summary>
        /// Gets or sets the current trade count for the "No" side.
        /// </summary>
        double CurrentTradeCount_No { get; set; }

        /// <summary>
        /// Gets or sets the current order volume per minute for "Yes" bids.
        /// </summary>
        double CurrentOrderVolumePerMinute_YesBid { get; set; }

        /// <summary>
        /// Gets or sets the current order volume per minute for "No" bids.
        /// </summary>
        double CurrentOrderVolumePerMinute_NoBid { get; set; }

        /// <summary>
        /// Gets or sets the current count of non-trade related orders for the "Yes" side.
        /// </summary>
        double CurrentNonTradeRelatedOrderCount_Yes { get; set; }

        /// <summary>
        /// Gets or sets the current count of non-trade related orders for the "No" side.
        /// </summary>
        double CurrentNonTradeRelatedOrderCount_No { get; set; }

        /// <summary>
        /// Gets or sets the current average trade size for the "Yes" side.
        /// </summary>
        double CurrentAverageTradeSize_Yes { get; set; }

        /// <summary>
        /// Gets or sets the current average trade size for the "No" side.
        /// </summary>
        double CurrentAverageTradeSize_No { get; set; }

        /// <summary>
        /// Gets the list of recent pseudo candlesticks.
        /// </summary>
        List<PseudoCandlestick> RecentCandlesticks { get; }

        /// <summary>
        /// Cancels all ongoing operations for this market data instance.
        /// Used when the market is being removed from watch list to prevent further processing.
        /// </summary>
        void CancelOperations();
    }
}
