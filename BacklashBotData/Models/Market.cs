

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
/// <summary>Gets or sets the market_ticker.</summary>
/// <summary>Gets or sets the market_ticker.</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public required string market_ticker { get; set; }
/// <summary>Gets or sets the expiration_value.</summary>
/// <summary>Gets or sets the can_close_early.</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public required string event_ticker { get; set; }
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public required string market_type { get; set; }
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public required string title { get; set; }
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public string? subtitle { get; set; }
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public required string yes_sub_title { get; set; }
/// <summary>
/// </summary>
        public required string no_sub_title { get; set; }
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public DateTime open_time { get; set; }
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public DateTime close_time { get; set; }
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public DateTime? expected_expiration_time { get; set; }
/// <summary>
/// </summary>
        public DateTime expiration_time { get; set; }
/// <summary>
/// </summary>
        public DateTime latest_expiration_time { get; set; }
/// <summary>
/// </summary>
        public int settlement_timer_seconds { get; set; }
/// <summary>
/// </summary>
        public required string status { get; set; }
/// <summary>Gets or sets the response_price_units.</summary>
/// <summary>Gets or sets the response_price_units.</summary>
        public required string response_price_units { get; set; }
/// <summary>
/// </summary>
        public int notional_value { get; set; }
/// <summary>Gets or sets the yes_ask.</summary>
/// <summary>Gets or sets the yes_bid.</summary>
        public int tick_size { get; set; }
/// <summary>Gets or sets the last_price.</summary>
/// <summary>Gets or sets the no_bid.</summary>
        public int yes_bid { get; set; }
/// <summary>Gets or sets the previous_price.</summary>
/// <summary>Gets or sets the last_price.</summary>
        public int yes_ask { get; set; }
/// <summary>Gets or sets the liquidity.</summary>
/// <summary>Gets or sets the previous_yes_ask.</summary>
        public int no_bid { get; set; }
/// <summary>Gets or sets the can_close_early.</summary>
/// <summary>Gets or sets the volume.</summary>
        public int no_ask { get; set; }
/// <summary>Gets or sets the risk_limit_cents.</summary>
/// <summary>Gets or sets the liquidity.</summary>
        public int last_price { get; set; }
/// <summary>Gets or sets the rules_primary.</summary>
/// <summary>Gets or sets the result.</summary>
        public int previous_yes_bid { get; set; }
/// <summary>Gets or sets the LastModifiedDate.</summary>
/// <summary>Gets or sets the expiration_value.</summary>
        public int previous_yes_ask { get; set; }
/// <summary>Gets or sets the risk_limit_cents.</summary>
        public int previous_price { get; set; }
/// <summary>Gets or sets the MarketWatch.</summary>
/// <summary>Gets or sets the floor_strike.</summary>
        public long volume { get; set; }
/// <summary>Gets or sets the rules_secondary.</summary>
        public int volume_24h { get; set; }
/// <summary>Gets or sets the LastModifiedDate.</summary>
        public long liquidity { get; set; }
/// <summary>Gets or sets the APILastFetchedDate.</summary>
        public int open_interest { get; set; }
/// <summary>Gets or sets the Event.</summary>
        public required string result { get; set; }
        public bool can_close_early { get; set; }
        public required string expiration_value { get; set; }
        public required string category { get; set; }
        public int risk_limit_cents { get; set; }
        public required string strike_type { get; set; }
        public double? floor_strike { get; set; }
        public required string rules_primary { get; set; }
        public string? rules_secondary { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public DateTime? LastCandlestickUTC { get; set; }
        public DateTime? APILastFetchedDate { get; set; }

        public Event? Event { get; set; }
        public MarketWatch? MarketWatch { get; set; }

    }


}
