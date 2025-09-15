namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for series settlement source data.
    /// </summary>
    public class SeriesSettlementSourceDTO
    {
        /// <summary>
        /// Gets or sets the series ticker symbol.
        /// </summary>
        public string? series_ticker { get; set; }

        /// <summary>
        /// Gets or sets the name of the settlement source.
        /// </summary>
        public string? name { get; set; }

        /// <summary>
        /// Gets or sets the URL of the settlement source.
        /// </summary>
        public string? url { get; set; }
    }
}
