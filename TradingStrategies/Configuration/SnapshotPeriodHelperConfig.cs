using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration class for SnapshotPeriodHelper settings.
    /// Contains parameters for determining snapshot period boundaries based on time gaps and price changes.
    /// </summary>
    public class SnapshotPeriodHelperConfig
    {
        /// <summary>
        /// The maximum time gap in minutes between snapshots that allows them to be considered
        /// part of the same continuous period without requiring price stability checks.
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "SmallGapMinutes must be greater than 0.")]
        public double SmallGapMinutes { get; set; } = 10.0;

        /// <summary>
        /// The maximum time gap in hours for active market periods. Gaps exceeding this threshold
        /// will cause a period break regardless of price stability.
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "MaxActiveGapHours must be greater than 0.")]
        public double MaxActiveGapHours { get; set; } = 1.0;

        /// <summary>
        /// The price change threshold in points for determining significant price movements.
        /// Used to break snapshot groups when price changes exceed this threshold.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "PriceChangeThreshold must be at least 1.")]
        public int PriceChangeThreshold { get; set; } = 3;
    }
}