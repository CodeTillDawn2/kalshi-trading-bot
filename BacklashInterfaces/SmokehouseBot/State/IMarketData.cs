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
        (int Bid, DateTime When) AllTimeHighYes_Bid { get; set; }
        (int Bid, DateTime When) AllTimeHighNo_Bid { get; }
        (int Bid, DateTime When) AllTimeLowYes_Bid { get; set; }
        (int Bid, DateTime When) AllTimeLowNo_Bid { get; }
        (int Bid, DateTime When) RecentHighYes_Bid { get; set; }
        (int Bid, DateTime When) RecentHighNo_Bid { get; }
        (int Bid, DateTime When) RecentLowYes_Bid { get; set; }
        (int Bid, DateTime When) RecentLowNo_Bid { get; }
        string GoodBadPriceYes { get; set; }
        string GoodBadPriceNo { get; set; }
        string MarketBehaviorYes { get; set; }
        string MarketBehaviorNo { get; set; }
        List<MarketPositionDTO> Positions { get; set; }
        int PositionSize { get; }
        string PositionSide { get; }
        double MarketExposure { get; }
        double BuyinPrice { get; }
        double PositionUpside { get; }
        double PositionDownside { get; }
        double PositionROIAmt { get; }
        long TotalPositionTraded { get; }
        double RealizedPnl { get; }
        double FeesPaid { get; }
        List<OrderDTO> RestingOrders { get; set; }
        double PositionROI { get; }
        double ExpectedFees { get; set; }
        double TolerancePercentage { get; set; }
        IOrderbookChangeTracker ChangeTracker { get; }
        bool ChangeMetricsMature { get; }
        string MarketCategory { get; set; }
        string MarketStatus { get; set; }
        double AverageTradeSize_Yes { get; set; }
        double AverageTradeSize_No { get; set; }
        int TradeCount_Yes { get; set; }
        int TradeCount_No { get; set; }
        int NonTradeRelatedOrderCount_Yes { get; set; }
        int NonTradeRelatedOrderCount_No { get; set; }
        int LevelCount_Top_Yes_Bid { get; set; }
        int LevelCount_Top_No_Bid { get; set; }
        int LevelCount_Bottom_Yes_Bid { get; set; }
        int LevelCount_Bottom_No_Bid { get; set; }
        double VelocityPerMinute_Top_Yes_Bid { get; set; }
        double VelocityPerMinute_Top_No_Bid { get; set; }
        double VelocityPerMinute_Bottom_Yes_Bid { get; set; }
        double VelocityPerMinute_Bottom_No_Bid { get; set; }
        double TradeVolumePerMinute_Yes { get; set; }
        double TradeVolumePerMinute_No { get; set; }
        double TradeRatePerMinute_Yes { get; set; }
        double TradeRatePerMinute_No { get; set; }
        double OrderVolumePerMinute_YesBid { get; set; }
        double OrderVolumePerMinute_NoBid { get; set; }
        int BestYesBid { get; }
        int BestNoBid { get; }
        int BestYesAsk { get; }
        int BestNoAsk { get; }
        int YesSpread { get; }
        int NoSpread { get; }
        int DepthAtBestYesBid { get; }
        int DepthAtBestNoBid { get; }
        int TopTenPercentLevelDepth_Yes { get; }
        int TopTenPercentLevelDepth_No { get; }
        int BidRange_Yes { get; }
        int BidRange_No { get; }
        int TotalBidContracts_Yes { get; }
        int TotalBidContracts_No { get; }
        int BidCountImbalance { get; }
        double TotalBidVolume_Yes { get; }
        double TotalBidVolume_No { get; }
        double BidVolumeImbalance { get; }
        double HighestVolume_Day { get; set; }
        double HighestVolume_Hour { get; set; }
        double HighestVolume_Minute { get; set; }
        double RecentVolume_LastHour { get; set; }
        double RecentVolume_LastThreeHours { get; set; }
        double RecentVolume_LastMonth { get; set; }
        int DepthAtTop4YesBids { get; }
        int DepthAtTop4NoBids { get; }
        double YesBidCenterOfMass { get; }
        double NoBidCenterOfMass { get; }
        double? RSI_Short { get; set; }
        double? RSI_Medium { get; set; }
        double? RSI_Long { get; set; }
        (double? MACD, double? Signal, double? Histogram) MACD_Medium { get; set; }
        (double? MACD, double? Signal, double? Histogram) MACD_Long { get; set; }
        double? EMA_Medium { get; set; }
        double? EMA_Long { get; set; }
        (double? lower, double? middle, double? upper) BollingerBands_Medium { get; set; }
        (double? lower, double? middle, double? upper) BollingerBands_Long { get; set; }
        double? ATR_Medium { get; set; }
        double? ATR_Long { get; set; }
        double? VWAP_Short { get; set; }
        double? VWAP_Medium { get; set; }
        (double? K, double? D) StochasticOscillator_Short { get; set; }
        (double? K, double? D) StochasticOscillator_Medium { get; set; }
        (double? K, double? D) StochasticOscillator_Long { get; set; }
        long OBV_Medium { get; set; }
        long OBV_Long { get; set; }
        bool ReceivedFirstSnapshot { get; set; }
        bool WebSocketHealthy { get; set; }
        TimeSpan? HoldTime { get; }
        TimeSpan? MarketAge { get; }
        TimeSpan? TimeLeft { get; }
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

        double YesBidSlopePerMinute_Short { get; set; }
        double NoBidSlopePerMinute_Short { get; set; }
        double YesBidSlopePerMinute_Medium { get; set; }
        double NoBidSlopePerMinute_Medium { get; set; }

        public double? PSAR { get; set; }
        public double? ADX { get; set; }
        public double? PlusDI { get; set; }
        public double? MinusDI { get; set; }
        double CurrentTradeRatePerMinute_Yes { get; set; }
        double CurrentTradeRatePerMinute_No { get; set; }
        double CurrentTradeVolumePerMinute_No { get; set; }
        double CurrentTradeVolumePerMinute_Yes { get; set; }
        double CurrentTradeCount_Yes { get; set; }
        double CurrentTradeCount_No { get; set; }
        double CurrentOrderVolumePerMinute_YesBid { get; set; }
        double CurrentOrderVolumePerMinute_NoBid { get; set; }
        double CurrentNonTradeRelatedOrderCount_Yes { get; set; }
        double CurrentNonTradeRelatedOrderCount_No { get; set; }
        double CurrentAverageTradeSize_Yes { get; set; }
        double CurrentAverageTradeSize_No { get; set; }
        List<PseudoCandlestick> RecentCandlesticks { get; }
    }
}
