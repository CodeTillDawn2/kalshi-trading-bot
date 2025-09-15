using BacklashDTOs;
using System.Diagnostics;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Counterattack candlestick pattern, a two-candle reversal pattern.
    /// Bullish: Indicates a potential reversal from a downtrend to an uptrend.
    /// Bearish: Indicates a potential reversal from an uptrend to a downtrend.
    /// Requirements (Source: TradingView, BabyPips):
    /// - Occurs after a clear trend (downtrend for bullish, uptrend for bearish), measured via TrendDirectionRatio.
    /// - First candle: Strong move in trend direction (bearish for bullish, bullish for bearish).
    /// - Second candle: Opposite direction, similar size, closes near first open, not engulfing.
    /// Optimized for ML: Relative scaling based on lookback average range, loosened for broader detection across timeframes.
    /// </summary>
    public class CounterattackPattern2 : PatternDefinition
    {
        /// <summary>
        /// Number of candles required to form the pattern.
        /// Purpose: Defines the fixed size of the Counterattack pattern (two candles).
        /// Default: 2 (standard for Counterattack pattern)
        /// </summary>
        public static int PatternSize { get; } = 2;

        /// <summary>
        /// Minimum body size for both candles relative to the lookback average range, varies by timeframe.
        /// Purpose: Ensures significant movement compared to prior volatility, adjusted for minute, hour, day.
        /// Default: 0.25 (minute), 0.35 (hour), 0.45 (day)
        /// Range: 0.2 0.5 (0.2 for very loose significance, 0.5 for stronger signals).
        /// </summary>
        /// Note: Tightened to eliminate FP in backtesting.
        public static double GetMinBodyToAvgRangeRatio(int intervalType)
        {
            if (intervalType == 2) return 0.01;
            if (intervalType == 3) return 0.01;
            return 0.01; // Assume minute for others (e.g., no suffix or custom)
        }

        /// <summary>
        /// Gets or sets the minimum ratio of the price movement (drop for bearish, rise for bullish) from the first candle's close
        /// to the second candle's close, relative to the average range. This ensures a significant reversal move.
        /// Default is 0.5, meaning the drop or rise must be at least half the average range of the lookback period.
        /// </summary>
        /// <remarks>
        /// For a bearish pattern, this represents the minimum drop from the first candle's close to the second candle's close.
        /// For a bullish pattern, this represents the minimum rise from the first candle's close to the second candle's close.
        /// Increase this value (e.g., to 0.6 or 0.7) to exclude patterns with smaller reversals; decrease it (e.g., to 0.3 or 0.4)
        /// to include patterns with less pronounced moves.
        /// </remarks>
        public static double MinDropToAvgRangeRatio { get; set; } = 0.1;

        /// <summary>
        /// Minimum ratio of body size to total candle range for both candles.
        /// Purpose: Ensures notable bodies relative to range (per TradingView), loosened for flexibility.
        /// Default: 0.3 (adjusted from 0.4 for broader detection)
        /// Range: 0.2 0.5 (0.2 for very loose, 0.5 for stricter).
        /// </summary>
        public static double BodyToRangeRatio { get; set; } = 0.2;

        /// <summary>
        /// Gets or sets the maximum distance between the second candle s close and the first candle s open, 
        /// relative to the lookback average range, for the Counterattack pattern.
        /// </summary>
        /// <remarks>
        /// This property defines how close the second candle s close must be to the first candle s open to qualify 
        /// as a Counterattack pattern, ensuring the reversal intent is clear without engulfing the first candle. 
        /// The value is multiplied by the lookback average range to scale with volatility, but an absolute minimum 
        /// (e.g., 2.0 units) may be applied to accommodate patterns in low-volatility contexts. 
        /// The default value of 3.0 allows flexibility for broader detection across timeframes, increased from 1.5 
        /// to capture true positives with slightly larger distances (e.g., up to 3 times the average range). 
        /// Range: 1.0 3.0 (1.0 for stricter proximity, 3.0 for looser, broader detection).
        /// </remarks>
        /// <value>The default value is 3.0, representing 3 times the lookback average range.</value>
        public static double NearOpenCloseToAvgRange { get; set; } = 4;

        /// <summary>
        /// Minimum trend strength threshold relative to the lookback average range.
        /// Purpose: Confirms a mild preceding trend.
        /// Default: 0.1 (10% of average range, loosened for flexibility)
        /// Range: 0.05 0.2 (0.05 for very mild trends, 0.2 for stronger trends).
        /// </summary>
        public static double TrendThresholdToAvgRange { get; set; } = 0.0025;

        /// <summary>
        /// Minimum trend consistency over the lookback period.
        /// Purpose: Ensures a mildly consistent trend before the pattern, loosened for broader detection.
        /// Default: 0.2 (20% consistency, reduced from 0.3)
        /// Range: 0.1 0.4 (0.1 for very loose, 0.4 for stricter).
        /// </summary>
        public static double TrendConsistencyMin { get; set; } = 0.05;

        /// <summary>
        /// Minimum trend direction ratio in the lookback period to confirm a prior trend.
        /// Purpose: Ensures a consistent prior trend (downtrend for bullish, uptrend for bearish) before the pattern.
        /// Default: 0.5 (50% of candles in trend direction, loosened for flexibility)
        /// Range: 0.4 0.7 (0.4 for very loose consistency, 0.7 for stronger trends).
        /// </summary>
        public static double TrendDirectionRatioMin { get; set; } = 0.05;


        /// <summary>
        /// Gets or sets the minimum ratio of the second candle's body size to the first candle's body size in a Counterattack pattern.
        /// Purpose: Ensures the reversal candle has sufficient magnitude relative to the trend candle to qualify as a counterattack.
        /// </summary>
        /// <remarks>
        /// This property enforces a balance between the two candles, requiring the second candle's body size to be at least this fraction 
        /// of the first candle's body size. For a bearish pattern, it ensures the bearish reversal is significant compared to the prior 
        /// bullish move; for a bullish pattern, it ensures the bullish reversal matches the prior bearish move. 
        /// A higher value (e.g., 0.75) demands a stronger reversal, potentially reducing false positives but missing subtler patterns. 
        /// A lower value (e.g., 0.25) allows weaker reversals, increasing detection but risking noise. 
        /// The default of 0.5 strikes a balance, suitable for a 0 100 price scale where significant moves are expected.
        /// </remarks>
        /// <value>
        /// The default value is 0.5, meaning the second candle s body must be at least 50% of the first candle s body size.
        /// Range: 0.1 1.0 (0.1 for very loose balance, 1.0 for near-equal bodies).
        /// </value>
        public static double MinBodyBalanceRatio { get; set; } = 0.3;

        public const string BaseName = "Counterattack";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        private readonly bool IsBullish;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public CounterattackPattern2(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Determines if a Counterattack pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the second candle in the pattern.</param>
        /// <param name="trendLookback">Number of candles to look back for trend and average range.</param>
        /// <param name="prices">Array of candle price data.</param>
        /// <param name="metricsCache">Cache of candle metrics.</param>
        /// <param name="isBullish">True for bullish pattern, false for bearish.</param>
        /// <returns>A CounterattackPattern instance if detected, otherwise null.</returns>
        public static async Task<CounterattackPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache,
            bool isBullish)
        {
            // Ensure enough data for pattern and trend lookback
            if (index < PatternSize + trendLookback - 1 || index >= prices.Length) return null;
            int startIndex = index - (PatternSize - 1);
            var candles = new List<int> { startIndex, index };

            // Get metrics
            var prevMetrics = await GetCandleMetricsAsync(metricsCache, startIndex, prices, trendLookback, false);
            var currMetrics = await GetCandleMetricsAsync(metricsCache, index, prices, trendLookback, true);
            var previousPrices = prices[startIndex];
            var currentPrices = prices[index];


            if (currMetrics.BodySize < MinBodyBalanceRatio * prevMetrics.BodySize) return null;

            // Scaling with lookback average range
            double avgRange = currMetrics.LookbackAvgRange[PatternSize - 1];
            double minBodyToAvgRangeRatio = GetMinBodyToAvgRangeRatio(currMetrics.IntervalType);

            // Check candle sizes (scaled)
            if (prevMetrics.BodySize < minBodyToAvgRangeRatio * avgRange ||
                prevMetrics.BodyToRangeRatio < BodyToRangeRatio) return null;
            if (currMetrics.BodySize < minBodyToAvgRangeRatio * avgRange ||
                currMetrics.BodyToRangeRatio < BodyToRangeRatio) return null;

            // Direction checks
            if (isBullish ? !prevMetrics.IsBearish : !prevMetrics.IsBullish) return null;
            if (isBullish ? !currMetrics.IsBullish : !currMetrics.IsBearish) return null;

            // Conditions for bearish counterattack
            if (!isBullish)
            {
                if (currentPrices.Close >= previousPrices.Close) return null;
                double dropSize = previousPrices.Close - currentPrices.Close;
                if (dropSize < MinDropToAvgRangeRatio * avgRange) return null;
            }
            else
            {
                if (currentPrices.Close <= previousPrices.Close) return null;
                double riseSize = currentPrices.Close - previousPrices.Close;
                if (riseSize < MinDropToAvgRangeRatio * avgRange) return null;
            }

            // [CHANGED] Proximity check: Only check close-to-open with absolute cap
            if (Math.Abs(currentPrices.Close - previousPrices.Open) > NearOpenCloseToAvgRange * avgRange) return null;

            // Not engulfing
            if (isBullish ? (currentPrices.Open < previousPrices.Close && currentPrices.Close > previousPrices.Open) :
                            (currentPrices.Open > previousPrices.Close && currentPrices.Close < previousPrices.Open)) return null;

            // [CHANGED] Trend checks: Add minimum meanTrend for bearish
            double meanTrend = currMetrics.GetLookbackMeanTrend(PatternSize);
            double trendConsistency = currMetrics.GetLookbackTrendConsistency(PatternSize);
            double trendDirectionRatio = isBullish ? currMetrics.BearishRatio[PatternSize - 1] : currMetrics.BullishRatio[PatternSize - 1];
            if (isBullish)
            {
                if (meanTrend > -TrendThresholdToAvgRange * avgRange ||
                    trendConsistency < TrendConsistencyMin ||
                    trendDirectionRatio < TrendDirectionRatioMin) return null;
            }
            else
            {
                if (meanTrend < TrendThresholdToAvgRange * avgRange ||  // Ensure positive trend
                    meanTrend <= 0.05 * avgRange ||                     // [NEW] Minimum trend strength
                    trendConsistency < TrendConsistencyMin ||
                    trendDirectionRatio < TrendDirectionRatioMin) return null;
            }

            return new CounterattackPattern(candles, isBullish);
        }
    }
}


