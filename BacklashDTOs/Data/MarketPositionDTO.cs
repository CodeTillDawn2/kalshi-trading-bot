namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for market position data.
    /// </summary>
    public class MarketPositionDTO
    {
        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public string Ticker { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total traded amount.
        /// </summary>
        public long TotalTraded { get; set; }

        /// <summary>
        /// Gets or sets the current position.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets or sets the market exposure.
        /// </summary>
        public long MarketExposure { get; set; }

        /// <summary>
        /// Gets or sets the realized profit and loss.
        /// </summary>
        public long RealizedPnl { get; set; }

        /// <summary>
        /// Gets or sets the count of resting orders.
        /// </summary>
        public int RestingOrdersCount { get; set; }

        /// <summary>
        /// Gets or sets the fees paid.
        /// </summary>
        public long FeesPaid { get; set; }

        /// <summary>
        /// Gets or sets the last updated timestamp in UTC.
        /// </summary>
        public DateTime LastUpdatedUTC { get; set; }

        /// <summary>
        /// Gets or sets the last modified timestamp.
        /// </summary>
        public DateTime? LastModified { get; set; }
    }
}
