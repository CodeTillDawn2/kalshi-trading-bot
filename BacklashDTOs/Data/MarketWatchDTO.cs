namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for market watch data.
    /// </summary>
    public class MarketWatchDTO
    {
        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public string? market_ticker { get; set; }

        /// <summary>
        /// Gets or sets the brain lock identifier.
        /// </summary>
        public Guid? BrainLock { get; set; }

        /// <summary>
        /// Gets or sets the interest score.
        /// </summary>
        public double? InterestScore { get; set; }

        /// <summary>
        /// Gets or sets the interest score date.
        /// </summary>
        public DateTime? InterestScoreDate { get; set; }

        /// <summary>
        /// Gets or sets the last watched timestamp.
        /// </summary>
        public DateTime? LastWatched { get; set; }

        /// <summary>
        /// Gets or sets the average WebSocket events per minute.
        /// </summary>
        public double? AverageWebsocketEventsPerMinute { get; set; }
    }
}
