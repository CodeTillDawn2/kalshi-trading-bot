using Microsoft.Extensions.Logging;

namespace BacklashPatterns
{
    /// <summary>
    /// Represents the metrics calculated for a candle.
    /// </summary>
    public record struct CandleMetrics
    {
        private static ILogger? _logger;

        /// <summary>
        /// Sets the logger for the CandleMetrics class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public static void SetLogger(ILogger logger) => _logger = logger;

        /// <summary>
        /// Gets the size of the candle body.
        /// </summary>
        public double BodySize { get; init; }
        /// <summary>
        /// Gets the length of the upper wick.
        /// </summary>
        public double UpperWick { get; init; }
        /// <summary>
        /// Gets the length of the lower wick.
        /// </summary>
        public double LowerWick { get; init; }
        /// <summary>
        /// Gets the total range of the candle (high - low).
        /// </summary>
        public double TotalRange { get; init; }
        /// <summary>
        /// Gets a value indicating whether the candle is bullish.
        /// </summary>
        public bool IsBullish { get; init; }
        /// <summary>
        /// Gets a value indicating whether the candle is bearish.
        /// </summary>
        public bool IsBearish { get; init; }

        /// <summary>
        /// Gets the ratio of body size to total range.
        /// </summary>
        public double BodyToRangeRatio { get; init; }
        /// <summary>
        /// Gets the midpoint of the candle.
        /// </summary>
        public double MidPoint { get; init; }
        /// <summary>
        /// Gets a value indicating whether the candle has no upper wick.
        /// </summary>
        public bool HasNoUpperWick { get; init; }
        /// <summary>
        /// Gets a value indicating whether the candle has no lower wick.
        /// </summary>
        public bool HasNoLowerWick { get; init; }
        /// <summary>
        /// Gets the midpoint of the candle body.
        /// </summary>
        public double BodyMidPoint { get; init; }
        /// <summary>
        /// Mean Trend for the candle at this index if the pattern is index - 1 long.. e.g. Single candlestick pattern mean is at index 0
        /// </summary>
        public double[]? LookbackMeanTrend { get; init; }
        /// <summary>
        /// Gets the lookback trend consistency.
        /// </summary>
        public double[]? LookbackTrendConsistency { get; init; }
        /// <summary>
        /// Gets the average volume vs lookback.
        /// </summary>
        public double[]? AvgVolumeVsLookback { get; init; }
        /// <summary>
        /// Gets the lookback average range.
        /// </summary>
        public double[]? LookbackAvgRange { get; init; }
        /// <summary>
        /// Gets the bullish ratio.
        /// </summary>
        public double[]? BullishRatio { get; init; }
        /// <summary>
        /// Gets the bearish ratio.
        /// </summary>
        public double[]? BearishRatio { get; init; }
        /// <summary>
        /// Gets the interval type.
        /// </summary>
        public int IntervalType { get; init; }
        /// <summary>
        /// Time taken to calculate these metrics in milliseconds.
        /// </summary>
        public long CalculationTimeMs { get; init; }

        /// <summary>
        /// Gets the lookback mean trend for the given pattern length.
        /// </summary>
        /// <param name="patternLength">The pattern length.</param>
        /// <returns>The lookback mean trend.</returns>
        public double GetLookbackMeanTrend(int patternLength)
        {
            if (LookbackMeanTrend == null || patternLength - 1 >= LookbackMeanTrend.Length)
            {
                _logger?.LogWarning("Invalid access to LookbackMeanTrend array. PatternLength: {PatternLength}, ArrayLength: {ArrayLength}", patternLength, LookbackMeanTrend?.Length ?? 0);
                return 0;
            }
            return LookbackMeanTrend[patternLength - 1];
        }

        /// <summary>
        /// Gets the lookback trend consistency for the given pattern length.
        /// </summary>
        /// <param name="patternLength">The pattern length.</param>
        /// <returns>The lookback trend consistency.</returns>
        public double GetLookbackTrendConsistency(int patternLength)
        {
            if (LookbackTrendConsistency == null || patternLength - 1 >= LookbackTrendConsistency.Length)
            {
                _logger?.LogWarning("Invalid access to LookbackTrendConsistency array. PatternLength: {PatternLength}, ArrayLength: {ArrayLength}", patternLength, LookbackTrendConsistency?.Length ?? 0);
                return 0;
            }
            return LookbackTrendConsistency[patternLength - 1];
        }

        /// <summary>
        /// Gets the average volume vs lookback for the given pattern length.
        /// </summary>
        /// <param name="patternLength">The pattern length.</param>
        /// <returns>The average volume vs lookback.</returns>
        public double GetAvgVolumeVsLookback(int patternLength)
        {
            if (AvgVolumeVsLookback == null || patternLength - 1 >= AvgVolumeVsLookback.Length)
            {
                _logger?.LogWarning("Invalid access to AvgVolumeVsLookback array. PatternLength: {PatternLength}, ArrayLength: {ArrayLength}", patternLength, AvgVolumeVsLookback?.Length ?? 0);
                return 0;
            }
            return AvgVolumeVsLookback[patternLength - 1];
        }

        /// <summary>
        /// Gets the lookback average range for the given pattern length.
        /// </summary>
        /// <param name="patternLength">The pattern length.</param>
        /// <returns>The lookback average range.</returns>
        public double GetLookbackAvgRange(int patternLength)
        {
            if (LookbackAvgRange == null || patternLength - 1 >= LookbackAvgRange.Length)
            {
                _logger?.LogWarning("Invalid access to LookbackAvgRange array. PatternLength: {PatternLength}, ArrayLength: {ArrayLength}", patternLength, LookbackAvgRange?.Length ?? 0);
                return 0;
            }
            return LookbackAvgRange[patternLength - 1];
        }

    }
}


