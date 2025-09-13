using BacklashDTOs;

namespace BacklashPatterns
{
    /// <summary>
    /// Provides static utility methods for calculating various trend-related metrics and ratios
    /// used in candlestick pattern analysis. This class serves as a computational foundation for
    /// assessing market trends, consistency, volume patterns, and directional biases over specified
    /// lookback periods. All methods are designed to be thread-safe and efficient for real-time
    /// pattern detection algorithms.
    /// </summary>
    public static class TrendCalcs
    {

        /// <summary>
        /// Calculates the strength of the prior trend before a pattern.
        /// Purpose: Measures the net price change over the lookback period, adjusted for expected direction.
        /// Behavior: Returns a positive value for uptrends (bearish Belt Hold) and a negative value for downtrends (bullish Belt Hold).
        /// Use Case: Used in BeltHoldPattern to assess if the prior trend is strong enough to support a reversal.
        /// </summary>
        /// <param name="index">The current candle index (end of lookback period).</param>
        /// <param name="trendLookback">Number of candles to look back.</param>
        /// <param name="prices">Array of candle data.</param>
        /// <param name="patternSize">Number of candles in the pattern (e.g., 1 for Belt Hold).</param>
        /// <returns>The net trend change (positive for uptrend, negative for downtrend, or zero if insufficient data).</returns>
        public static double CalculatePriorTrendStrength(
            int index,
            int trendLookback,
            CandleMids[] prices,
            int patternSize)
        {
            if (index < patternSize) return 0.0;
            patternSize = Math.Max(1, patternSize);

            int lookbackStart = Math.Max(0, index - (patternSize - 1) - trendLookback);
            int lookbackEnd = index - patternSize;
            int lookbackCount = Math.Min(trendLookback, lookbackEnd - lookbackStart + 1);

            if (lookbackCount <= 0) return 0.0;

            // Calculate total trend change (close of last lookback candle minus open of first)
            double totalTrendChange = prices[index - 1].Close - prices[lookbackStart].Open;
            return totalTrendChange;
        }

        /// <summary>
        /// Calculates the average trend change over a specified lookback period before a pattern.
        /// This method computes the mean price movement per candle to assess overall trend direction.
        /// </summary>
        /// <param name="prices">Array of candle data.</param>
        /// <param name="index">The current candle index.</param>
        /// <param name="lookback">Number of candles to look back.</param>
        /// <param name="patternSize">Number of candles in the pattern.</param>
        /// <returns>The average trend change per candle, or zero if insufficient data.</returns>
        public static double CalculateAverageTrendOverLookbackPeriod(
                    CandleMids[] prices,
                    int index,
                    int lookback,
                    int patternSize)
        {
            // Start lookback before the pattern begins
            int lookbackStart = Math.Max(0, index - (patternSize - 1) - lookback);
            // Number of lookback periods, adjusted for pattern size and array bounds
            int lookbackEnd = index - patternSize; // Last candle before pattern
            int lookbackCount = Math.Min(lookback, lookbackEnd - lookbackStart + 1);

            if (lookbackCount <= 0) return 0.0;

            double totalChange = 0;
            for (int i = lookbackStart; i <= lookbackEnd; i++)
            {
                double openMidpoint = prices[i].Open;
                double closeMidpoint = prices[i].Close; // Single candle trend
                totalChange += closeMidpoint - openMidpoint;
            }

            return totalChange / lookbackCount;
        }

        /// <summary>
        /// Calculates the consistency ratio of trend direction over a lookback period.
        /// This method measures how consistently the market has been trending in one direction,
        /// providing a smoothed ratio between 0 and 1.
        /// </summary>
        /// <param name="index">The current candle index.</param>
        /// <param name="lookback">Number of candles to look back.</param>
        /// <param name="prices">Array of candle data.</param>
        /// <param name="patternSize">Number of candles in the pattern.</param>
        /// <returns>A smoothed ratio (0-1) indicating trend consistency, or zero if insufficient data.</returns>
        public static double CalculateTrendConsistencyRatio(
            int index,
            int lookback,
            CandleMids[] prices,
            int patternSize)
        {
            if (index < patternSize) return 0.0;
            patternSize = Math.Max(1, patternSize);

            int lookbackStart = Math.Max(0, index - (patternSize - 1) - lookback);
            int lookbackEnd = index - patternSize;
            int lookbackCount = Math.Min(lookback, lookbackEnd - lookbackStart + 1);

            if (lookbackCount <= 0) return 0.0;

            int consistentTrendCount = 0;
            for (int i = lookbackStart; i <= lookbackEnd; i++)
            {
                double change = prices[i].Close - prices[i].Open;
                consistentTrendCount += change > 0 ? 1 : change < 0 ? -1 : 0;
            }

            double unweightedRatio = (double)Math.Abs(consistentTrendCount) / lookbackCount;
            double stepSize = 1.0 / lookbackCount;
            double smoothedRatio = unweightedRatio + stepSize / 2.0;
            return Math.Min(smoothedRatio, 1.0);
        }

        /// <summary>
        /// Calculates the average price range over a specified lookback period before a pattern.
        /// This method computes the mean volatility (high-low difference) per candle to assess
        /// market volatility levels.
        /// </summary>
        /// <param name="index">The current candle index.</param>
        /// <param name="lookback">Number of candles to look back.</param>
        /// <param name="prices">Array of candle data.</param>
        /// <param name="patternSize">Number of candles in the pattern.</param>
        /// <returns>The average price range per candle, or zero if insufficient data.</returns>
        public static double CalculateAverageRangeOverLookbackPeriod(
            int index,
            int lookback,
            CandleMids[] prices,
            int patternSize)
        {
            if (index < patternSize) return 0.0;
            patternSize = Math.Max(1, patternSize);

            // Start lookback before the pattern begins
            int lookbackStart = Math.Max(0, index - (patternSize - 1) - lookback);
            int lookbackEnd = index - patternSize; // Last candle before pattern
            int lookbackCount = Math.Min(lookback, lookbackEnd - lookbackStart + 1);

            if (lookbackCount <= 0) return 0.0;

            double totalRangeSum = 0.0;
            for (int i = lookbackStart; i <= lookbackEnd; i++)
            {
                double candleRange = prices[i].High - prices[i].Low;
                totalRangeSum += candleRange;
            }

            return totalRangeSum / lookbackCount;
        }

        /// <summary>
        /// Calculates the ratio of current candle volume to historical average volume.
        /// This method compares the volume of the current candle against the average volume
        /// over a lookback period to identify unusual volume activity.
        /// </summary>
        /// <param name="prices">Array of candle data.</param>
        /// <param name="index">The current candle index.</param>
        /// <param name="lookback">Number of candles to look back for historical average.</param>
        /// <param name="patternSize">Number of candles in the pattern (kept for consistency).</param>
        /// <returns>The volume ratio (current/historical average), or infinity if historical average is zero.</returns>
        public static double CalculateVolumeRatioToHistoricalAverage(
            CandleMids[] prices,
            int index,
            int lookback,
            int patternSize) // patternSize might not be needed, kept for consistency
        {
            if (prices == null || index < 1) return 0.0;

            // Historical average volume per candle (lookback ending at index - 1)
            int lookbackStart = Math.Max(0, index - lookback);
            int lookbackEnd = index - 1; // Just before the current candle
            int historicalCandleCount = Math.Min(lookback, lookbackEnd - lookbackStart + 1);

            double historicalTotalVolume = 0.0;
            if (historicalCandleCount > 0)
            {
                for (int i = lookbackStart; i <= lookbackEnd && i < prices.Length; i++)
                {
                    historicalTotalVolume += prices[i].Volume;
                }
            }

            double historicalAvg = historicalCandleCount > 0 ? historicalTotalVolume / historicalCandleCount : 0.0;

            // Current candle volume (single candle at index)
            double currentVolume = prices[index].Volume;

            // Return the ratio of current volume to historical average
            if (historicalAvg == 0.0)
            {
                return currentVolume > 0.0 ? double.PositiveInfinity : 0.0; // Handle division by zero
            }
            return currentVolume / historicalAvg;
        }

        /// <summary>
        /// Calculates the ratio of bullish candles over a specified lookback period.
        /// This method determines the proportion of candles that closed higher than they opened,
        /// providing insight into bullish market sentiment.
        /// </summary>
        /// <param name="index">The current candle index.</param>
        /// <param name="lookback">Number of candles to look back.</param>
        /// <param name="prices">Array of candle data.</param>
        /// <returns>A smoothed ratio (0-1) of bullish candles, or zero if insufficient data.</returns>
        public static double CalculateBullishCandleRatio(int index, int lookback, CandleMids[] prices)
        {
            int bullishCount = 0;
            int totalCount = 0;
            int lookbackStart = Math.Max(0, index - lookback);
            int lookbackEnd = index - 1;
            for (int i = lookbackStart; i <= lookbackEnd && i < prices.Length; i++)
            {
                if (prices[i].Close > prices[i].Open) bullishCount++;
                totalCount++;
            }
            double unweightedRatio = totalCount > 0 ? (double)bullishCount / totalCount : 0;
            double stepSize = totalCount > 0 ? 1.0 / totalCount : 0;
            double smoothedRatio = unweightedRatio + stepSize / 2.0;
            return Math.Min(smoothedRatio, 1.0);
        }


        /// <summary>
        /// Calculates the proportion of candles in the specified direction over the lookback period before the pattern.
        /// Purpose: Measures the strength of a specific directional trend (bullish or bearish) to validate pattern prerequisites.
        /// Behavior: Returns the ratio of bullish candles if isBullishDirection is true, bearish if false.
        /// Use Case: Ensures a strong uptrend (bullish) for bearish Belt Hold or downtrend (bearish) for bullish Belt Hold.
        /// </summary>
        /// <param name="index">The current candle index.</param>
        /// <param name="lookback">The number of candles to look back.</param>
        /// <param name="prices">Array of candle data.</param>
        /// <param name="patternSize">Number of candles in the pattern (e.g., 1 for Belt Hold).</param>
        /// <param name="isBullishDirection">True to calculate bullish ratio, false for bearish ratio.</param>
        /// <returns>A value from 0 to 1 representing the proportion of candles in the specified direction.</returns>
        public static double CalculateTrendDirectionRatio(
            int index,
            int lookback,
            CandleMids[] prices,
            int patternSize,
            bool isBullishDirection)
        {
            if (index < patternSize) return 0.0;
            patternSize = Math.Max(1, patternSize);

            int lookbackStart = Math.Max(0, index - (patternSize - 1) - lookback);
            int lookbackEnd = index - patternSize;
            int lookbackCount = Math.Min(lookback, lookbackEnd - lookbackStart + 1);

            if (lookbackCount <= 0) return 0.0;

            int directionCount = 0;
            for (int i = lookbackStart; i <= lookbackEnd; i++)
            {
                double change = prices[i].Close - prices[i].Open;
                if (isBullishDirection && change > 0) directionCount++;
                else if (!isBullishDirection && change < 0) directionCount++;
            }

            double unweightedRatio = (double)directionCount / lookbackCount;
            double stepSize = 1.0 / lookbackCount;
            double smoothedRatio = unweightedRatio + stepSize / 2.0; // Midpoint smoothing
            return Math.Min(smoothedRatio, 1.0);
        }


    }
}








