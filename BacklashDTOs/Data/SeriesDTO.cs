namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for series data.
    /// </summary>
    public class SeriesDTO
    {
        /// <summary>
        /// Gets or sets the series ticker symbol.
        /// </summary>
        public string? series_ticker { get; set; }

        /// <summary>
        /// Gets or sets the frequency of the series.
        /// </summary>
        public string? frequency { get; set; }

        /// <summary>
        /// Gets or sets the title of the series.
        /// </summary>
        public string? title { get; set; }

        /// <summary>
        /// Gets or sets the category of the series.
        /// </summary>
        public string? category { get; set; }

        /// <summary>
        /// Gets or sets the contract URL.
        /// </summary>
        public string? contract_url { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the collection of series tags.
        /// </summary>
        public ICollection<SeriesTagDTO>? Tags { get; set; }

        /// <summary>
        /// Gets or sets the collection of series settlement sources.
        /// </summary>
        public ICollection<SeriesSettlementSourceDTO>? SettlementSources { get; set; }
    }
}
