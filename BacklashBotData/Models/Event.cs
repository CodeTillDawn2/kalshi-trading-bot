namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents an event in the Kalshi trading platform.
    /// An event groups related markets (e.g., yes/no outcomes for a binary question) and is associated with a series.
    /// Used for organizing trading data, market discovery, and strategy analysis in the bot.
    /// </summary>
    public class Event
    {
        /// <summary>
        /// Gets or sets the unique ticker symbol for this event (primary key).
        /// </summary>
        public required string event_ticker { get; set; }
        /// <summary>
        /// Gets or sets the ticker symbol of the parent series this event belongs to (foreign key).
        /// </summary>
        public required string series_ticker { get; set; }
        /// <summary>
        /// Gets or sets the main title or description of the event.
        /// </summary>
        public required string title { get; set; }
        /// <summary>
        /// Gets or sets the optional subtitle providing additional context for the event.
        /// </summary>
        public string? sub_title { get; set; }
        /// <summary>
        /// Gets or sets the type of collateral return for this event (e.g., how winnings are calculated).
        /// </summary>
        public required string collateral_return_type { get; set; }
        /// <summary>
        /// Gets or sets whether the outcomes in this event are mutually exclusive (e.g., only one can be true).
        /// </summary>
        public bool mutually_exclusive { get; set; }
        /// <summary>
        /// Gets or sets the category classification for the event (e.g., politics, weather).
        /// </summary>
        public required string category { get; set; }
        /// <summary>
        /// Gets or sets the date and time when this event record was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }
        /// <summary>
        /// Gets or sets the date and time when this event record was last modified.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }
        /// <summary>
        /// Gets or sets the collection of markets associated with this event.
        /// </summary>
        public List<Market>? Markets { get; set; }
        /// <summary>
        /// Gets or sets the parent series entity for this event.
        /// </summary>
        public Series? Series { get; set; }
    }
}
