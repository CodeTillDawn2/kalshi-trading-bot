using System;

namespace BacklashPatterns
{
    /// <summary>
    /// Configuration class for TrendCalcs calculation parameters.
    /// Contains settings for smoothing factors, lookback periods, and other calculation parameters
    /// used in trend-related calculations. These values can be configured via appsettings.json
    /// to allow runtime customization without code changes.
    /// </summary>
    public class TrendCalculationConfig
    {
        /// <summary>
        /// Offset factor used in smoothing calculations for trend ratios.
        /// This value is divided by 2.0 and added to the unweighted ratio for smoothing.
        /// Default value: 1.0
        /// </summary>
        public double SmoothingOffset { get; set; } = 1.0;

        /// <summary>
        /// Minimum allowed lookback period for trend calculations.
        /// Prevents calculations with insufficient historical data.
        /// Default value: 1
        /// </summary>
        public int MinLookback { get; set; } = 1;

        /// <summary>
        /// Maximum allowed lookback period for trend calculations.
        /// Limits computational complexity for very long lookbacks.
        /// Default value: 1000
        /// </summary>
        public int MaxLookback { get; set; } = 1000;

        /// <summary>
        /// Default lookback period when not specified.
        /// Used as fallback for methods that don't require explicit lookback.
        /// Default value: 14
        /// </summary>
        public int DefaultLookback { get; set; } = 14;

        /// <summary>
        /// Minimum allowed pattern size.
        /// Ensures pattern size is at least 1 to prevent division by zero.
        /// Default value: 1
        /// </summary>
        public int MinPatternSize { get; set; } = 1;

        /// <summary>
        /// Maximum allowed pattern size.
        /// Limits pattern size to reasonable values.
        /// Default value: 10
        /// </summary>
        public int MaxPatternSize { get; set; } = 10;
    }
}