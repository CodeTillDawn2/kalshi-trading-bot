using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using System.Collections.Concurrent;

namespace SmokehouseBot.State.Interfaces
{


    public interface IMarketData
    {
        DateTime LastSuccessfulSync { get; set; }
        string MarketTicker { get; set; }
        string MarketType { get; set; }
        MarketDTO MarketInfo { get; set; }
        Dictionary<string, List<CandlestickData>> Candlesticks { get; }
        ConcurrentBag<TickerDTO> Tickers { get; set; }
        List<OrderbookData> OrderbookData { get; set; }
        DateTime LastWebSocketMessageReceived { get; set; }
        DateTime LastOrderbookEventTimestamp { get; set; }
        DateTime LastSnapshotTaken { get; set; }
        string CurrentPriceSource { get; }
        (int Ask, int Bid, DateTime When) TickerPriceYes { get; }
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
        TimeSpan? HoldTime { get; }
        TimeSpan? MarketAge { get; }
        TimeSpan? TimeLeft { get; }
        bool CanCloseEarly { get; }
        List<SupportResistanceLevel> AllSupportResistanceLevels { get; set; }
        List<OrderbookData> GetBids(string side = "");
        void UpdateCurrentPrice(int yesAsk, int yesBid, DateTime timestamp, string source);
        void RefreshAllMetadata();
        void RefreshCandlestickMetadata();
        void RefreshTickerMetadata();
        void RefreshPositionMetadata();
        void RecalculateOrderbookChangeMetrics();
        List<PseudoCandlestick> BuildPseudoCandlesticks(string period, int lookbackPeriods = 34);
        List<SupportResistanceLevel> GetFilteredSupportResistanceLevels();
        void UpdateTradingMetrics();

        double YesBidSlopePerMinute_Short { get; set; }
        double NoBidSlopePerMinute_Short { get; set; }
        double YesBidSlopePerMinute_Medium { get; set; }
        double NoBidSlopePerMinute_Medium { get; set; }

        public double? PSAR { get; set; }
        public double? ADX { get; set; }
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
