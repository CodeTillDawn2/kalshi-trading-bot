namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for event data.
    /// </summary>
    public class EventDTO
    {
        /// <summary>
        /// Gets or sets the event ticker symbol.
        /// </summary>
        public string? event_ticker { get; set; }

        /// <summary>
        /// Gets or sets the series ticker symbol.
        /// </summary>
        public string? series_ticker { get; set; }

        /// <summary>
        /// Gets or sets the event title.
        /// </summary>
        public string? title { get; set; }

        /// <summary>
        /// Gets or sets the event subtitle.
        /// </summary>
        public string? sub_title { get; set; }

        /// <summary>
        /// Gets or sets the collateral return type.
        /// </summary>
        public string? collateral_return_type { get; set; }

        /// <summary>
        /// Gets or sets whether the event is mutually exclusive.
        /// </summary>
        public bool mutually_exclusive { get; set; }

        /// <summary>
        /// Gets or sets the event category.
        /// </summary>
        public string? category { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the associated series data.
        /// </summary>
        public SeriesDTO? Series { get; set; }
    }
}
