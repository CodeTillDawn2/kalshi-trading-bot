namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for market data.
    /// </summary>
    public class MarketDTO
    {
        /// <summary>
        /// Gets or sets the market ticker symbol.
        /// </summary>
        public string? market_ticker { get; set; }

        /// <summary>
        /// Gets or sets the event ticker symbol.
        /// </summary>
        public string? event_ticker { get; set; }

        /// <summary>
        /// Gets or sets the market type.
        /// </summary>
        public string? market_type { get; set; }

        /// <summary>
        /// Gets or sets the market title.
        /// </summary>
        public string? title { get; set; }

        /// <summary>
        /// Gets or sets the market subtitle.
        /// </summary>
        public string? subtitle { get; set; }

        /// <summary>
        /// Gets or sets the yes sub-title.
        /// </summary>
        public string? yes_sub_title { get; set; }

        /// <summary>
        /// Gets or sets the no sub-title.
        /// </summary>
        public string? no_sub_title { get; set; }

        /// <summary>
        /// Gets or sets the market open time.
        /// </summary>
        public DateTime open_time { get; set; }

        /// <summary>
        /// Gets or sets the market close time.
        /// </summary>
        public DateTime close_time { get; set; }

        /// <summary>
        /// Gets or sets the expected expiration time.
        /// </summary>
        public DateTime? expected_expiration_time { get; set; }

        /// <summary>
        /// Gets or sets the expiration time.
        /// </summary>
        public DateTime expiration_time { get; set; }

        /// <summary>
        /// Gets or sets the latest expiration time.
        /// </summary>
        public DateTime latest_expiration_time { get; set; }

        /// <summary>
        /// Gets or sets the settlement timer in seconds.
        /// </summary>
        public int settlement_timer_seconds { get; set; }

        /// <summary>
        /// Gets or sets the market status.
        /// </summary>
        public string? status { get; set; }

        /// <summary>
        /// Gets or sets the response price units.
        /// </summary>
        public string? response_price_units { get; set; }

        /// <summary>
        /// Gets or sets the notional value.
        /// </summary>
        public int notional_value { get; set; }

        /// <summary>
        /// Gets or sets the tick size.
        /// </summary>
        public int tick_size { get; set; }

        /// <summary>
        /// Gets or sets the yes bid price.
        /// </summary>
        public int yes_bid { get; set; }

        /// <summary>
        /// Gets or sets the yes ask price.
        /// </summary>
        public int yes_ask { get; set; }

        /// <summary>
        /// Gets or sets the no bid price.
        /// </summary>
        public int no_bid { get; set; }

        /// <summary>
        /// Gets or sets the no ask price.
        /// </summary>
        public int no_ask { get; set; }

        /// <summary>
        /// Gets or sets the last price.
        /// </summary>
        public int last_price { get; set; }

        /// <summary>
        /// Gets or sets the previous yes bid price.
        /// </summary>
        public int previous_yes_bid { get; set; }

        /// <summary>
        /// Gets or sets the previous yes ask price.
        /// </summary>
        public int previous_yes_ask { get; set; }

        /// <summary>
        /// Gets or sets the previous price.
        /// </summary>
        public int previous_price { get; set; }

        /// <summary>
        /// Gets or sets the trading volume.
        /// </summary>
        public long volume { get; set; }

        /// <summary>
        /// Gets or sets the 24-hour trading volume.
        /// </summary>
        public int volume_24h { get; set; }

        /// <summary>
        /// Gets or sets the market liquidity.
        /// </summary>
        public long liquidity { get; set; }

        /// <summary>
        /// Gets or sets the open interest.
        /// </summary>
        public int open_interest { get; set; }

        /// <summary>
        /// Gets or sets the market result.
        /// </summary>
        public string? result { get; set; }

        /// <summary>
        /// Gets or sets whether the market can close early.
        /// </summary>
        public bool can_close_early { get; set; }

        /// <summary>
        /// Gets or sets the expiration value.
        /// </summary>
        public string? expiration_value { get; set; }

        /// <summary>
        /// Gets or sets the market category.
        /// </summary>
        public string? category { get; set; }

        /// <summary>
        /// Gets or sets the risk limit in cents.
        /// </summary>
        public int risk_limit_cents { get; set; }

        /// <summary>
        /// Gets or sets the strike type.
        /// </summary>
        public string? strike_type { get; set; }

        /// <summary>
        /// Gets or sets the floor strike price.
        /// </summary>
        public double? floor_strike { get; set; }

        /// <summary>
        /// Gets or sets the primary rules.
        /// </summary>
        public string? rules_primary { get; set; }

        /// <summary>
        /// Gets or sets the secondary rules.
        /// </summary>
        public string? rules_secondary { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the last candlestick timestamp in UTC.
        /// </summary>
        public DateTime? LastCandlestickUTC { get; set; }

        /// <summary>
        /// Gets or sets the last API fetch date.
        /// </summary>
        public DateTime? APILastFetchedDate { get; set; }

        /// <summary>
        /// Gets or sets the list of orderbook data.
        /// </summary>
        public List<OrderbookData>? Orderbooks { get; set; }

        /// <summary>
        /// Gets or sets the associated event data.
        /// </summary>
        public EventDTO? Event { get; set; }
    }

}
