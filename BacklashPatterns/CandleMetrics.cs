using Microsoft.Extensions.Logging;

namespace BacklashPatterns
{
    public record struct CandleMetrics
    {
        private static ILogger? _logger;

        public static void SetLogger(ILogger logger) => _logger = logger;

        public double BodySize { get; init; }
        public double UpperWick { get; init; }
        public double LowerWick { get; init; }
        public double TotalRange { get; init; }
        public bool IsBullish { get; init; }
        public bool IsBearish { get; init; }

        public double BodyToRangeRatio { get; init; }
        public double MidPoint { get; init; }
        public bool HasNoUpperWick { get; init; }
        public bool HasNoLowerWick { get; init; }
        public double BodyMidPoint { get; init; }
        /// <summary>
        /// Mean Trend for the candle at this index if the pattern is index - 1 long.. e.g. Single candlestick pattern mean is at index 0
        /// </summary>
        public double[]? LookbackMeanTrend { get; init; }
        public double[]? LookbackTrendConsistency { get; init; }
        public double[]? AvgVolumeVsLookback { get; init; }
        public double[]? LookbackAvgRange { get; init; }
        public double[]? BullishRatio { get; init; }
        public double[]? BearishRatio { get; init; }
        public int IntervalType { get; init; }
        /// <summary>
        /// Time taken to calculate these metrics in milliseconds.
        /// </summary>
        public long CalculationTimeMs { get; init; }

        public double GetLookbackMeanTrend(int patternLength)
        {
            if (LookbackMeanTrend == null || patternLength - 1 >= LookbackMeanTrend.Length)
            {
                _logger?.LogWarning("Invalid access to LookbackMeanTrend array. PatternLength: {PatternLength}, ArrayLength: {ArrayLength}", patternLength, LookbackMeanTrend?.Length ?? 0);
                return 0;
            }
            return LookbackMeanTrend[patternLength - 1];
        }

        public double GetLookbackTrendConsistency(int patternLength)
        {
            if (LookbackTrendConsistency == null || patternLength - 1 >= LookbackTrendConsistency.Length)
            {
                _logger?.LogWarning("Invalid access to LookbackTrendConsistency array. PatternLength: {PatternLength}, ArrayLength: {ArrayLength}", patternLength, LookbackTrendConsistency?.Length ?? 0);
                return 0;
            }
            return LookbackTrendConsistency[patternLength - 1];
        }

        public double GetAvgVolumeVsLookback(int patternLength)
        {
            if (AvgVolumeVsLookback == null || patternLength - 1 >= AvgVolumeVsLookback.Length)
            {
                _logger?.LogWarning("Invalid access to AvgVolumeVsLookback array. PatternLength: {PatternLength}, ArrayLength: {ArrayLength}", patternLength, AvgVolumeVsLookback?.Length ?? 0);
                return 0;
            }
            return AvgVolumeVsLookback[patternLength - 1];
        }

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


