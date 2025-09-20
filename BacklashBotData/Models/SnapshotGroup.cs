namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents a group of snapshots aggregated over a specific time period for a market.
    /// Used for analyzing trading performance, calculating metrics like position changes and liquidity over intervals,
    /// and exporting grouped data for backtesting or reporting in the Kalshi trading bot.
    /// Maps to the SnapshotGroups database table.
    /// </summary>
    public class SnapshotGroup
    {
        /// <summary>
        /// Gets or sets the unique identifier for this snapshot group (primary key).
        /// </summary>
        public int SnapshotGroupID { get; set; }

        /// <summary>
        /// Gets or sets the ticker symbol of the market this group pertains to (foreign key).
        /// </summary>
        public string MarketTicker { get; set; }

        /// <summary>
        /// Gets or sets the UTC start time of the period covered by this snapshot group.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the UTC end time of the period covered by this snapshot group.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the starting position size for the "yes" outcome at the beginning of the period.
        /// </summary>
        public int YesStart { get; set; }

        /// <summary>
        /// Gets or sets the starting position size for the "no" outcome at the beginning of the period.
        /// </summary>
        public int NoStart { get; set; }

        /// <summary>
        /// Gets or sets the ending position size for the "yes" outcome at the end of the period.
        /// </summary>
        public int YesEnd { get; set; }

        /// <summary>
        /// Gets or sets the ending position size for the "no" outcome at the end of the period.
        /// </summary>
        public int NoEnd { get; set; }

        /// <summary>
        /// Gets or sets the average liquidity across snapshots in this group.
        /// </summary>
        public double AverageLiquidity { get; set; }

        /// <summary>
        /// Gets or sets the schema version used for the snapshots in this group.
        /// </summary>
        public int SnapshotSchema { get; set; }

        /// <summary>
        /// Gets or sets the file path to the exported JSON data for this group.
        /// </summary>
        public string JsonPath { get; set; }

        /// <summary>
        /// Gets or sets the UTC date and time when this snapshot group was processed.
        /// </summary>
        public DateTime ProcessedDttm { get; set; }

        /// <summary>
        /// Gets or sets the associated market entity for this snapshot group.
        /// </summary>
        public Market Market { get; set; }
    }
}
