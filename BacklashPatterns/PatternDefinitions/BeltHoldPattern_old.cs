using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Belt Hold candlestick pattern, a single-candle reversal pattern.
    /// Bullish: Indicates a potential reversal from a downtrend to an uptrend.
    /// Bearish: Indicates a potential reversal from an uptrend to a downtrend.
    /// Requirements (Source: BabyPips, TradingView):
    /// - Single candle with a large body (almost entire range).
    /// - Minimal shadows (small upper and lower wicks).
    /// - Occurs after a clear trend (downtrend for bullish, uptrend for bearish).
    /// Some traders require the next candle to confirm the reversal (e.g., a bullish candle after a Bullish Belt Hold). 
    /// Your single-candle check doesn�t include this.
    /// The pattern�s reliability often increases near support/resistance levels or after extreme price moves. Your code focuses solely on the candle and prior trend, not broader market context.
    //Suggestion: This might be out of scope for raw pattern detection, but you could flag patterns near key levels(e.g., 25, 50, 75 on a 0�100 scale) for your ML model.
    /// Many sources (e.g., Investopedia) suggest that a Belt Hold with higher-than-average volume strengthens the reversal signal. Your CandleMetrics calculates AvgVolumeVsLookback, but it�s not used in IsPattern.
    //Suggestion: Add an optional volume check(e.g., AvgVolumeVsLookback > 1.0) to filter for stronger signals, especially for machine learning where volume might be predictive.
    /// </summary>
    public class BeltHoldPattern_old : PatternDefinition
    {
        /// <summary>
        /// Minimum total range for the candle to be considered significant.
        /// Purpose: Ensures the candle has enough price movement to indicate a strong reversal.
        /// Default: 3.0
        /// Loosest Value: 1.0 (TradingView community notes that smaller ranges can still qualify in high-volatility contexts).
        /// </summary>
        public static double MinRange { get; set; } = 3.0;

        /// <summary>
        /// Minimum ratio of body size to total range for the candle.
        /// Purpose: Ensures the candle has a dominant body with minimal wicks, characteristic of a Belt Hold.
        /// Default: 0.9 (90%)
        /// Loosest Value: 0.7 (70%) (BabyPips allows a slightly smaller body ratio in less strict interpretations).
        /// </summary>
        public static double BodyRangeRatio { get; set; } = 0.8;

        /// <summary>
        /// Maximum size of upper and lower shadows (wicks).
        /// Purpose: Ensures minimal wicks, emphasizing the body�s dominance in the pattern.
        /// Default: 0.5
        /// Loosest Value: 1.0 (Investopedia suggests wicks up to 1.0 can still fit if the body is sufficiently large).
        /// </summary>
        public static double ShadowMax { get; set; } = .75;

        /// <summary>
        /// Minimum trend strength threshold in the lookback period.
        /// Purpose: Confirms the candle occurs after a clear prior trend, enhancing reversal significance.
        /// Default: 0.5
        /// Loosest Value: 0.3 (TradingView discussions indicate a weaker trend can still precede a valid Belt Hold).
        /// </summary>
        public static double TrendThreshold { get; set; } = 0.5;

        /// <summary>
        /// Minimum trend consistency in the lookback period.
        /// Purpose: Ensures the prior trend is steady, not erratic, for a reliable reversal signal.
        /// Default: 0.4 (40%)
        /// Loosest Value: 0.2 (20%) (Per BabyPips, a less consistent trend can still support the pattern in some cases).
        /// </summary>
        public static double TrendConsistencyMin { get; set; } = 0.25;

        /// <summary>
        /// Minimum absolute body size for the candle to be considered a valid Belt Hold.
        /// Purpose: Ensures the candle�s price movement (open to close) is significant enough to indicate a strong reversal, filtering out trivial changes.
        /// Default: 4.0
        /// Loosest Value: 3.0 (TradingView community notes that smaller bodies can still qualify in low-volatility markets with strong trend context).
        /// </summary>
        public static double MinBodySize { get; set; } = 6.0;

        public const string BaseName = "BeltHold";
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        private readonly bool IsBullish;
        public override double Strength { get; protected set; }
        public override double Certainty { get; protected set; }
        public override double Uncertainty { get; protected set; }

        public BeltHoldPattern_old(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        public static BeltHoldPattern IsPattern(
        int index,
        int trendLookback,
        CandleMids[] prices,
        Dictionary<int, CandleMetrics> metricsCache,
        bool isBullish)
        {

            if (index < 5) return null;
            CandleMids currentPrices = prices[index];

            CandleMetrics currentMetrics = GetCandleMetrics(ref metricsCache, index, prices, trendLookback, true);
            var candles = new List<int> { index };


            if (currentMetrics.TotalRange < MinRange) return null;
            double bodySize = currentMetrics.BodySize;
            if (bodySize < MinBodySize || bodySize < BodyRangeRatio * currentMetrics.TotalRange) return null;
            bool direction = isBullish ? currentMetrics.IsBullish : currentMetrics.IsBearish;
            if (!direction) return null;
            double upperShadow = isBullish ? currentMetrics.UpperWick : currentPrices.High - currentPrices.Open;
            double lowerShadow = isBullish ? currentPrices.Open - currentPrices.Low : currentMetrics.LowerWick;
            if (upperShadow > ShadowMax || lowerShadow > ShadowMax) return null;
            double meanTrend = currentMetrics.GetLookbackAverageTrend(1);
            double trendConsistency = currentMetrics.GetLookbackTrendStability(1);
            bool hasTrend = isBullish ? (meanTrend < -TrendThreshold && trendConsistency >= TrendConsistencyMin)
                                     : (meanTrend > TrendThreshold && trendConsistency >= TrendConsistencyMin);
            if (!hasTrend) return null;

            return new BeltHoldPattern(candles, isBullish);
        }
    }
}
