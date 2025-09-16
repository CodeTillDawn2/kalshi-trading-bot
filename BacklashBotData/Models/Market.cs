

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
        public required string market_ticker { get; set; }
        public required string event_ticker { get; set; }
        public required string market_type { get; set; }
        public required string title { get; set; }
        public string? subtitle { get; set; }
        public required string yes_sub_title { get; set; }
        public required string no_sub_title { get; set; }
        public DateTime open_time { get; set; }
        public DateTime close_time { get; set; }
        public DateTime? expected_expiration_time { get; set; }
        public DateTime expiration_time { get; set; }
        public DateTime latest_expiration_time { get; set; }
        public int settlement_timer_seconds { get; set; }
        public required string status { get; set; }
        public required string response_price_units { get; set; }
        public int notional_value { get; set; }
        public int tick_size { get; set; }
        public int yes_bid { get; set; }
        public int yes_ask { get; set; }
        public int no_bid { get; set; }
        public int no_ask { get; set; }
        public int last_price { get; set; }
        public int previous_yes_bid { get; set; }
        public int previous_yes_ask { get; set; }
        public int previous_price { get; set; }
        public long volume { get; set; }
        public int volume_24h { get; set; }
        public long liquidity { get; set; }
        public int open_interest { get; set; }
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
