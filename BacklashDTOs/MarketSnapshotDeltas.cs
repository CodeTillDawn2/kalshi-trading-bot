using System.Reflection;

namespace BacklashDTOs
{
    /// <summary>
    /// Represents the delta between two MarketSnapshot instances, tracking which properties have changed.
    /// </summary>
    public sealed class MarketSnapshotDelta
    {
        private readonly MarketSnapshot _previous;
        /// <summary>
        /// Initializes a new instance of the MarketSnapshotDelta class.
        /// </summary>
        /// <param name="previous">The previous MarketSnapshot.</param>
        /// <param name="current">The current MarketSnapshot.</param>
        private readonly MarketSnapshot _current;
        private readonly HashSet<string> _changed;

        public MarketSnapshotDelta(MarketSnapshot previous, MarketSnapshot current)
        {
            _previous = previous ?? throw new ArgumentNullException(nameof(previous));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            // Compute diffs once and cache changed paths
            var diffs = previous.Diff(current);
            _changed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var d in diffs)
            {
                // Pull the path string from the Difference object (property name may vary; pick first string prop)
                var pathProp = d.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .FirstOrDefault(p => p.PropertyType == typeof(string));
                var path = (string?)pathProp?.GetValue(d);
                if (!string.IsNullOrEmpty(path))
                    _changed.Add(path!);
            }
        }

        private bool Changed(string path) => _changed.Contains(path);

        // ValueTuple elements may surface as either their friendly name (e.g., "Bid")
        // or "Item{n}" depending on the runtime. Handle both.
        private bool ChangedTuple(string prop, int index1Based, string named)
        {
            return _changed.Contains(prop + "." + named) || _changed.Contains(prop + ".Item" + index1Based.ToString());
        }

        /// <summary>
        /// Gets the timestamp if it has changed, otherwise null.
        /// </summary>
        /// <returns>The timestamp or null if unchanged.</returns>
        public DateTime? Timestamp() => Changed("Timestamp") ? _current.Timestamp : (DateTime?)null;
        public string? MarketTicker() => Changed("MarketTicker") ? _current.MarketTicker : null;
        public string? MarketCategory() => Changed("MarketCategory") ? _current.MarketCategory : null;
        public string? MarketStatus() => Changed("MarketStatus") ? _current.MarketStatus : null;
        public string? MarketType() => Changed("MarketType") ? _current.MarketType : null;
        public int? SnapshotSchemaVersion() => Changed("SnapshotSchemaVersion") ? _current.SnapshotSchemaVersion : (int?)null;

        public double? YesBidSlopePerMinute_Short() => Changed("YesBidSlopePerMinute_Short") ? _current.YesBidSlopePerMinute_Short : (double?)null;
        public double? NoBidSlopePerMinute_Short() => Changed("NoBidSlopePerMinute_Short") ? _current.NoBidSlopePerMinute_Short : (double?)null;

        public double? YesBidSlopePerMinute_Medium() => Changed("YesBidSlopePerMinute_Medium") ? _current.YesBidSlopePerMinute_Medium : (double?)null;
        public double? NoBidSlopePerMinute_Medium() => Changed("NoBidSlopePerMinute_Medium") ? _current.NoBidSlopePerMinute_Medium : (double?)null;
        public long? TotalOrderbookDepth_Yes() => Changed("TotalOrderbookDepth_Yes") ? _current.TotalOrderbookDepth_Yes : (long?)null;
        public long? TotalOrderbookDepth_No() => Changed("TotalOrderbookDepth_No") ? _current.TotalOrderbookDepth_No : (long?)null;

        public int? PositionSize() => Changed("PositionSize") ? _current.PositionSize : (int?)null;
        public double? MarketExposure() => Changed("MarketExposure") ? _current.MarketExposure : (double?)null;
        public double? BuyinPrice() => Changed("BuyinPrice") ? _current.BuyinPrice : (double?)null;
        public double? PositionUpside() => Changed("PositionUpside") ? _current.PositionUpside : (double?)null;
        public double? PositionDownside() => Changed("PositionDownside") ? _current.PositionDownside : (double?)null;
        public long? TotalTraded() => Changed("TotalTraded") ? _current.TotalTraded : (long?)null;
        public double? RealizedPnl() => Changed("RealizedPnl") ? _current.RealizedPnl : (double?)null;
        public double? FeesPaid() => Changed("FeesPaid") ? _current.FeesPaid : (double?)null;
        public double? PositionROI() => Changed("PositionROI") ? _current.PositionROI : (double?)null;
        public double? PositionROIAmt() => Changed("PositionROIAmt") ? _current.PositionROIAmt : (double?)null;
        public double? ExpectedFees() => Changed("ExpectedFees") ? _current.ExpectedFees : (double?)null;

        public double? ADX() => Changed("ADX") ? _current.ADX : (double?)null;
        public double? PlusDI() => Changed("PlusDI") ? _current.PlusDI : (double?)null;
        public double? MinusDI() => Changed("MinusDI") ? _current.MinusDI : (double?)null;
        public double? PSAR() => Changed("PSAR") ? _current.PSAR : (double?)null;

        public double? TradeRatePerMinute_Yes() => Changed("TradeRatePerMinute_Yes") ? _current.TradeRatePerMinute_Yes : (double?)null;
        public double? TradeRatePerMinute_No() => Changed("TradeRatePerMinute_No") ? _current.TradeRatePerMinute_No : (double?)null;
        public double? TradeVolumePerMinute_Yes() => Changed("TradeVolumePerMinute_Yes") ? _current.TradeVolumePerMinute_Yes : (double?)null;
        public double? TradeVolumePerMinute_No() => Changed("TradeVolumePerMinute_No") ? _current.TradeVolumePerMinute_No : (double?)null;

        public double? AverageTradeSize_Yes() => Changed("AverageTradeSize_Yes") ? _current.AverageTradeSize_Yes : (double?)null;
        public double? AverageTradeSize_No() => Changed("AverageTradeSize_No") ? _current.AverageTradeSize_No : (double?)null;

        public int? TradeCount_Yes() => Changed("TradeCount_Yes") ? _current.TradeCount_Yes : (int?)null;
        public int? TradeCount_No() => Changed("TradeCount_No") ? _current.TradeCount_No : (int?)null;

        public int? NonTradeRelatedOrderCount_Yes() => Changed("NonTradeRelatedOrderCount_Yes") ? _current.NonTradeRelatedOrderCount_Yes : (int?)null;
        public int? NonTradeRelatedOrderCount_No() => Changed("NonTradeRelatedOrderCount_No") ? _current.NonTradeRelatedOrderCount_No : (int?)null;

        public double? HighestVolume_Day() => Changed("HighestVolume_Day") ? _current.HighestVolume_Day : (double?)null;
        public double? HighestVolume_Hour() => Changed("HighestVolume_Hour") ? _current.HighestVolume_Hour : (double?)null;
        public double? HighestVolume_Minute() => Changed("HighestVolume_Minute") ? _current.HighestVolume_Minute : (double?)null;

        public double? RecentVolume_LastHour() => Changed("RecentVolume_LastHour") ? _current.RecentVolume_LastHour : (double?)null;
        public double? RecentVolume_LastThreeHours() => Changed("RecentVolume_LastThreeHours") ? _current.RecentVolume_LastThreeHours : (double?)null;
        public double? RecentVolume_LastMonth() => Changed("RecentVolume_LastMonth") ? _current.RecentVolume_LastMonth : (double?)null;

        public double? VelocityPerMinute_Bottom_Yes_Bid() => Changed("VelocityPerMinute_Bottom_Yes_Bid") ? _current.VelocityPerMinute_Bottom_Yes_Bid : (double?)null;
        public int? LevelCount_Bottom_Yes_Bid() => Changed("LevelCount_Bottom_Yes_Bid") ? _current.LevelCount_Bottom_Yes_Bid : (int?)null;
        public double? VelocityPerMinute_Bottom_No_Bid() => Changed("VelocityPerMinute_Bottom_No_Bid") ? _current.VelocityPerMinute_Bottom_No_Bid : (double?)null;
        public int? LevelCount_Bottom_No_Bid() => Changed("LevelCount_Bottom_No_Bid") ? _current.LevelCount_Bottom_No_Bid : (int?)null;

        public double? VelocityPerMinute_Top_Yes_Bid() => Changed("VelocityPerMinute_Top_Yes_Bid") ? _current.VelocityPerMinute_Top_Yes_Bid : (double?)null;
        public int? LevelCount_Top_Yes_Bid() => Changed("LevelCount_Top_Yes_Bid") ? _current.LevelCount_Top_Yes_Bid : (int?)null;
        public double? VelocityPerMinute_Top_No_Bid() => Changed("VelocityPerMinute_Top_No_Bid") ? _current.VelocityPerMinute_Top_No_Bid : (double?)null;
        public int? LevelCount_Top_No_Bid() => Changed("LevelCount_Top_No_Bid") ? _current.LevelCount_Top_No_Bid : (int?)null;

        public int? BestYesBid() => Changed("BestYesBid") ? _current.BestYesBid : (int?)null;
        public int? BestNoBid() => Changed("BestNoBid") ? _current.BestNoBid : (int?)null;

        public int? YesSpread() => Changed("YesSpread") ? _current.YesSpread : (int?)null;
        public int? NoSpread() => Changed("NoSpread") ? _current.NoSpread : (int?)null;

        public int? DepthAtBestYesBid() => Changed("DepthAtBestYesBid") ? _current.DepthAtBestYesBid : (int?)null;
        public int? DepthAtBestNoBid() => Changed("DepthAtBestNoBid") ? _current.DepthAtBestNoBid : (int?)null;

        public int? TopTenPercentLevelDepth_Yes() => Changed("TopTenPercentLevelDepth_Yes") ? _current.TopTenPercentLevelDepth_Yes : (int?)null;
        public int? TopTenPercentLevelDepth_No() => Changed("TopTenPercentLevelDepth_No") ? _current.TopTenPercentLevelDepth_No : (int?)null;

        public int? BidRange_Yes() => Changed("BidRange_Yes") ? _current.BidRange_Yes : (int?)null;
        public int? BidRange_No() => Changed("BidRange_No") ? _current.BidRange_No : (int?)null;

        public int? TotalBidContracts_Yes() => Changed("TotalBidContracts_Yes") ? _current.TotalBidContracts_Yes : (int?)null;
        public int? TotalBidContracts_No() => Changed("TotalBidContracts_No") ? _current.TotalBidContracts_No : (int?)null;

        public int? BidCountImbalance() => Changed("BidCountImbalance") ? _current.BidCountImbalance : (int?)null;
        public double? BidVolumeImbalance() => Changed("BidVolumeImbalance") ? _current.BidVolumeImbalance : (double?)null;

        public int? DepthAtTop4YesBids() => Changed("DepthAtTop4YesBids") ? _current.DepthAtTop4YesBids : (int?)null;
        public int? DepthAtTop4NoBids() => Changed("DepthAtTop4NoBids") ? _current.DepthAtTop4NoBids : (int?)null;

        public double? TotalBidVolume_Yes() => Changed("TotalBidVolume_Yes") ? _current.TotalBidVolume_Yes : (double?)null;
        public double? TotalBidVolume_No() => Changed("TotalBidVolume_No") ? _current.TotalBidVolume_No : (double?)null;

        public double? RSI_Short() => Changed("RSI_Short") ? _current.RSI_Short : (double?)null;
        public double? RSI_Medium() => Changed("RSI_Medium") ? _current.RSI_Medium : (double?)null;
        public double? RSI_Long() => Changed("RSI_Long") ? _current.RSI_Long : (double?)null;

        // MACD tuples
        public double? MACD_Medium_MACD() => ChangedTuple("MACD_Medium", 1, "MACD") ? _current.MACD_Medium.MACD : (double?)null;
        public double? MACD_Medium_Signal() => ChangedTuple("MACD_Medium", 2, "Signal") ? _current.MACD_Medium.Signal : (double?)null;
        public double? MACD_Medium_Histogram() => ChangedTuple("MACD_Medium", 3, "Histogram") ? _current.MACD_Medium.Histogram : (double?)null;

        public double? MACD_Long_MACD() => ChangedTuple("MACD_Long", 1, "MACD") ? _current.MACD_Long.MACD : (double?)null;
        public double? MACD_Long_Signal() => ChangedTuple("MACD_Long", 2, "Signal") ? _current.MACD_Long.Signal : (double?)null;
        public double? MACD_Long_Histogram() => ChangedTuple("MACD_Long", 3, "Histogram") ? _current.MACD_Long.Histogram : (double?)null;

        public double? EMA_Medium() => Changed("EMA_Medium") ? _current.EMA_Medium : (double?)null;
        public double? EMA_Long() => Changed("EMA_Long") ? _current.EMA_Long : (double?)null;

        // Bollinger bands tuples
        public double? BollingerBands_Medium_Lower() => ChangedTuple("BollingerBands_Medium", 1, "Lower") ? _current.BollingerBands_Medium.Lower : (double?)null;
        public double? BollingerBands_Medium_Middle() => ChangedTuple("BollingerBands_Medium", 2, "Middle") ? _current.BollingerBands_Medium.Middle : (double?)null;
        public double? BollingerBands_Medium_Upper() => ChangedTuple("BollingerBands_Medium", 3, "Upper") ? _current.BollingerBands_Medium.Upper : (double?)null;

        public double? BollingerBands_Long_Lower() => ChangedTuple("BollingerBands_Long", 1, "Lower") ? _current.BollingerBands_Long.Lower : (double?)null;
        public double? BollingerBands_Long_Middle() => ChangedTuple("BollingerBands_Long", 2, "Middle") ? _current.BollingerBands_Long.Middle : (double?)null;
        public double? BollingerBands_Long_Upper() => ChangedTuple("BollingerBands_Long", 3, "Upper") ? _current.BollingerBands_Long.Upper : (double?)null;

        public double? ATR_Medium() => Changed("ATR_Medium") ? _current.ATR_Medium : (double?)null;
        public double? ATR_Long() => Changed("ATR_Long") ? _current.ATR_Long : (double?)null;

        public double? VWAP_Short() => Changed("VWAP_Short") ? _current.VWAP_Short : (double?)null;
        public double? VWAP_Medium() => Changed("VWAP_Medium") ? _current.VWAP_Medium : (double?)null;

        // Stochastic oscillator tuples
        public double? StochasticOscillator_Short_K() => ChangedTuple("StochasticOscillator_Short", 1, "K") ? _current.StochasticOscillator_Short.K : (double?)null;
        public double? StochasticOscillator_Short_D() => ChangedTuple("StochasticOscillator_Short", 2, "D") ? _current.StochasticOscillator_Short.D : (double?)null;

        public double? StochasticOscillator_Medium_K() => ChangedTuple("StochasticOscillator_Medium", 1, "K") ? _current.StochasticOscillator_Medium.K : (double?)null;
        public double? StochasticOscillator_Medium_D() => ChangedTuple("StochasticOscillator_Medium", 2, "D") ? _current.StochasticOscillator_Medium.D : (double?)null;

        public double? StochasticOscillator_Long_K() => ChangedTuple("StochasticOscillator_Long", 1, "K") ? _current.StochasticOscillator_Long.K : (double?)null;
        public double? StochasticOscillator_Long_D() => ChangedTuple("StochasticOscillator_Long", 2, "D") ? _current.StochasticOscillator_Long.D : (double?)null;

        public long? OBV_Medium() => Changed("OBV_Medium") ? _current.OBV_Medium : (long?)null;
        public long? OBV_Long() => Changed("OBV_Long") ? _current.OBV_Long : (long?)null;

        public double? YesBidCenterOfMass() => Changed("YesBidCenterOfMass") ? _current.YesBidCenterOfMass : (double?)null;
        public double? NoBidCenterOfMass() => Changed("NoBidCenterOfMass") ? _current.NoBidCenterOfMass : (double?)null;

        public TimeSpan? MarketAge() => Changed("MarketAge") ? _current.MarketAge : (TimeSpan?)null;
        public TimeSpan? TimeLeft() => Changed("TimeLeft") ? _current.TimeLeft : (TimeSpan?)null;

        public bool? ChangeMetricsMature() => Changed("ChangeMetricsMature") ? _current.ChangeMetricsMature : (bool?)null;
        public bool? CanCloseEarly() => Changed("CanCloseEarly") ? _current.CanCloseEarly : (bool?)null;

        public DateTime? LastWebSocketMessageReceived() => Changed("LastWebSocketMessageReceived") ? _current.LastWebSocketMessageReceived : (DateTime?)null;

        public TimeSpan? HoldTime() => Changed("HoldTime") ? _current.HoldTime : (TimeSpan?)null;

        public double? OrderVolumePerMinute_YesBid() => Changed("OrderVolumePerMinute_YesBid") ? _current.OrderVolumePerMinute_YesBid : (double?)null;
        public double? OrderVolumePerMinute_NoBid() => Changed("OrderVolumePerMinute_NoBid") ? _current.OrderVolumePerMinute_NoBid : (double?)null;

        public double? TolerancePercentage() => Changed("TolerancePercentage") ? _current.TolerancePercentage : (double?)null;

        public string? MarketBehaviorYes() => Changed("MarketBehaviorYes") ? _current.MarketBehaviorYes : null;
        public string? MarketBehaviorNo() => Changed("MarketBehaviorNo") ? _current.MarketBehaviorNo : null;
        public string? GoodBadPriceYes() => Changed("GoodBadPriceYes") ? _current.GoodBadPriceYes : null;
        public string? GoodBadPriceNo() => Changed("GoodBadPriceNo") ? _current.GoodBadPriceNo : null;

        // All-time & recent highs/lows (tuple-based)
        public int? AllTimeHighYes_Bid_Bid() => ChangedTuple("AllTimeHighYes_Bid", 1, "Bid") ? _current.AllTimeHighYes_Bid.Bid : (int?)null;
        public DateTime? AllTimeHighYes_Bid_When() => ChangedTuple("AllTimeHighYes_Bid", 2, "When") ? _current.AllTimeHighYes_Bid.When : (DateTime?)null;
        public int? AllTimeLowYes_Bid_Bid() => ChangedTuple("AllTimeLowYes_Bid", 1, "Bid") ? _current.AllTimeLowYes_Bid.Bid : (int?)null;
        public DateTime? AllTimeLowYes_Bid_When() => ChangedTuple("AllTimeLowYes_Bid", 2, "When") ? _current.AllTimeLowYes_Bid.When : (DateTime?)null;

        public int? AllTimeHighNo_Bid_Bid() => ChangedTuple("AllTimeHighNo_Bid", 1, "Bid") ? _current.AllTimeHighNo_Bid.Bid : (int?)null;
        public DateTime? AllTimeHighNo_Bid_When() => ChangedTuple("AllTimeHighNo_Bid", 2, "When") ? _current.AllTimeHighNo_Bid.When : (DateTime?)null;
        public int? AllTimeLowNo_Bid_Bid() => ChangedTuple("AllTimeLowNo_Bid", 1, "Bid") ? _current.AllTimeLowNo_Bid.Bid : (int?)null;
        public DateTime? AllTimeLowNo_Bid_When() => ChangedTuple("AllTimeLowNo_Bid", 2, "When") ? _current.AllTimeLowNo_Bid.When : (DateTime?)null;

        public int? RecentHighYes_Bid_Bid() => ChangedTuple("RecentHighYes_Bid", 1, "Bid") ? _current.RecentHighYes_Bid.Bid : (int?)null;
        public DateTime? RecentHighYes_Bid_When() => ChangedTuple("RecentHighYes_Bid", 2, "When") ? _current.RecentHighYes_Bid.When : (DateTime?)null;
        public int? RecentLowYes_Bid_Bid() => ChangedTuple("RecentLowYes_Bid", 1, "Bid") ? _current.RecentLowYes_Bid.Bid : (int?)null;
        public DateTime? RecentLowYes_Bid_When() => ChangedTuple("RecentLowYes_Bid", 2, "When") ? _current.RecentLowYes_Bid.When : (DateTime?)null;

        public int? RecentHighNo_Bid_Bid() => ChangedTuple("RecentHighNo_Bid", 1, "Bid") ? _current.RecentHighNo_Bid.Bid : (int?)null;
        public DateTime? RecentHighNo_Bid_When() => ChangedTuple("RecentHighNo_Bid", 2, "When") ? _current.RecentHighNo_Bid.When : (DateTime?)null;
        public int? RecentLowNo_Bid_Bid() => ChangedTuple("RecentLowNo_Bid", 1, "Bid") ? _current.RecentLowNo_Bid.Bid : (int?)null;
        public DateTime? RecentLowNo_Bid_When() => ChangedTuple("RecentLowNo_Bid", 2, "When") ? _current.RecentLowNo_Bid.When : (DateTime?)null;
    }
}
