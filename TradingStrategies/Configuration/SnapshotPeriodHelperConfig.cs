using System.Text.Json.Serialization;

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
        required
        public double SmallGapMinutes { get; set; }

        /// <summary>
        /// The maximum time gap in hours for active market periods. Gaps exceeding this threshold
        /// will cause a period break regardless of price stability.
        /// </summary>
        required
        public double MaxActiveGapHours { get; set; }

        /// <summary>
        /// The price change threshold in points for determining significant price movements.
        /// Used to break snapshot groups when price changes exceed this threshold.
        /// </summary>
        required
        public int PriceChangeThreshold { get; set; }
    }
}
