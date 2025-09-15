namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for snapshot group data.
    /// </summary>
    public class SnapshotGroupDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the snapshot group.
        /// </summary>
        public int SnapshotGroupID { get; set; }

        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public string? MarketTicker { get; set; }

        /// <summary>
        /// Gets or sets the start time of the snapshot group.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the snapshot group.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the yes start value.
        /// </summary>
        public int YesStart { get; set; }

        /// <summary>
        /// Gets or sets the no start value.
        /// </summary>
        public int NoStart { get; set; }

        /// <summary>
        /// Gets or sets the yes end value.
        /// </summary>
        public int YesEnd { get; set; }

        /// <summary>
        /// Gets or sets the no end value.
        /// </summary>
        public int NoEnd { get; set; }

        /// <summary>
        /// Gets or sets the snapshot schema version.
        /// </summary>
        public int SnapshotSchema { get; set; }

        /// <summary>
        /// Gets or sets the JSON path.
        /// </summary>
        public string? JsonPath { get; set; }

        /// <summary>
        /// Gets or sets the average liquidity.
        /// </summary>
        public double AverageLiquidity { get; set; }

        /// <summary>
        /// Gets or sets the processed timestamp.
        /// </summary>
        public DateTime ProcessedDttm { get; set; }
    }
}
