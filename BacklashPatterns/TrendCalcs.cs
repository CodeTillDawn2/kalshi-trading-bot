using BacklashDTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace BacklashPatterns
{
    /// <summary>
    /// Provides static utility methods for calculating various trend-related metrics and ratios
    /// used in candlestick pattern analysis. This class serves as a computational foundation for
    /// assessing market trends, consistency, volume patterns, and directional biases over specified
    /// lookback periods. All methods are designed to be thread-safe and efficient for real-time
    /// pattern detection algorithms.
    ///
    /// Configuration is handled via TrendCalculationConfig, which can be set using SetConfig method.
    /// Logging is supported via ILogger, set using SetLogger method.
    /// Async versions are available for long-running calculations to improve performance in high-volume scenarios.
    /// IMPORTANT: When using async methods, callers must await them to ensure calculations complete
    /// before dependent operations begin.
    /// </summary>
    public static class TrendCalcs
    {
        private static TrendCalculationConfig _config = new TrendCalculationConfig();
        private static ILogger _logger;

        /// <summary>
        /// Sets the configuration for TrendCalcs calculations.
        /// </summary>
        /// <param name="config">The configuration options.</param>
        public static void SetConfig(TrendCalculationConfig config)
        {
            _config = config ?? new TrendCalculationConfig();
        }

        /// <summary>
        /// Sets the logger for TrendCalcs.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the current configuration for TrendCalcs.
        /// </summary>
        /// <returns>The current TrendCalculationConfig instance.</returns>
        public static TrendCalculationConfig GetConfig()
        {
            return _config;
        }

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
            // Input validation
            if (prices == null)
            {
                _logger?.LogWarning("CalculatePriorTrendStrength: prices array is null");
                return 0.0;
            }
            if (index < 0 || index >= prices.Length)
            {
                _logger?.LogWarning("CalculatePriorTrendStrength: invalid index {Index}, array length {Length}", index, prices.Length);
                return 0.0;
            }
            if (trendLookback < _config.MinLookback || trendLookback > _config.MaxLookback)
            {
                _logger?.LogWarning("CalculatePriorTrendStrength: trendLookback {TrendLookback} out of range [{Min}, {Max}]", trendLookback, _config.MinLookback, _config.MaxLookback);
                trendLookback = Math.Clamp(trendLookback, _config.MinLookback, _config.MaxLookback);
            }
            if (patternSize < _config.MinPatternSize || patternSize > _config.MaxPatternSize)
            {
                _logger?.LogWarning("CalculatePriorTrendStrength: patternSize {PatternSize} out of range [{Min}, {Max}]", patternSize, _config.MinPatternSize, _config.MaxPatternSize);
                patternSize = Math.Clamp(patternSize, _config.MinPatternSize, _config.MaxPatternSize);
            }

            if (index < patternSize) return 0.0;

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
            // Input validation
            if (prices == null)
            {
                _logger?.LogWarning("CalculateAverageTrendOverLookbackPeriod: prices array is null");
                return 0.0;
            }
            if (index < 0 || index >= prices.Length)
            {
                _logger?.LogWarning("CalculateAverageTrendOverLookbackPeriod: invalid index {Index}, array length {Length}", index, prices.Length);
                return 0.0;
            }
            if (lookback < _config.MinLookback || lookback > _config.MaxLookback)
            {
                _logger?.LogWarning("CalculateAverageTrendOverLookbackPeriod: lookback {Lookback} out of range [{Min}, {Max}]", lookback, _config.MinLookback, _config.MaxLookback);
                lookback = Math.Clamp(lookback, _config.MinLookback, _config.MaxLookback);
            }
            if (patternSize < _config.MinPatternSize || patternSize > _config.MaxPatternSize)
            {
                _logger?.LogWarning("CalculateAverageTrendOverLookbackPeriod: patternSize {PatternSize} out of range [{Min}, {Max}]", patternSize, _config.MinPatternSize, _config.MaxPatternSize);
                patternSize = Math.Clamp(patternSize, _config.MinPatternSize, _config.MaxPatternSize);
            }

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
            // Input validation
            if (prices == null)
            {
                _logger?.LogWarning("CalculateTrendConsistencyRatio: prices array is null");
                return 0.0;
            }
            if (index < 0 || index >= prices.Length)
            {
                _logger?.LogWarning("CalculateTrendConsistencyRatio: invalid index {Index}, array length {Length}", index, prices.Length);
                return 0.0;
            }
            if (lookback < _config.MinLookback || lookback > _config.MaxLookback)
            {
                _logger?.LogWarning("CalculateTrendConsistencyRatio: lookback {Lookback} out of range [{Min}, {Max}]", lookback, _config.MinLookback, _config.MaxLookback);
                lookback = Math.Clamp(lookback, _config.MinLookback, _config.MaxLookback);
            }
            if (patternSize < _config.MinPatternSize || patternSize > _config.MaxPatternSize)
            {
                _logger?.LogWarning("CalculateTrendConsistencyRatio: patternSize {PatternSize} out of range [{Min}, {Max}]", patternSize, _config.MinPatternSize, _config.MaxPatternSize);
                patternSize = Math.Clamp(patternSize, _config.MinPatternSize, _config.MaxPatternSize);
            }

            if (index < patternSize) return 0.0;

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
            double stepSize = _config.SmoothingOffset / lookbackCount;
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
            // Input validation
            if (prices == null)
            {
                _logger?.LogWarning("CalculateAverageRangeOverLookbackPeriod: prices array is null");
                return 0.0;
            }
            if (index < 0 || index >= prices.Length)
            {
                _logger?.LogWarning("CalculateAverageRangeOverLookbackPeriod: invalid index {Index}, array length {Length}", index, prices.Length);
                return 0.0;
            }
            if (lookback < _config.MinLookback || lookback > _config.MaxLookback)
            {
                _logger?.LogWarning("CalculateAverageRangeOverLookbackPeriod: lookback {Lookback} out of range [{Min}, {Max}]", lookback, _config.MinLookback, _config.MaxLookback);
                lookback = Math.Clamp(lookback, _config.MinLookback, _config.MaxLookback);
            }
            if (patternSize < _config.MinPatternSize || patternSize > _config.MaxPatternSize)
            {
                _logger?.LogWarning("CalculateAverageRangeOverLookbackPeriod: patternSize {PatternSize} out of range [{Min}, {Max}]", patternSize, _config.MinPatternSize, _config.MaxPatternSize);
                patternSize = Math.Clamp(patternSize, _config.MinPatternSize, _config.MaxPatternSize);
            }

            if (index < patternSize) return 0.0;

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
            // Input validation
            if (prices == null)
            {
                _logger?.LogWarning("CalculateVolumeRatioToHistoricalAverage: prices array is null");
                return 0.0;
            }
            if (index < 0 || index >= prices.Length)
            {
                _logger?.LogWarning("CalculateVolumeRatioToHistoricalAverage: invalid index {Index}, array length {Length}", index, prices.Length);
                return 0.0;
            }
            if (lookback < _config.MinLookback || lookback > _config.MaxLookback)
            {
                _logger?.LogWarning("CalculateVolumeRatioToHistoricalAverage: lookback {Lookback} out of range [{Min}, {Max}]", lookback, _config.MinLookback, _config.MaxLookback);
                lookback = Math.Clamp(lookback, _config.MinLookback, _config.MaxLookback);
            }
            if (patternSize < _config.MinPatternSize || patternSize > _config.MaxPatternSize)
            {
                _logger?.LogWarning("CalculateVolumeRatioToHistoricalAverage: patternSize {PatternSize} out of range [{Min}, {Max}]", patternSize, _config.MinPatternSize, _config.MaxPatternSize);
                patternSize = Math.Clamp(patternSize, _config.MinPatternSize, _config.MaxPatternSize);
            }

            if (index < 1) return 0.0;

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
            // Input validation
            if (prices == null)
            {
                _logger?.LogWarning("CalculateBullishCandleRatio: prices array is null");
                return 0.0;
            }
            if (index < 0 || index >= prices.Length)
            {
                _logger?.LogWarning("CalculateBullishCandleRatio: invalid index {Index}, array length {Length}", index, prices.Length);
                return 0.0;
            }
            if (lookback < _config.MinLookback || lookback > _config.MaxLookback)
            {
                _logger?.LogWarning("CalculateBullishCandleRatio: lookback {Lookback} out of range [{Min}, {Max}]", lookback, _config.MinLookback, _config.MaxLookback);
                lookback = Math.Clamp(lookback, _config.MinLookback, _config.MaxLookback);
            }

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
            double stepSize = totalCount > 0 ? _config.SmoothingOffset / totalCount : 0;
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
            // Input validation
            if (prices == null)
            {
                _logger?.LogWarning("CalculateTrendDirectionRatio: prices array is null");
                return 0.0;
            }
            if (index < 0 || index >= prices.Length)
            {
                _logger?.LogWarning("CalculateTrendDirectionRatio: invalid index {Index}, array length {Length}", index, prices.Length);
                return 0.0;
            }
            if (lookback < _config.MinLookback || lookback > _config.MaxLookback)
            {
                _logger?.LogWarning("CalculateTrendDirectionRatio: lookback {Lookback} out of range [{Min}, {Max}]", lookback, _config.MinLookback, _config.MaxLookback);
                lookback = Math.Clamp(lookback, _config.MinLookback, _config.MaxLookback);
            }
            if (patternSize < _config.MinPatternSize || patternSize > _config.MaxPatternSize)
            {
                _logger?.LogWarning("CalculateTrendDirectionRatio: patternSize {PatternSize} out of range [{Min}, {Max}]", patternSize, _config.MinPatternSize, _config.MaxPatternSize);
                patternSize = Math.Clamp(patternSize, _config.MinPatternSize, _config.MaxPatternSize);
            }

            if (index < patternSize) return 0.0;

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
            double stepSize = _config.SmoothingOffset / lookbackCount;
            double smoothedRatio = unweightedRatio + stepSize / 2.0; // Midpoint smoothing
            return Math.Min(smoothedRatio, 1.0);
        }

        // Async versions for long-running calculations
        // These use Task.Run to execute CPU-bound work on background threads
        // Callers should await these methods to ensure completion before proceeding

        /// <summary>
        /// Async version of CalculatePriorTrendStrength.
        /// Executes the calculation on a background thread to avoid blocking the calling thread.
        /// The caller must await this method to ensure the calculation completes before proceeding.
        /// </summary>
        public static async Task<double> CalculatePriorTrendStrengthAsync(
            int index,
            int trendLookback,
            CandleMids[] prices,
            int patternSize)
        {
            return await Task.Run(() => CalculatePriorTrendStrength(index, trendLookback, prices, patternSize));
        }

        /// <summary>
        /// Async version of CalculateAverageTrendOverLookbackPeriod.
        /// Executes the calculation on a background thread to avoid blocking the calling thread.
        /// The caller must await this method to ensure the calculation completes before proceeding.
        /// </summary>
        public static async Task<double> CalculateAverageTrendOverLookbackPeriodAsync(
            CandleMids[] prices,
            int index,
            int lookback,
            int patternSize)
        {
            return await Task.Run(() => CalculateAverageTrendOverLookbackPeriod(prices, index, lookback, patternSize));
        }

        /// <summary>
        /// Async version of CalculateTrendConsistencyRatio.
        /// Executes the calculation on a background thread to avoid blocking the calling thread.
        /// The caller must await this method to ensure the calculation completes before proceeding.
        /// </summary>
        public static async Task<double> CalculateTrendConsistencyRatioAsync(
            int index,
            int lookback,
            CandleMids[] prices,
            int patternSize)
        {
            return await Task.Run(() => CalculateTrendConsistencyRatio(index, lookback, prices, patternSize));
        }

        /// <summary>
        /// Async version of CalculateAverageRangeOverLookbackPeriod.
        /// Executes the calculation on a background thread to avoid blocking the calling thread.
        /// The caller must await this method to ensure the calculation completes before proceeding.
        /// </summary>
        public static async Task<double> CalculateAverageRangeOverLookbackPeriodAsync(
            int index,
            int lookback,
            CandleMids[] prices,
            int patternSize)
        {
            return await Task.Run(() => CalculateAverageRangeOverLookbackPeriod(index, lookback, prices, patternSize));
        }

        /// <summary>
        /// Async version of CalculateVolumeRatioToHistoricalAverage.
        /// Executes the calculation on a background thread to avoid blocking the calling thread.
        /// The caller must await this method to ensure the calculation completes before proceeding.
        /// </summary>
        public static async Task<double> CalculateVolumeRatioToHistoricalAverageAsync(
            CandleMids[] prices,
            int index,
            int lookback,
            int patternSize)
        {
            return await Task.Run(() => CalculateVolumeRatioToHistoricalAverage(prices, index, lookback, patternSize));
        }

        /// <summary>
        /// Async version of CalculateBullishCandleRatio.
        /// Executes the calculation on a background thread to avoid blocking the calling thread.
        /// The caller must await this method to ensure the calculation completes before proceeding.
        /// </summary>
        public static async Task<double> CalculateBullishCandleRatioAsync(int index, int lookback, CandleMids[] prices)
        {
            return await Task.Run(() => CalculateBullishCandleRatio(index, lookback, prices));
        }

        /// <summary>
        /// Async version of CalculateTrendDirectionRatio.
        /// Executes the calculation on a background thread to avoid blocking the calling thread.
        /// The caller must await this method to ensure the calculation completes before proceeding.
        /// </summary>
        public static async Task<double> CalculateTrendDirectionRatioAsync(
            int index,
            int lookback,
            CandleMids[] prices,
            int patternSize,
            bool isBullishDirection)
        {
            return await Task.Run(() => CalculateTrendDirectionRatio(index, lookback, prices, patternSize, isBullishDirection));
        }

    }
}








