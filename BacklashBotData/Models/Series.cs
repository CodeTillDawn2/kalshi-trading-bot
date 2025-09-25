namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents a series in the Kalshi trading platform, which is a collection of related events over time.
    /// Series organize events by theme or frequency (e.g., monthly economic indicators) for market discovery and strategy grouping.
    /// </summary>
    public class Series
    {
        /// <summary>
        /// Gets or sets the unique ticker symbol for this series (primary key).
        /// </summary>
        public required string series_ticker { get; set; }
        /// <summary>
        /// Gets or sets the frequency of events in this series (e.g., daily, monthly).
        /// </summary>
        public required string frequency { get; set; }
        /// <summary>
        /// Gets or sets the main title or description of the series.
        /// </summary>
        public required string title { get; set; }
        /// <summary>
        /// Gets or sets the category classification for the series (e.g., economics, politics).
        /// </summary>
        public required string category { get; set; }
        /// <summary>
        /// Gets or sets the URL for the contract details or documentation.
        /// </summary>
        public required string contract_url { get; set; }
        /// <summary>
        /// Gets or sets the date and time when this series record was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }
        /// <summary>
        /// Gets or sets the date and time when this series record was last modified.
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }
        /// <summary>
        /// Gets or sets the collection of tags associated with this series for metadata and filtering.
        /// </summary>
        public required ICollection<SeriesTag> Tags { get; set; }
        /// <summary>
        /// Gets or sets the collection of settlement sources for this series (e.g., data providers for outcome verification).
        /// </summary>
        public required ICollection<SeriesSettlementSource> SettlementSources { get; set; }
        /// <summary>
        /// Gets or sets the collection of events belonging to this series.
        /// </summary>
        public List<Event>? Events { get; set; }
    }
}
