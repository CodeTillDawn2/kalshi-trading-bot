namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents a settlement source reference for a trading series.
    /// This entity links a series to external data sources that will be used to
    /// determine the final outcome and settlement value of the series contracts.
    /// Settlement sources provide the authoritative data that resolves the market
    /// and determines payouts to winning positions.
    /// </summary>
    public class SeriesSettlementSource
    {
        /// <summary>
        /// Gets or sets the series ticker identifier that this settlement source belongs to.
        /// This links the settlement source to its parent series.
        /// </summary>
        public string series_ticker { get; set; }

        /// <summary>
        /// Gets or sets the name or title of this settlement source.
        /// This provides a human-readable description of the data source.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Gets or sets the URL or reference to the external data source.
        /// This points to the authoritative source that will determine the settlement value.
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Series entity.
        /// This provides access to the parent series and related settlement information.
        /// </summary>
        public Series Series { get; set; }
    }
}
