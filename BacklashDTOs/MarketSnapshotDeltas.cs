using System.Reflection;

namespace BacklashDTOs
{
    /// <summary>
    /// Represents the delta between two MarketSnapshot instances, tracking which properties have changed.
    /// </summary>
    public sealed class MarketSnapshotDelta
    {
        private readonly MarketSnapshot _previous;

        private readonly MarketSnapshot _current;
        private readonly HashSet<string> _changed;
        /// <summary>
        /// Initializes a new instance of the MarketSnapshotDelta class by computing the differences between two MarketSnapshot instances.
        /// </summary>
        /// <param name="previous">The previous MarketSnapshot to compare against.</param>
        /// <param name="current">The current MarketSnapshot to compare.</param>
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

        /// <summary>
        /// Determines whether the specified property path has changed.
        /// </summary>
        /// <param name="path">The property path to check.</param>
        /// <returns>True if the property has changed, otherwise false.</returns>
        private bool Changed(string path) => _changed.Contains(path);

        // ValueTuple elements may surface as either their friendly name (e.g., "Bid")
        // or "Item{n}" depending on the runtime. Handle both.
        /// <summary>
        /// Determines whether the specified tuple property has changed, handling both named and indexed access.
        /// </summary>
        /// <param name="prop">The property name.</param>
        /// <param name="index1Based">The 1-based index for Item access.</param>
        /// <param name="named">The named component (e.g., "Bid").</param>
        /// <returns>True if the tuple property has changed, otherwise false.</returns>
        private bool ChangedTuple(string prop, int index1Based, string named)
        {
            return _changed.Contains(prop + "." + named) || _changed.Contains(prop + ".Item" + index1Based.ToString());
        }

        /// <summary>
        /// Gets the timestamp if it has changed, otherwise null.
        /// </summary>
        /// <returns>The timestamp or null if unchanged.</returns>
        public DateTime? Timestamp() => Changed("Timestamp") ? _current.Timestamp : (DateTime?)null;
        /// <summary>
        /// Gets the market ticker if it has changed, otherwise null.
        /// </summary>
        /// <returns>The market ticker or null if unchanged.</returns>
        public string? MarketTicker() => Changed("MarketTicker") ? _current.MarketTicker : null;
        /// <summary>
        /// Gets the market category if it has changed, otherwise null.
        /// </summary>
        /// <returns>The market category or null if unchanged.</returns>
        public string? MarketCategory() => Changed("MarketCategory") ? _current.MarketCategory : null;
        /// <summary>
        /// Gets the market status if it has changed, otherwise null.
        /// </summary>
        /// <returns>The market status or null if unchanged.</returns>
        public string? MarketStatus() => Changed("MarketStatus") ? _current.MarketStatus : null;
        /// <summary>
        /// Gets the market type if it has changed, otherwise null.
        /// </summary>
        /// <returns>The market type or null if unchanged.</returns>
        public string? MarketType() => Changed("MarketType") ? _current.MarketType : null;
        /// <summary>
        /// Gets the snapshot schema version if it has changed, otherwise null.
        /// </summary>
        /// <returns>The snapshot schema version or null if unchanged.</returns>
        public int? SnapshotSchemaVersion() => Changed("SnapshotSchemaVersion") ? _current.SnapshotSchemaVersion : (int?)null;

        /// <summary>
        /// Gets the yes bid slope per minute (short) if it has changed, otherwise null.
        /// </summary>
        /// <returns>The yes bid slope per minute (short) or null if unchanged.</returns>
        public double? YesBidSlopePerMinute_Short() => Changed("YesBidSlopePerMinute_Short") ? _current.YesBidSlopePerMinute_Short : (double?)null;
        /// <summary>
        /// Gets the no bid slope per minute (short) if it has changed, otherwise null.
        /// </summary>
        /// <returns>The no bid slope per minute (short) or null if unchanged.</returns>
        public double? NoBidSlopePerMinute_Short() => Changed("NoBidSlopePerMinute_Short") ? _current.NoBidSlopePerMinute_Short : (double?)null;

        /// <summary>
        /// Gets the yes bid slope per minute (medium) if it has changed, otherwise null.
        /// </summary>
        /// <returns>The yes bid slope per minute (medium) or null if unchanged.</returns>
        public double? YesBidSlopePerMinute_Medium() => Changed("YesBidSlopePerMinute_Medium") ? _current.YesBidSlopePerMinute_Medium : (double?)null;
        /// <summary>
        /// Gets the no bid slope per minute (medium) if it has changed, otherwise null.
        /// </summary>
        /// <returns>The no bid slope per minute (medium) or null if unchanged.</returns>
        public double? NoBidSlopePerMinute_Medium() => Changed("NoBidSlopePerMinute_Medium") ? _current.NoBidSlopePerMinute_Medium : (double?)null;
        /// <summary>
        /// Gets the total orderbook depth for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The total orderbook depth for yes or null if unchanged.</returns>
        public long? TotalOrderbookDepth_Yes() => Changed("TotalOrderbookDepth_Yes") ? _current.TotalOrderbookDepth_Yes : (long?)null;
        /// <summary>
        /// Gets the total orderbook depth for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The total orderbook depth for no or null if unchanged.</returns>
        public long? TotalOrderbookDepth_No() => Changed("TotalOrderbookDepth_No") ? _current.TotalOrderbookDepth_No : (long?)null;

        /// <summary>
        /// Gets the position size if it has changed, otherwise null.
        /// </summary>
        /// <returns>The position size or null if unchanged.</returns>
        public int? PositionSize() => Changed("PositionSize") ? _current.PositionSize : (int?)null;
        /// <summary>
        /// Gets the market exposure if it has changed, otherwise null.
        /// </summary>
        /// <returns>The market exposure or null if unchanged.</returns>
        public double? MarketExposure() => Changed("MarketExposure") ? _current.MarketExposure : (double?)null;
        /// <summary>
        /// Gets the buy-in price if it has changed, otherwise null.
        /// </summary>
        /// <returns>The buy-in price or null if unchanged.</returns>
        public double? BuyinPrice() => Changed("BuyinPrice") ? _current.BuyinPrice : (double?)null;
        /// <summary>
        /// Gets the position upside if it has changed, otherwise null.
        /// </summary>
        /// <returns>The position upside or null if unchanged.</returns>
        public double? PositionUpside() => Changed("PositionUpside") ? _current.PositionUpside : (double?)null;
        /// <summary>
        /// Gets the position downside if it has changed, otherwise null.
        /// </summary>
        /// <returns>The position downside or null if unchanged.</returns>
        public double? PositionDownside() => Changed("PositionDownside") ? _current.PositionDownside : (double?)null;
        /// <summary>
        /// Gets the total traded if it has changed, otherwise null.
        /// </summary>
        /// <returns>The total traded or null if unchanged.</returns>
        public long? TotalTraded() => Changed("TotalTraded") ? _current.TotalTraded : (long?)null;
        /// <summary>
        /// Gets the realized P&L if it has changed, otherwise null.
        /// </summary>
        /// <returns>The realized P&L or null if unchanged.</returns>
        public double? RealizedPnl() => Changed("RealizedPnl") ? _current.RealizedPnl : (double?)null;
        /// <summary>
        /// Gets the fees paid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The fees paid or null if unchanged.</returns>
        public double? FeesPaid() => Changed("FeesPaid") ? _current.FeesPaid : (double?)null;
        /// <summary>
        /// Gets the position ROI if it has changed, otherwise null.
        /// </summary>
        /// <returns>The position ROI or null if unchanged.</returns>
        public double? PositionROI() => Changed("PositionROI") ? _current.PositionROI : (double?)null;
        /// <summary>
        /// Gets the position ROI amount if it has changed, otherwise null.
        /// </summary>
        /// <returns>The position ROI amount or null if unchanged.</returns>
        public double? PositionROIAmt() => Changed("PositionROIAmt") ? _current.PositionROIAmt : (double?)null;
        /// <summary>
        /// Gets the expected fees if it has changed, otherwise null.
        /// </summary>
        /// <returns>The expected fees or null if unchanged.</returns>
        public double? ExpectedFees() => Changed("ExpectedFees") ? _current.ExpectedFees : (double?)null;

        /// <summary>
        /// Gets the ADX (Average Directional Index) if it has changed, otherwise null.
        /// </summary>
        /// <returns>The ADX or null if unchanged.</returns>
        public double? ADX() => Changed("ADX") ? _current.ADX : (double?)null;
        /// <summary>
        /// Gets the PlusDI (Positive Directional Indicator) if it has changed, otherwise null.
        /// </summary>
        /// <returns>The PlusDI or null if unchanged.</returns>
        public double? PlusDI() => Changed("PlusDI") ? _current.PlusDI : (double?)null;
        /// <summary>
        /// Gets the MinusDI (Negative Directional Indicator) if it has changed, otherwise null.
        /// </summary>
        /// <returns>The MinusDI or null if unchanged.</returns>
        public double? MinusDI() => Changed("MinusDI") ? _current.MinusDI : (double?)null;
        /// <summary>
        /// Gets the PSAR (Parabolic SAR) if it has changed, otherwise null.
        /// </summary>
        /// <returns>The PSAR or null if unchanged.</returns>
        public double? PSAR() => Changed("PSAR") ? _current.PSAR : (double?)null;

        /// <summary>
        /// Gets the trade rate per minute for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The trade rate per minute for yes or null if unchanged.</returns>
        public double? TradeRatePerMinute_Yes() => Changed("TradeRatePerMinute_Yes") ? _current.TradeRatePerMinute_Yes : (double?)null;
        /// <summary>
        /// Gets the trade rate per minute for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The trade rate per minute for no or null if unchanged.</returns>
        public double? TradeRatePerMinute_No() => Changed("TradeRatePerMinute_No") ? _current.TradeRatePerMinute_No : (double?)null;
        /// <summary>
        /// Gets the trade volume per minute for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The trade volume per minute for yes or null if unchanged.</returns>
        public double? TradeVolumePerMinute_Yes() => Changed("TradeVolumePerMinute_Yes") ? _current.TradeVolumePerMinute_Yes : (double?)null;
        /// <summary>
        /// Gets the trade volume per minute for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The trade volume per minute for no or null if unchanged.</returns>
        public double? TradeVolumePerMinute_No() => Changed("TradeVolumePerMinute_No") ? _current.TradeVolumePerMinute_No : (double?)null;

        /// <summary>
        /// Gets the average trade size for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The average trade size for yes or null if unchanged.</returns>
        public double? AverageTradeSize_Yes() => Changed("AverageTradeSize_Yes") ? _current.AverageTradeSize_Yes : (double?)null;
        /// <summary>
        /// Gets the average trade size for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The average trade size for no or null if unchanged.</returns>
        public double? AverageTradeSize_No() => Changed("AverageTradeSize_No") ? _current.AverageTradeSize_No : (double?)null;

        /// <summary>
        /// Gets the trade count for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The trade count for yes or null if unchanged.</returns>
        public int? TradeCount_Yes() => Changed("TradeCount_Yes") ? _current.TradeCount_Yes : (int?)null;
        /// <summary>
        /// Gets the trade count for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The trade count for no or null if unchanged.</returns>
        public int? TradeCount_No() => Changed("TradeCount_No") ? _current.TradeCount_No : (int?)null;

        /// <summary>
        /// Gets the non-trade related order count for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The non-trade related order count for yes or null if unchanged.</returns>
        public int? NonTradeRelatedOrderCount_Yes() => Changed("NonTradeRelatedOrderCount_Yes") ? _current.NonTradeRelatedOrderCount_Yes : (int?)null;
        /// <summary>
        /// Gets the non-trade related order count for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The non-trade related order count for no or null if unchanged.</returns>
        public int? NonTradeRelatedOrderCount_No() => Changed("NonTradeRelatedOrderCount_No") ? _current.NonTradeRelatedOrderCount_No : (int?)null;

        /// <summary>
        /// Gets the highest volume for the day if it has changed, otherwise null.
        /// </summary>
        /// <returns>The highest volume for the day or null if unchanged.</returns>
        public double? HighestVolume_Day() => Changed("HighestVolume_Day") ? _current.HighestVolume_Day : (double?)null;
        /// <summary>
        /// Gets the highest volume for the hour if it has changed, otherwise null.
        /// </summary>
        /// <returns>The highest volume for the hour or null if unchanged.</returns>
        public double? HighestVolume_Hour() => Changed("HighestVolume_Hour") ? _current.HighestVolume_Hour : (double?)null;
        /// <summary>
        /// Gets the highest volume for the minute if it has changed, otherwise null.
        /// </summary>
        /// <returns>The highest volume for the minute or null if unchanged.</returns>
        public double? HighestVolume_Minute() => Changed("HighestVolume_Minute") ? _current.HighestVolume_Minute : (double?)null;

        /// <summary>
        /// Gets the recent volume for the last hour if it has changed, otherwise null.
        /// </summary>
        /// <returns>The recent volume for the last hour or null if unchanged.</returns>
        public double? RecentVolume_LastHour() => Changed("RecentVolume_LastHour") ? _current.RecentVolume_LastHour : (double?)null;
        /// <summary>
        /// Gets the recent volume for the last three hours if it has changed, otherwise null.
        /// </summary>
        /// <returns>The recent volume for the last three hours or null if unchanged.</returns>
        public double? RecentVolume_LastThreeHours() => Changed("RecentVolume_LastThreeHours") ? _current.RecentVolume_LastThreeHours : (double?)null;
        /// <summary>
        /// Gets the recent volume for the last month if it has changed, otherwise null.
        /// </summary>
        /// <returns>The recent volume for the last month or null if unchanged.</returns>
        public double? RecentVolume_LastMonth() => Changed("RecentVolume_LastMonth") ? _current.RecentVolume_LastMonth : (double?)null;

        /// <summary>
        /// Gets the velocity per minute for the bottom yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The velocity per minute for the bottom yes bid or null if unchanged.</returns>
        public double? VelocityPerMinute_Bottom_Yes_Bid() => Changed("VelocityPerMinute_Bottom_Yes_Bid") ? _current.VelocityPerMinute_Bottom_Yes_Bid : (double?)null;
        /// <summary>
        /// Gets the level count for the bottom yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The level count for the bottom yes bid or null if unchanged.</returns>
        public int? LevelCount_Bottom_Yes_Bid() => Changed("LevelCount_Bottom_Yes_Bid") ? _current.LevelCount_Bottom_Yes_Bid : (int?)null;
        /// <summary>
        /// Gets the velocity per minute for the bottom no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The velocity per minute for the bottom no bid or null if unchanged.</returns>
        public double? VelocityPerMinute_Bottom_No_Bid() => Changed("VelocityPerMinute_Bottom_No_Bid") ? _current.VelocityPerMinute_Bottom_No_Bid : (double?)null;
        /// <summary>
        /// Gets the level count for the bottom no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The level count for the bottom no bid or null if unchanged.</returns>
        public int? LevelCount_Bottom_No_Bid() => Changed("LevelCount_Bottom_No_Bid") ? _current.LevelCount_Bottom_No_Bid : (int?)null;

        /// <summary>
        /// Gets the velocity per minute for the top yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The velocity per minute for the top yes bid or null if unchanged.</returns>
        public double? VelocityPerMinute_Top_Yes_Bid() => Changed("VelocityPerMinute_Top_Yes_Bid") ? _current.VelocityPerMinute_Top_Yes_Bid : (double?)null;
        /// <summary>
        /// Gets the level count for the top yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The level count for the top yes bid or null if unchanged.</returns>
        public int? LevelCount_Top_Yes_Bid() => Changed("LevelCount_Top_Yes_Bid") ? _current.LevelCount_Top_Yes_Bid : (int?)null;
        /// <summary>
        /// Gets the velocity per minute for the top no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The velocity per minute for the top no bid or null if unchanged.</returns>
        public double? VelocityPerMinute_Top_No_Bid() => Changed("VelocityPerMinute_Top_No_Bid") ? _current.VelocityPerMinute_Top_No_Bid : (double?)null;
        /// <summary>
        /// Gets the level count for the top no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The level count for the top no bid or null if unchanged.</returns>
        public int? LevelCount_Top_No_Bid() => Changed("LevelCount_Top_No_Bid") ? _current.LevelCount_Top_No_Bid : (int?)null;

        /// <summary>
        /// Gets the best yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The best yes bid or null if unchanged.</returns>
        public int? BestYesBid() => Changed("BestYesBid") ? _current.BestYesBid : (int?)null;
        /// <summary>
        /// Gets the best no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The best no bid or null if unchanged.</returns>
        public int? BestNoBid() => Changed("BestNoBid") ? _current.BestNoBid : (int?)null;

        /// <summary>
        /// Gets the yes spread if it has changed, otherwise null.
        /// </summary>
        /// <returns>The yes spread or null if unchanged.</returns>
        public int? YesSpread() => Changed("YesSpread") ? _current.YesSpread : (int?)null;
        /// <summary>
        /// Gets the no spread if it has changed, otherwise null.
        /// </summary>
        /// <returns>The no spread or null if unchanged.</returns>
        public int? NoSpread() => Changed("NoSpread") ? _current.NoSpread : (int?)null;

        /// <summary>
        /// Gets the depth at the best yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The depth at the best yes bid or null if unchanged.</returns>
        public int? DepthAtBestYesBid() => Changed("DepthAtBestYesBid") ? _current.DepthAtBestYesBid : (int?)null;
        /// <summary>
        /// Gets the depth at the best no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The depth at the best no bid or null if unchanged.</returns>
        public int? DepthAtBestNoBid() => Changed("DepthAtBestNoBid") ? _current.DepthAtBestNoBid : (int?)null;

        /// <summary>
        /// Gets the top ten percent level depth for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The top ten percent level depth for yes or null if unchanged.</returns>
        public int? TopTenPercentLevelDepth_Yes() => Changed("TopTenPercentLevelDepth_Yes") ? _current.TopTenPercentLevelDepth_Yes : (int?)null;
        /// <summary>
        /// Gets the top ten percent level depth for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The top ten percent level depth for no or null if unchanged.</returns>
        public int? TopTenPercentLevelDepth_No() => Changed("TopTenPercentLevelDepth_No") ? _current.TopTenPercentLevelDepth_No : (int?)null;

        /// <summary>
        /// Gets the bid range for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid range for yes or null if unchanged.</returns>
        public int? BidRange_Yes() => Changed("BidRange_Yes") ? _current.BidRange_Yes : (int?)null;
        /// <summary>
        /// Gets the bid range for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid range for no or null if unchanged.</returns>
        public int? BidRange_No() => Changed("BidRange_No") ? _current.BidRange_No : (int?)null;

        /// <summary>
        /// Gets the total bid contracts for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The total bid contracts for yes or null if unchanged.</returns>
        public int? TotalBidContracts_Yes() => Changed("TotalBidContracts_Yes") ? _current.TotalBidContracts_Yes : (int?)null;
        /// <summary>
        /// Gets the total bid contracts for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The total bid contracts for no or null if unchanged.</returns>
        public int? TotalBidContracts_No() => Changed("TotalBidContracts_No") ? _current.TotalBidContracts_No : (int?)null;

        /// <summary>
        /// Gets the bid count imbalance if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid count imbalance or null if unchanged.</returns>
        public int? BidCountImbalance() => Changed("BidCountImbalance") ? _current.BidCountImbalance : (int?)null;
        /// <summary>
        /// Gets the bid volume imbalance if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid volume imbalance or null if unchanged.</returns>
        public double? BidVolumeImbalance() => Changed("BidVolumeImbalance") ? _current.BidVolumeImbalance : (double?)null;

        /// <summary>
        /// Gets the depth at the top 4 yes bids if it has changed, otherwise null.
        /// </summary>
        /// <returns>The depth at the top 4 yes bids or null if unchanged.</returns>
        public int? DepthAtTop4YesBids() => Changed("DepthAtTop4YesBids") ? _current.DepthAtTop4YesBids : (int?)null;
        /// <summary>
        /// Gets the depth at the top 4 no bids if it has changed, otherwise null.
        /// </summary>
        /// <returns>The depth at the top 4 no bids or null if unchanged.</returns>
        public int? DepthAtTop4NoBids() => Changed("DepthAtTop4NoBids") ? _current.DepthAtTop4NoBids : (int?)null;

        /// <summary>
        /// Gets the total bid volume for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The total bid volume for yes or null if unchanged.</returns>
        public double? TotalBidVolume_Yes() => Changed("TotalBidVolume_Yes") ? _current.TotalBidVolume_Yes : (double?)null;
        /// <summary>
        /// Gets the total bid volume for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The total bid volume for no or null if unchanged.</returns>
        public double? TotalBidVolume_No() => Changed("TotalBidVolume_No") ? _current.TotalBidVolume_No : (double?)null;

        /// <summary>
        /// Gets the RSI (Relative Strength Index) short if it has changed, otherwise null.
        /// </summary>
        /// <returns>The RSI short or null if unchanged.</returns>
        public double? RSI_Short() => Changed("RSI_Short") ? _current.RSI_Short : (double?)null;
        /// <summary>
        /// Gets the RSI (Relative Strength Index) medium if it has changed, otherwise null.
        /// </summary>
        /// <returns>The RSI medium or null if unchanged.</returns>
        public double? RSI_Medium() => Changed("RSI_Medium") ? _current.RSI_Medium : (double?)null;
        /// <summary>
        /// Gets the RSI (Relative Strength Index) long if it has changed, otherwise null.
        /// </summary>
        /// <returns>The RSI long or null if unchanged.</returns>
        public double? RSI_Long() => Changed("RSI_Long") ? _current.RSI_Long : (double?)null;

        // MACD tuples
        /// <summary>
        /// Gets the MACD component of the MACD medium indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The MACD component of the MACD medium or null if unchanged.</returns>
        public double? MACD_Medium_MACD() => ChangedTuple("MACD_Medium", 1, "MACD") ? _current.MACD_Medium.MACD : (double?)null;
        /// <summary>
        /// Gets the signal component of the MACD medium indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The signal component of the MACD medium or null if unchanged.</returns>
        public double? MACD_Medium_Signal() => ChangedTuple("MACD_Medium", 2, "Signal") ? _current.MACD_Medium.Signal : (double?)null;
        /// <summary>
        /// Gets the histogram component of the MACD medium indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The histogram component of the MACD medium or null if unchanged.</returns>
        public double? MACD_Medium_Histogram() => ChangedTuple("MACD_Medium", 3, "Histogram") ? _current.MACD_Medium.Histogram : (double?)null;

        /// <summary>
        /// Gets the MACD component of the MACD long indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The MACD component of the MACD long or null if unchanged.</returns>
        public double? MACD_Long_MACD() => ChangedTuple("MACD_Long", 1, "MACD") ? _current.MACD_Long.MACD : (double?)null;
        /// <summary>
        /// Gets the signal component of the MACD long indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The signal component of the MACD long or null if unchanged.</returns>
        public double? MACD_Long_Signal() => ChangedTuple("MACD_Long", 2, "Signal") ? _current.MACD_Long.Signal : (double?)null;
        /// <summary>
        /// Gets the histogram component of the MACD long indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The histogram component of the MACD long or null if unchanged.</returns>
        public double? MACD_Long_Histogram() => ChangedTuple("MACD_Long", 3, "Histogram") ? _current.MACD_Long.Histogram : (double?)null;

        /// <summary>
        /// Gets the EMA (Exponential Moving Average) medium if it has changed, otherwise null.
        /// </summary>
        /// <returns>The EMA medium or null if unchanged.</returns>
        public double? EMA_Medium() => Changed("EMA_Medium") ? _current.EMA_Medium : (double?)null;
        /// <summary>
        /// Gets the EMA (Exponential Moving Average) long if it has changed, otherwise null.
        /// </summary>
        /// <returns>The EMA long or null if unchanged.</returns>
        public double? EMA_Long() => Changed("EMA_Long") ? _current.EMA_Long : (double?)null;

        // Bollinger bands tuples
        /// <summary>
        /// Gets the lower band of the Bollinger Bands medium indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The lower band of the Bollinger Bands medium or null if unchanged.</returns>
        public double? BollingerBands_Medium_Lower() => ChangedTuple("BollingerBands_Medium", 1, "Lower") ? _current.BollingerBands_Medium.Lower : (double?)null;
        /// <summary>
        /// Gets the middle band of the Bollinger Bands medium indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The middle band of the Bollinger Bands medium or null if unchanged.</returns>
        public double? BollingerBands_Medium_Middle() => ChangedTuple("BollingerBands_Medium", 2, "Middle") ? _current.BollingerBands_Medium.Middle : (double?)null;
        /// <summary>
        /// Gets the upper band of the Bollinger Bands medium indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The upper band of the Bollinger Bands medium or null if unchanged.</returns>
        public double? BollingerBands_Medium_Upper() => ChangedTuple("BollingerBands_Medium", 3, "Upper") ? _current.BollingerBands_Medium.Upper : (double?)null;

        /// <summary>
        /// Gets the lower band of the Bollinger Bands long indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The lower band of the Bollinger Bands long or null if unchanged.</returns>
        public double? BollingerBands_Long_Lower() => ChangedTuple("BollingerBands_Long", 1, "Lower") ? _current.BollingerBands_Long.Lower : (double?)null;
        /// <summary>
        /// Gets the middle band of the Bollinger Bands long indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The middle band of the Bollinger Bands long or null if unchanged.</returns>
        public double? BollingerBands_Long_Middle() => ChangedTuple("BollingerBands_Long", 2, "Middle") ? _current.BollingerBands_Long.Middle : (double?)null;
        /// <summary>
        /// Gets the upper band of the Bollinger Bands long indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The upper band of the Bollinger Bands long or null if unchanged.</returns>
        public double? BollingerBands_Long_Upper() => ChangedTuple("BollingerBands_Long", 3, "Upper") ? _current.BollingerBands_Long.Upper : (double?)null;

        /// <summary>
        /// Gets the ATR (Average True Range) medium if it has changed, otherwise null.
        /// </summary>
        /// <returns>The ATR medium or null if unchanged.</returns>
        public double? ATR_Medium() => Changed("ATR_Medium") ? _current.ATR_Medium : (double?)null;
        /// <summary>
        /// Gets the ATR (Average True Range) long if it has changed, otherwise null.
        /// </summary>
        /// <returns>The ATR long or null if unchanged.</returns>
        public double? ATR_Long() => Changed("ATR_Long") ? _current.ATR_Long : (double?)null;

        /// <summary>
        /// Gets the VWAP (Volume Weighted Average Price) short if it has changed, otherwise null.
        /// </summary>
        /// <returns>The VWAP short or null if unchanged.</returns>
        public double? VWAP_Short() => Changed("VWAP_Short") ? _current.VWAP_Short : (double?)null;
        /// <summary>
        /// Gets the VWAP (Volume Weighted Average Price) medium if it has changed, otherwise null.
        /// </summary>
        /// <returns>The VWAP medium or null if unchanged.</returns>
        public double? VWAP_Medium() => Changed("VWAP_Medium") ? _current.VWAP_Medium : (double?)null;

        // Stochastic oscillator tuples
        /// <summary>
        /// Gets the K component of the Stochastic Oscillator short indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The K component of the Stochastic Oscillator short or null if unchanged.</returns>
        public double? StochasticOscillator_Short_K() => ChangedTuple("StochasticOscillator_Short", 1, "K") ? _current.StochasticOscillator_Short.K : (double?)null;
        /// <summary>
        /// Gets the D component of the Stochastic Oscillator short indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The D component of the Stochastic Oscillator short or null if unchanged.</returns>
        public double? StochasticOscillator_Short_D() => ChangedTuple("StochasticOscillator_Short", 2, "D") ? _current.StochasticOscillator_Short.D : (double?)null;

        /// <summary>
        /// Gets the K component of the Stochastic Oscillator medium indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The K component of the Stochastic Oscillator medium or null if unchanged.</returns>
        public double? StochasticOscillator_Medium_K() => ChangedTuple("StochasticOscillator_Medium", 1, "K") ? _current.StochasticOscillator_Medium.K : (double?)null;
        /// <summary>
        /// Gets the D component of the Stochastic Oscillator medium indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The D component of the Stochastic Oscillator medium or null if unchanged.</returns>
        public double? StochasticOscillator_Medium_D() => ChangedTuple("StochasticOscillator_Medium", 2, "D") ? _current.StochasticOscillator_Medium.D : (double?)null;

        /// <summary>
        /// Gets the K component of the Stochastic Oscillator long indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The K component of the Stochastic Oscillator long or null if unchanged.</returns>
        public double? StochasticOscillator_Long_K() => ChangedTuple("StochasticOscillator_Long", 1, "K") ? _current.StochasticOscillator_Long.K : (double?)null;
        /// <summary>
        /// Gets the D component of the Stochastic Oscillator long indicator if it has changed, otherwise null.
        /// </summary>
        /// <returns>The D component of the Stochastic Oscillator long or null if unchanged.</returns>
        public double? StochasticOscillator_Long_D() => ChangedTuple("StochasticOscillator_Long", 2, "D") ? _current.StochasticOscillator_Long.D : (double?)null;

        /// <summary>
        /// Gets the OBV (On Balance Volume) medium if it has changed, otherwise null.
        /// </summary>
        /// <returns>The OBV medium or null if unchanged.</returns>
        public long? OBV_Medium() => Changed("OBV_Medium") ? _current.OBV_Medium : (long?)null;
        /// <summary>
        /// Gets the OBV (On Balance Volume) long if it has changed, otherwise null.
        /// </summary>
        /// <returns>The OBV long or null if unchanged.</returns>
        public long? OBV_Long() => Changed("OBV_Long") ? _current.OBV_Long : (long?)null;

        /// <summary>
        /// Gets the yes bid center of mass if it has changed, otherwise null.
        /// </summary>
        /// <returns>The yes bid center of mass or null if unchanged.</returns>
        public double? YesBidCenterOfMass() => Changed("YesBidCenterOfMass") ? _current.YesBidCenterOfMass : (double?)null;
        /// <summary>
        /// Gets the no bid center of mass if it has changed, otherwise null.
        /// </summary>
        /// <returns>The no bid center of mass or null if unchanged.</returns>
        public double? NoBidCenterOfMass() => Changed("NoBidCenterOfMass") ? _current.NoBidCenterOfMass : (double?)null;

        /// <summary>
        /// Gets the market age if it has changed, otherwise null.
        /// </summary>
        /// <returns>The market age or null if unchanged.</returns>
        public TimeSpan? MarketAge() => Changed("MarketAge") ? _current.MarketAge : (TimeSpan?)null;
        /// <summary>
        /// Gets the time left if it has changed, otherwise null.
        /// </summary>
        /// <returns>The time left or null if unchanged.</returns>
        public TimeSpan? TimeLeft() => Changed("TimeLeft") ? _current.TimeLeft : (TimeSpan?)null;

        /// <summary>
        /// Gets whether change metrics are mature if it has changed, otherwise null.
        /// </summary>
        /// <returns>True if change metrics are mature, false otherwise, or null if unchanged.</returns>
        public bool? ChangeMetricsMature() => Changed("ChangeMetricsMature") ? _current.ChangeMetricsMature : (bool?)null;
        /// <summary>
        /// Gets whether the market can close early if it has changed, otherwise null.
        /// </summary>
        /// <returns>True if the market can close early, false otherwise, or null if unchanged.</returns>
        public bool? CanCloseEarly() => Changed("CanCloseEarly") ? _current.CanCloseEarly : (bool?)null;

        /// <summary>
        /// Gets the last WebSocket message received timestamp if it has changed, otherwise null.
        /// </summary>
        /// <returns>The last WebSocket message received timestamp or null if unchanged.</returns>
        public DateTime? LastWebSocketMessageReceived() => Changed("LastWebSocketMessageReceived") ? _current.LastWebSocketMessageReceived : (DateTime?)null;

        /// <summary>
        /// Gets the hold time if it has changed, otherwise null.
        /// </summary>
        /// <returns>The hold time or null if unchanged.</returns>
        public TimeSpan? HoldTime() => Changed("HoldTime") ? _current.HoldTime : (TimeSpan?)null;

        /// <summary>
        /// Gets the order volume per minute for yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The order volume per minute for yes bid or null if unchanged.</returns>
        public double? OrderVolumePerMinute_YesBid() => Changed("OrderVolumePerMinute_YesBid") ? _current.OrderVolumePerMinute_YesBid : (double?)null;
        /// <summary>
        /// Gets the order volume per minute for no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The order volume per minute for no bid or null if unchanged.</returns>
        public double? OrderVolumePerMinute_NoBid() => Changed("OrderVolumePerMinute_NoBid") ? _current.OrderVolumePerMinute_NoBid : (double?)null;

        /// <summary>
        /// Gets the tolerance percentage if it has changed, otherwise null.
        /// </summary>
        /// <returns>The tolerance percentage or null if unchanged.</returns>
        public double? TolerancePercentage() => Changed("TolerancePercentage") ? _current.TolerancePercentage : (double?)null;

        /// <summary>
        /// Gets the market behavior for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The market behavior for yes or null if unchanged.</returns>
        public string? MarketBehaviorYes() => Changed("MarketBehaviorYes") ? _current.MarketBehaviorYes : null;
        /// <summary>
        /// Gets the market behavior for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The market behavior for no or null if unchanged.</returns>
        public string? MarketBehaviorNo() => Changed("MarketBehaviorNo") ? _current.MarketBehaviorNo : null;
        /// <summary>
        /// Gets the good/bad price for yes if it has changed, otherwise null.
        /// </summary>
        /// <returns>The good/bad price for yes or null if unchanged.</returns>
        public string? GoodBadPriceYes() => Changed("GoodBadPriceYes") ? _current.GoodBadPriceYes : null;
        /// <summary>
        /// Gets the good/bad price for no if it has changed, otherwise null.
        /// </summary>
        /// <returns>The good/bad price for no or null if unchanged.</returns>
        public string? GoodBadPriceNo() => Changed("GoodBadPriceNo") ? _current.GoodBadPriceNo : null;

        // All-time & recent highs/lows (tuple-based)
        /// <summary>
        /// Gets the bid value of the all-time high yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid value of the all-time high yes bid or null if unchanged.</returns>
        public int? AllTimeHighYes_Bid_Bid() => ChangedTuple("AllTimeHighYes_Bid", 1, "Bid") ? _current.AllTimeHighYes_Bid.Bid : (int?)null;
        /// <summary>
        /// Gets the when timestamp of the all-time high yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The when timestamp of the all-time high yes bid or null if unchanged.</returns>
        public DateTime? AllTimeHighYes_Bid_When() => ChangedTuple("AllTimeHighYes_Bid", 2, "When") ? _current.AllTimeHighYes_Bid.When : (DateTime?)null;
        /// <summary>
        /// Gets the bid value of the all-time low yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid value of the all-time low yes bid or null if unchanged.</returns>
        public int? AllTimeLowYes_Bid_Bid() => ChangedTuple("AllTimeLowYes_Bid", 1, "Bid") ? _current.AllTimeLowYes_Bid.Bid : (int?)null;
        /// <summary>
        /// Gets the when timestamp of the all-time low yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The when timestamp of the all-time low yes bid or null if unchanged.</returns>
        public DateTime? AllTimeLowYes_Bid_When() => ChangedTuple("AllTimeLowYes_Bid", 2, "When") ? _current.AllTimeLowYes_Bid.When : (DateTime?)null;

        /// <summary>
        /// Gets the bid value of the all-time high no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid value of the all-time high no bid or null if unchanged.</returns>
        public int? AllTimeHighNo_Bid_Bid() => ChangedTuple("AllTimeHighNo_Bid", 1, "Bid") ? _current.AllTimeHighNo_Bid.Bid : (int?)null;
        /// <summary>
        /// Gets the when timestamp of the all-time high no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The when timestamp of the all-time high no bid or null if unchanged.</returns>
        public DateTime? AllTimeHighNo_Bid_When() => ChangedTuple("AllTimeHighNo_Bid", 2, "When") ? _current.AllTimeHighNo_Bid.When : (DateTime?)null;
        /// <summary>
        /// Gets the bid value of the all-time low no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid value of the all-time low no bid or null if unchanged.</returns>
        public int? AllTimeLowNo_Bid_Bid() => ChangedTuple("AllTimeLowNo_Bid", 1, "Bid") ? _current.AllTimeLowNo_Bid.Bid : (int?)null;
        /// <summary>
        /// Gets the when timestamp of the all-time low no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The when timestamp of the all-time low no bid or null if unchanged.</returns>
        public DateTime? AllTimeLowNo_Bid_When() => ChangedTuple("AllTimeLowNo_Bid", 2, "When") ? _current.AllTimeLowNo_Bid.When : (DateTime?)null;

        /// <summary>
        /// Gets the bid value of the recent high yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid value of the recent high yes bid or null if unchanged.</returns>
        public int? RecentHighYes_Bid_Bid() => ChangedTuple("RecentHighYes_Bid", 1, "Bid") ? _current.RecentHighYes_Bid.Bid : (int?)null;
        /// <summary>
        /// Gets the when timestamp of the recent high yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The when timestamp of the recent high yes bid or null if unchanged.</returns>
        public DateTime? RecentHighYes_Bid_When() => ChangedTuple("RecentHighYes_Bid", 2, "When") ? _current.RecentHighYes_Bid.When : (DateTime?)null;
        /// <summary>
        /// Gets the bid value of the recent low yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid value of the recent low yes bid or null if unchanged.</returns>
        public int? RecentLowYes_Bid_Bid() => ChangedTuple("RecentLowYes_Bid", 1, "Bid") ? _current.RecentLowYes_Bid.Bid : (int?)null;
        /// <summary>
        /// Gets the when timestamp of the recent low yes bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The when timestamp of the recent low yes bid or null if unchanged.</returns>
        public DateTime? RecentLowYes_Bid_When() => ChangedTuple("RecentLowYes_Bid", 2, "When") ? _current.RecentLowYes_Bid.When : (DateTime?)null;

        /// <summary>
        /// Gets the bid value of the recent high no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid value of the recent high no bid or null if unchanged.</returns>
        public int? RecentHighNo_Bid_Bid() => ChangedTuple("RecentHighNo_Bid", 1, "Bid") ? _current.RecentHighNo_Bid.Bid : (int?)null;
        /// <summary>
        /// Gets the when timestamp of the recent high no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The when timestamp of the recent high no bid or null if unchanged.</returns>
        public DateTime? RecentHighNo_Bid_When() => ChangedTuple("RecentHighNo_Bid", 2, "When") ? _current.RecentHighNo_Bid.When : (DateTime?)null;
        /// <summary>
        /// Gets the bid value of the recent low no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The bid value of the recent low no bid or null if unchanged.</returns>
        public int? RecentLowNo_Bid_Bid() => ChangedTuple("RecentLowNo_Bid", 1, "Bid") ? _current.RecentLowNo_Bid.Bid : (int?)null;
        /// <summary>
        /// Gets the when timestamp of the recent low no bid if it has changed, otherwise null.
        /// </summary>
        /// <returns>The when timestamp of the recent low no bid or null if unchanged.</returns>
        public DateTime? RecentLowNo_Bid_When() => ChangedTuple("RecentLowNo_Bid", 2, "When") ? _current.RecentLowNo_Bid.When : (DateTime?)null;
    }
}
