namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents the watch status and monitoring configuration for a specific market.
    /// This entity tracks which markets are being actively monitored by the trading bot,
    /// including interest scores, brain instance assignments, and activity metrics.
    /// MarketWatch entities are crucial for managing the bot's market coverage and
    /// optimizing resource allocation across different trading opportunities.
    /// </summary>
    public class MarketWatch
    {
        /// <summary>
        /// Gets or sets the market ticker identifier for this watch entry.
        /// This uniquely identifies the market being monitored.
        /// </summary>
        public required string market_ticker { get; set; }

        /// <summary>
        /// Gets or sets the brain instance GUID that has locked this market for exclusive monitoring.
        /// When set, only the specified brain instance can trade or monitor this market.
        /// </summary>
        public Guid? BrainLock { get; set; }

        /// <summary>
        /// Gets or sets the calculated interest score for this market.
        /// This score represents the market's attractiveness for trading based on
        /// volume, volatility, and other market characteristics.
        /// </summary>
        public double? InterestScore { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the interest score was last calculated.
        /// This indicates the freshness of the interest score data.
        /// </summary>
        public DateTime? InterestScoreDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this market was last actively watched or monitored.
        /// This helps track market engagement and can be used for cleanup of stale watches.
        /// </summary>
        public DateTime? LastWatched { get; set; }

        /// <summary>
        /// Gets or sets the average number of WebSocket events received per minute for this market.
        /// This metric indicates market activity level and can be used for resource allocation decisions.
        /// </summary>
        public double? AverageWebsocketEventsPerMinute { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Market entity.
        /// This provides access to the full market details and trading data.
        /// </summary>
        public Market? Market { get; set; }
    }
}
