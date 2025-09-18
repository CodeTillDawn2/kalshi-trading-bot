using BacklashDTOs;
using static BacklashPatterns.PatternUtils;

namespace BacklashPatterns.PatternDefinitions
{
    /// <summary>
    /// Represents a Tasuki Gap candlestick pattern.
    /// </summary>
    public class TasukiGapPattern : PatternDefinition
    {
        /// <summary>
        /// Threshold for determining trend strength. Positive for uptrend, negative for downtrend.
        /// Strictest: 0.5 (strong trend), Loosest: 0.05 (very weak trend).
        /// </summary>
        public static double TrendThreshold { get; } = 0.1;

        /// <summary>
        /// Minimum gap size between the first and second candles.
        /// Strictest: 1.0 (clear gap), Loosest: 0.1 (minimal gap).
        /// </summary>
        public static double MinGapSize { get; } = 0.5;
        /// <summary>
        /// Gets the base name of the pattern.
        /// </summary>
        public const string BaseName = "TasukiGap";
        /// <summary>
        /// Gets the name of the pattern.
        /// </summary>
        public override string Name => BaseName + (IsBullish ? "_Bullish" : "_Bearish");
        /// <summary>
        /// Gets the description of the pattern.
        /// </summary>
        public override string Description => IsBullish
            ? "A bullish continuation pattern in an uptrend with three candles where the second gaps up from the first and the third fills part of that gap, signaling continued upward momentum."
            : "A bearish continuation pattern in a downtrend with three candles where the second gaps down from the first and the third fills part of that gap, signaling continued downward momentum.";
        /// <summary>
        /// Gets the strength of the pattern.
        /// </summary>
        public override double Strength { get; protected set; }
        /// <summary>
        /// Gets the certainty of the pattern.
        /// </summary>
        public override double Certainty { get; protected set; }
        /// <summary>
        /// Gets the uncertainty of the pattern.
        /// </summary>
        public override double Uncertainty { get; protected set; }
        private readonly bool IsBullish;

        /// <summary>
        /// Initializes a new instance of the TasukiGapPattern class.
        /// </summary>
        /// <param name="candles">The list of candle indices.</param>
        /// <param name="isBullish">Whether the pattern is bullish.</param>
        public TasukiGapPattern(List<int> candles, bool isBullish) : base(candles)
        {
            IsBullish = isBullish;
        }

        /// <summary>
        /// Identifies a Tasuki Gap pattern, a three-candle continuation pattern.
        /// Bullish: Occurs in an uptrend; two bullish candles with a gap up between them, followed by a bearish candle that closes within the gap.
        /// Bearish: Occurs in a downtrend; two bearish candles with a gap down between them, followed by a bullish candle that closes within the gap.
        /// Indicates continuation of the prior trend after a brief pullback.
        /// Source: https://www.investopedia.com/terms/t/tasukigap.asp
        /// </summary>
        /// <summary>
        /// Determines if a Tasuki Gap pattern exists at the specified index.
        /// </summary>
        /// <param name="index">The index of the third candle.</param>
        /// <param name="trendLookback">The trend lookback period.</param>
        /// <param name="isBullish">Whether to check for bullish pattern.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <returns>A task that represents the asynchronous operation, containing the pattern if found, otherwise null.</returns>
        public static async Task<TasukiGapPattern?> IsPatternAsync(
            int index,
            int trendLookback,
            bool isBullish,
            CandleMids[] prices,
            Dictionary<int, CandleMetrics> metricsCache)
        {
            if (index < 2) return null; // Need 3 candles

            int c1 = index - 2; // First candle
            int c2 = index - 1; // Second candle
            int c3 = index;     // Third candle

            var metrics1 = await GetCandleMetricsAsync(metricsCache, c1, prices, trendLookback, false);
            var metrics2 = await GetCandleMetricsAsync(metricsCache, c2, prices, trendLookback, false);
            var metrics3 = await GetCandleMetricsAsync(metricsCache, c3, prices, trendLookback, true);

            // Trend check using LookbackMeanTrend (3 candles)
            bool hasTrend = isBullish ? metrics3.GetLookbackMeanTrend(3) > TrendThreshold
                                     : metrics3.GetLookbackMeanTrend(3) < -TrendThreshold;
            if (!hasTrend) return null;

            var ask1 = prices[c1];
            var ask2 = prices[c2];
            var ask3 = prices[c3];

            if (isBullish)
            {
                // First candle: Bullish or small/neutral if second is strongly bullish
                bool c1Direction = metrics1.IsBullish || (metrics1.BodySize < 1.0 && metrics2.IsBullish);
                if (!c1Direction) return null;

                // Gap up between c1 and c2 (relaxed to = 0.5)
                bool hasGap = ask2.Open > ask1.Close && (ask2.Open - ask1.Close) >= MinGapSize;
                if (!hasGap) return null;

                // Second candle: Bullish
                bool c2Direction = metrics2.IsBullish;
                if (!c2Direction) return null;

                // Third candle: Bearish, closes within gap (inclusive bounds)
                bool c3Direction = metrics3.IsBearish;
                bool c3InGap = ask1.Close <= ask3.Close && ask3.Close <= ask2.Open;
                if (!c3Direction || !c3InGap) return null;
            }
            else // Bearish
            {
                // First candle: Bearish or small/neutral if second is strongly bearish
                bool c1Direction = metrics1.IsBearish || (metrics1.BodySize < 1.0 && metrics2.IsBearish);
                if (!c1Direction) return null;

                // Gap down between c1 and c2 (relaxed to = 0.5)
                bool hasGap = ask2.Open < ask1.Close && (ask1.Close - ask2.Open) >= MinGapSize;
                if (!hasGap) return null;

                // Second candle: Bearish
                bool c2Direction = metrics2.IsBearish;
                if (!c2Direction) return null;

                // Third candle: Bullish, closes within gap (inclusive bounds)
                bool c3Direction = metrics3.IsBullish;
                bool c3InGap = ask2.Open <= ask3.Close && ask3.Close <= ask1.Close;
                if (!c3Direction || !c3InGap) return null;
            }

            var candles = new List<int> { c1, c2, c3 };
            return new TasukiGapPattern(candles, isBullish);
        }

        /// <summary>
        /// Calculates the strength of the pattern using historical cache for comparison.
        /// </summary>
        /// <param name="metricsCache">The metrics cache.</param>
        /// <param name="prices">The array of candle prices.</param>
        /// <param name="avgVolume">The average volume.</param>
        /// <param name="historicalCache">The historical pattern cache.</param>
        public void CalculateStrength(
            Dictionary<int, CandleMetrics> metricsCache,
            CandleMids[] prices,
            double avgVolume,
            HistoricalPatternCache historicalCache)
        {
            if (Candles.Count != 3)
                throw new InvalidOperationException("TasukiGapPattern must have exactly 3 candles.");

            int c1 = Candles[0], c2 = Candles[1], c3 = Candles[2];

            var metrics1 = metricsCache[c1];
            var metrics2 = metricsCache[c2];
            var metrics3 = metricsCache[c3];

            var prices1 = prices[c1];
            var prices2 = prices[c2];
            var prices3 = prices[c3];

            // Power Score: Based on gap size, third candle closing within gap, trend
            double gapScore = 0;
            if (IsBullish)
            {
                gapScore = (prices2.Open - prices1.Close) / MinGapSize;
                gapScore = Math.Min(gapScore, 1);
            }
            else
            {
                gapScore = (prices1.Close - prices2.Open) / MinGapSize;
                gapScore = Math.Min(gapScore, 1);
            }

            double inGapScore = 0;
            if (IsBullish)
            {
                if (prices1.Close <= prices3.Close && prices3.Close <= prices2.Open)
                    inGapScore = 1.0;
                else
                {
                    // Partial score based on how close it is
                    double distance = 0;
                    if (prices3.Close < prices1.Close)
                        distance = prices1.Close - prices3.Close;
                    else if (prices3.Close > prices2.Open)
                        distance = prices3.Close - prices2.Open;
                    inGapScore = Math.Max(0, 1 - (distance / (prices2.Open - prices1.Close)));
                }
            }
            else
            {
                if (prices2.Open <= prices3.Close && prices3.Close <= prices1.Close)
                    inGapScore = 1.0;
                else
                {
                    // Partial score based on how close it is
                    double distance = 0;
                    if (prices3.Close < prices2.Open)
                        distance = prices2.Open - prices3.Close;
                    else if (prices3.Close > prices1.Close)
                        distance = prices3.Close - prices1.Close;
                    inGapScore = Math.Max(0, 1 - (distance / (prices1.Close - prices2.Open)));
                }
            }

            double trendStrength = Math.Abs(metrics3.GetLookbackMeanTrend(3) - TrendThreshold) / Math.Abs(TrendThreshold);
            trendStrength = 1 - trendStrength; // Closer to threshold is better

            double volumeScore = 0.5; // Placeholder

            double wGap = 0.4, wInGap = 0.4, wTrend = 0.1, wVolume = 0.1;
            double powerScore = (wGap * gapScore + wInGap * inGapScore + wTrend * trendStrength + wVolume * volumeScore) /
                                (wGap + wInGap + wTrend + wVolume);

            // Match Score: Deviation from thresholds
            double trendDeviation = Math.Abs(metrics3.GetLookbackMeanTrend(3) - TrendThreshold) / Math.Abs(TrendThreshold);
            double matchScore = 1 - trendDeviation;
            matchScore = Math.Clamp(matchScore, 0, 1);

            // Use historical cache for comparative strength
            Strength = CalculateComparativeStrength(historicalCache, Name, powerScore, matchScore);
        }
    }
}








