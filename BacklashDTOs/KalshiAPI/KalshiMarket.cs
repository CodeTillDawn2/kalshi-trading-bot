using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a market on the Kalshi platform with details such as ticker, prices, and trading status.
    /// </summary>
    public class KalshiMarket
    {
        /// <summary>
        /// Gets or sets the ticker symbol.
        /// </summary>
        [JsonPropertyName("ticker")] public string Ticker { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the event ticker.
        /// </summary>
        [JsonPropertyName("event_ticker")] public string EventTicker { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the market type.
        /// </summary>
        [JsonPropertyName("market_type")] public string MarketType { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the subtitle.
        /// </summary>
        [JsonPropertyName("subtitle")] public string Subtitle { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the yes subtitle.
        /// </summary>
        [JsonPropertyName("yes_sub_title")] public string YesSubTitle { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the no subtitle.
        /// </summary>
        [JsonPropertyName("no_sub_title")] public string NoSubTitle { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the open time.
        /// </summary>
        [JsonPropertyName("open_time")] public DateTime OpenTime { get; set; }
        /// <summary>
        /// Gets or sets the close time.
        /// </summary>
        [JsonPropertyName("close_time")] public DateTime CloseTime { get; set; }
        /// <summary>
        /// Gets or sets the expected expiration time.
        /// </summary>
        [JsonPropertyName("expected_expiration_time")] public DateTime ExpectedExpirationTime { get; set; }
        /// <summary>
        /// Gets or sets the expiration time.
        /// </summary>
        [JsonPropertyName("expiration_time")] public DateTime ExpirationTime { get; set; }
        /// <summary>
        /// Gets or sets the latest expiration time.
        /// </summary>
        [JsonPropertyName("latest_expiration_time")] public DateTime LatestExpirationTime { get; set; }
        /// <summary>
        /// Gets or sets the settlement timer seconds.
        /// </summary>
        [JsonPropertyName("settlement_timer_seconds")] public int SettlementTimerSeconds { get; set; }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the response price units.
        /// </summary>
        [JsonPropertyName("response_price_units")] public string ResponsePriceUnits { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the notional value.
        /// </summary>
        [JsonPropertyName("notional_value")] public int NotionalValue { get; set; }
        /// <summary>
        /// Gets or sets the tick size.
        /// </summary>
        [JsonPropertyName("tick_size")] public int TickSize { get; set; }
        /// <summary>
        /// Gets or sets the yes bid.
        /// </summary>
        [JsonPropertyName("yes_bid")] public int YesBid { get; set; }
        /// <summary>
        /// Gets or sets the yes ask.
        /// </summary>
        [JsonPropertyName("yes_ask")] public int YesAsk { get; set; }
        /// <summary>
        /// Gets or sets the no bid.
        /// </summary>
        [JsonPropertyName("no_bid")] public int NoBid { get; set; }
        /// <summary>
        /// Gets or sets the no ask.
        /// </summary>
        [JsonPropertyName("no_ask")] public int NoAsk { get; set; }
        /// <summary>
        /// Gets or sets the last price.
        /// </summary>
        [JsonPropertyName("last_price")] public int LastPrice { get; set; }
        /// <summary>
        /// Gets or sets the previous yes bid.
        /// </summary>
        [JsonPropertyName("previous_yes_bid")] public int PreviousYesBid { get; set; }
        /// <summary>
        /// Gets or sets the previous yes ask.
        /// </summary>
        [JsonPropertyName("previous_yes_ask")] public int PreviousYesAsk { get; set; }
        /// <summary>
        /// Gets or sets the previous price.
        /// </summary>
        [JsonPropertyName("previous_price")] public int PreviousPrice { get; set; }
        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        [JsonPropertyName("volume")] public long Volume { get; set; }
        /// <summary>
        /// Gets or sets the 24-hour volume.
        /// </summary>
        [JsonPropertyName("volume_24h")] public int Volume24h { get; set; }
        /// <summary>
        /// Gets or sets the liquidity.
        /// </summary>
        [JsonPropertyName("liquidity")] public long Liquidity { get; set; }
        /// <summary>
        /// Gets or sets the open interest.
        /// </summary>
        [JsonPropertyName("open_interest")] public int OpenInterest { get; set; }
        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        [JsonPropertyName("result")] public string Result { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether the market can close early.
        /// </summary>
        [JsonPropertyName("can_close_early")] public bool CanCloseEarly { get; set; }
        /// <summary>
        /// Gets or sets the expiration value.
        /// </summary>
        [JsonPropertyName("expiration_value")] public string ExpirationValue { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the risk limit cents.
        /// </summary>
        [JsonPropertyName("risk_limit_cents")] public int RiskLimitCents { get; set; }
        /// <summary>
        /// Gets or sets the strike type.
        /// </summary>
        [JsonPropertyName("strike_type")] public string StrikeType { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the floor strike.
        /// </summary>
        [JsonPropertyName("floor_strike")] public double? FloorStrike { get; set; }
        /// <summary>
        /// Gets or sets the custom strike.
        /// </summary>
        [JsonPropertyName("custom_strike")] public Dictionary<string, string>? CustomStrike { get; set; }
        /// <summary>
        /// Gets or sets the primary rules.
        /// </summary>
        [JsonPropertyName("rules_primary")] public string RulesPrimary { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the secondary rules.
        /// </summary>
        [JsonPropertyName("rules_secondary")] public string? RulesSecondary { get; set; } = string.Empty;
    }
}
