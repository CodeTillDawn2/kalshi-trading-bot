using System.ComponentModel.DataAnnotations;
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
        /// The configuration section name for SnapshotPeriodHelperConfig.
        /// </summary>
        public const string SectionName = "WatchedMarkets:SnapshotPeriodHelper";

        /// <summary>
        /// The maximum time gap in minutes between snapshots that allows them to be considered
        /// part of the same continuous period without requiring price stability checks.
        /// </summary>
        [Required(ErrorMessage = "The 'SmallGapMinutes' is missing in the configuration.")]
        public double SmallGapMinutes { get; set; }

        /// <summary>
        /// The maximum time gap in hours for active market periods. Gaps exceeding this threshold
        /// will cause a period break regardless of price stability.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxActiveGapHours' is missing in the configuration.")]
        public double MaxActiveGapHours { get; set; }

        /// <summary>
        /// The price change threshold in points for determining significant price movements.
        /// Used to break snapshot groups when price changes exceed this threshold.
        /// </summary>
        [Required(ErrorMessage = "The 'PriceChangeThreshold' is missing in the configuration.")]
        public int PriceChangeThreshold { get; set; }
    }
}
