namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for market snapshot data.
    /// </summary>
    public class SnapshotDTO
    {
        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public string MarketTicker { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time of the snapshot.
        /// </summary>
        public DateTime SnapshotDate { get; set; }

        /// <summary>
        /// Gets or sets the JSON schema version.
        /// </summary>
        public int JSONSchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets whether the change metrics are mature.
        /// </summary>
        public bool ChangeMetricsMature { get; set; }

        /// <summary>
        /// Gets or sets the position size.
        /// </summary>
        public int PositionSize { get; set; }

        /// <summary>
        /// Gets or sets the velocity per minute for top yes bid.
        /// </summary>
        public double? VelocityPerMinute_Top_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity per minute for top no bid.
        /// </summary>
        public double? VelocityPerMinute_Top_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity per minute for bottom yes bid.
        /// </summary>
        public double? VelocityPerMinute_Bottom_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity per minute for bottom no bid.
        /// </summary>
        public double? VelocityPerMinute_Bottom_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the order volume for yes bid.
        /// </summary>
        public double? OrderVolume_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the order volume for no bid.
        /// </summary>
        public double? OrderVolume_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the trade volume for yes.
        /// </summary>
        public double? TradeVolume_Yes { get; set; }

        /// <summary>
        /// Gets or sets the trade volume for no.
        /// </summary>
        public double? TradeVolume_No { get; set; }

        /// <summary>
        /// Gets or sets the average trade size for yes.
        /// </summary>
        public double? AverageTradeSize_Yes { get; set; }

        /// <summary>
        /// Gets or sets the average trade size for no.
        /// </summary>
        public double? AverageTradeSize_No { get; set; }

        /// <summary>
        /// Gets or sets the market type identifier.
        /// </summary>
        public int? MarketTypeID { get; set; }

        /// <summary>
        /// Gets or sets whether the snapshot is validated.
        /// </summary>
        public bool? IsValidated { get; set; }

        /// <summary>
        /// Gets or sets the raw JSON data.
        /// </summary>
        public string RawJSON { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the brain instance identifier.
        /// </summary>
        public string? BrainInstance { get; set; }
    }
}
