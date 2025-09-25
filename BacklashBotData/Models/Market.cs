namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents a tradable market with comprehensive pricing, volume, and status information.
    /// This entity serves as the core data model for all market-related operations in the trading bot,
    /// containing real-time and historical market data, order book information, and market metadata.
    /// Maps to the Markets database table and is central to the trading system's data model.
    /// </summary>
    public class Market
    {
        /// <summary>
        /// Gets or sets the unique ticker symbol for this market contract (primary key).
        /// </summary>
        public required string market_ticker { get; set; }
        /// <summary>
        /// Gets or sets the ticker symbol of the parent event this market belongs to (foreign key).
        /// </summary>
        public required string event_ticker { get; set; }
        /// <summary>
        /// Gets or sets the type of market (e.g., binary yes/no outcome).
        /// </summary>
        public required string market_type { get; set; }
        /// <summary>
        /// Gets or sets the main title or question for this market.
        /// </summary>
        public required string title { get; set; }
        /// <summary>
        /// Gets or sets the optional subtitle providing additional market details.
        /// </summary>
        public string? subtitle { get; set; }
        /// <summary>
        /// Gets or sets the subtitle for the "yes" outcome.
        /// </summary>
        public required string yes_sub_title { get; set; }
        /// <summary>
        /// Gets or sets the subtitle for the "no" outcome.
        /// </summary>
        public required string no_sub_title { get; set; }
        /// <summary>
        /// Gets or sets the UTC time when trading opens for this market.
        /// </summary>
        public DateTime open_time { get; set; }
        /// <summary>
        /// Gets or sets the UTC time when trading closes for this market.
        /// </summary>
        public DateTime close_time { get; set; }
        /// <summary>
        /// Gets or sets the expected UTC expiration time for the market outcome.
        /// </summary>
        public DateTime? expected_expiration_time { get; set; }
        /// <summary>
        /// Gets or sets the final UTC expiration time for settlement.
        /// </summary>
        public DateTime expiration_time { get; set; }
        /// <summary>
        /// Gets or sets the latest possible UTC expiration time.
        /// </summary>
        public DateTime latest_expiration_time { get; set; }
        /// <summary>
        /// Gets or sets the settlement timer duration in seconds.
        /// </summary>
        public int settlement_timer_seconds { get; set; }
        /// <summary>
        /// Gets or sets the current status of the market (e.g., open, closed, settled).
        /// </summary>
        public required string status { get; set; }
        /// <summary>
        /// Gets or sets the units for price responses (e.g., cents).
        /// </summary>
        public required string response_price_units { get; set; }
        /// <summary>
        /// Gets or sets the notional value of the market contract.
        /// </summary>
        public int notional_value { get; set; }
        /// <summary>
        /// Gets or sets the tick size for price increments.
        /// </summary>
        public int tick_size { get; set; }
        /// <summary>
        /// Gets or sets the current bid price for the "yes" outcome (in basis points).
        /// </summary>
        public int yes_bid { get; set; }
        /// <summary>
        /// Gets or sets the current ask price for the "yes" outcome (in basis points).
        /// </summary>
        public int yes_ask { get; set; }
        /// <summary>
        /// Gets or sets the current bid price for the "no" outcome (in basis points).
        /// </summary>
        public int no_bid { get; set; }
        /// <summary>
        /// Gets or sets the current ask price for the "no" outcome (in basis points).
        /// </summary>
        public int no_ask { get; set; }
        /// <summary>
        /// Gets or sets the last traded price (in basis points).
        /// </summary>
        public int last_price { get; set; }
        /// <summary>
        /// Gets or sets the previous "yes" bid price for comparison.
        /// </summary>
        public int previous_yes_bid { get; set; }
        /// <summary>
        /// Gets or sets the previous "yes" ask price for comparison.
        /// </summary>
        public int previous_yes_ask { get; set; }
        /// <summary>
        /// Gets or sets the previous last traded price.
        /// </summary>
        public int previous_price { get; set; }
        /// <summary>
        /// Gets or sets the total trading volume for this market.
        /// </summary>
        public long volume { get; set; }
        /// <summary>
        /// Gets or sets the 24-hour trading volume.
        /// </summary>
        public int volume_24h { get; set; }
        /// <summary>
        /// Gets or sets the current liquidity measure for the market.
        /// </summary>
        public long liquidity { get; set; }
        /// <summary>
        /// Gets or sets the open interest (outstanding contracts).
        /// </summary>
        public int open_interest { get; set; }
        /// <summary>
        /// Gets or sets the settlement result (e.g., yes/no outcome).
        /// </summary>
        public required string result { get; set; }
        /// <summary>
        /// Gets or sets whether the market can be closed early.
        /// </summary>
        public bool can_close_early { get; set; }
        /// <summary>
        /// Gets or sets the final expiration value for settlement.
        /// </summary>
        public required string expiration_value { get; set; }
        /// <summary>
        /// Gets or sets the category of the market (e.g., politics, finance).
        /// </summary>
        public required string category { get; set; }
        /// <summary>
        /// Gets or sets the risk limit in cents for trading this market.
        /// </summary>
        public int risk_limit_cents { get; set; }
        /// <summary>
        /// Gets or sets the type of strike price (e.g., floor, range).
        /// </summary>
        public required string strike_type { get; set; }
        /// <summary>
        /// Gets or sets the floor strike price if applicable.
        /// </summary>
        public double? floor_strike { get; set; }
        /// <summary>
        /// Gets or sets the primary rules or description for the market.
        /// </summary>
        public required string rules_primary { get; set; }
        /// <summary>
        /// Gets or sets the secondary rules or additional details.
        /// </summary>
        public string? rules_secondary { get; set; }
        /// <summary>
        /// Gets or sets the date and time when this market record was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }
        /// <summary>
        /// Gets or sets the date and time when this market record was last modified.
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }
        /// <summary>
        /// Gets or sets the UTC time of the last candlestick update.
        /// </summary>
        public DateTime? LastCandlestickUTC { get; set; }
        /// <summary>
        /// Gets or sets the UTC time of the last API fetch for this market.
        /// </summary>
        public DateTime? APILastFetchedDate { get; set; }
        /// <summary>
        /// Gets or sets the associated event entity for this market.
        /// </summary>
        public Event? Event { get; set; }
        /// <summary>
        /// Gets or sets the associated market watch entity for monitoring.
        /// </summary>
        public MarketWatch? MarketWatch { get; set; }
    }
}
