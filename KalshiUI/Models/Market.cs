
namespace KalshiUI.Models
{

    public class Market
    {
        public string market_ticker { get; set; }
        public string event_ticker { get; set; }
        public string market_type { get; set; }
        public string title { get; set; }
        public string? subtitle { get; set; }
        public string yes_sub_title { get; set; }
        public string no_sub_title { get; set; }
        public DateTime open_time { get; set; }
        public DateTime close_time { get; set; }
        public DateTime expected_expiration_time { get; set; }
        public DateTime expiration_time { get; set; }
        public DateTime latest_expiration_time { get; set; }
        public int settlement_timer_seconds { get; set; }
        public string status { get; set; }
        public string response_price_units { get; set; }
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
        public int volume { get; set; }
        public int volume_24h { get; set; }
        public int liquidity { get; set; }
        public int open_interest { get; set; }
        public string result { get; set; }
        public bool can_close_early { get; set; }
        public string expiration_value { get; set; }
        public string category { get; set; }
        public int risk_limit_cents { get; set; }
        public string strike_type { get; set; }
        public int? floor_strike { get; set; }
        public string rules_primary { get; set; }
        public string rules_secondary { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public DateTime? LastCandlestick { get; set; }

        public Event Event { get; set; }
        public List<Orderbook> Orderbooks { get; set; }
    }

}