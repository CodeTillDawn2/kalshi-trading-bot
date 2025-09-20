using System;
using System.Text.Json.Serialization;

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
        required
        public double SmoothingOffset { get; set; }

        /// <summary>
        /// Minimum allowed lookback period for trend calculations.
        /// Prevents calculations with insufficient historical data.
        /// Default value: 1
        /// </summary>
        required
        public int MinLookback { get; set; }

        /// <summary>
        /// Maximum allowed lookback period for trend calculations.
        /// Limits computational complexity for very long lookbacks.
        /// Default value: 1000
        /// </summary>
        required
        public int MaxLookback { get; set; }

        /// <summary>
        /// Default lookback period when not specified.
        /// Used as fallback for methods that don't require explicit lookback.
        /// Default value: 14
        /// </summary>
        required
        public int DefaultLookback { get; set; }

        /// <summary>
        /// Minimum allowed pattern size.
        /// Ensures pattern size is at least 1 to prevent division by zero.
        /// Default value: 1
        /// </summary>
        required
        public int MinPatternSize { get; set; }

        /// <summary>
        /// Maximum allowed pattern size.
        /// Limits pattern size to reasonable values.
        /// Default value: 10
        /// </summary>
        required
        public int MaxPatternSize { get; set; }

        /// <summary>
        /// Array of lookback periods used in PatternUtils for multi-period calculations.
        /// These periods are used to compute metrics at different timeframes.
        /// Default values: [1, 2, 3, 4, 5]
        /// </summary>
        required
        public int[] LookbackPeriods { get; set; }

        /// <summary>
        /// Number of lookback periods to use in calculations.
        /// Should match the length of LookbackPeriods array.
        /// Default value: 5
        /// </summary>
        public int LookbackPeriodCount => LookbackPeriods.Length;
    }
}
