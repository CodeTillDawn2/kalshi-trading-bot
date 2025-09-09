using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a market on the Kalshi platform with details such as ticker, prices, and trading status.
    /// </summary>
    public class KalshiMarket
    {
        [JsonPropertyName("ticker")] public string Ticker { get; set; } = string.Empty;
        [JsonPropertyName("event_ticker")] public string EventTicker { get; set; } = string.Empty;
        [JsonPropertyName("market_type")] public string MarketType { get; set; } = string.Empty;
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("subtitle")] public string Subtitle { get; set; } = string.Empty;
        [JsonPropertyName("yes_sub_title")] public string YesSubTitle { get; set; } = string.Empty;
        [JsonPropertyName("no_sub_title")] public string NoSubTitle { get; set; } = string.Empty;
        [JsonPropertyName("open_time")] public DateTime OpenTime { get; set; }
        [JsonPropertyName("close_time")] public DateTime CloseTime { get; set; }
        [JsonPropertyName("expected_expiration_time")] public DateTime ExpectedExpirationTime { get; set; }
        [JsonPropertyName("expiration_time")] public DateTime ExpirationTime { get; set; }
        [JsonPropertyName("latest_expiration_time")] public DateTime LatestExpirationTime { get; set; }
        [JsonPropertyName("settlement_timer_seconds")] public int SettlementTimerSeconds { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
        [JsonPropertyName("response_price_units")] public string ResponsePriceUnits { get; set; } = string.Empty;
        [JsonPropertyName("notional_value")] public int NotionalValue { get; set; }
        [JsonPropertyName("tick_size")] public int TickSize { get; set; }
        [JsonPropertyName("yes_bid")] public int YesBid { get; set; }
        [JsonPropertyName("yes_ask")] public int YesAsk { get; set; }
        [JsonPropertyName("no_bid")] public int NoBid { get; set; }
        [JsonPropertyName("no_ask")] public int NoAsk { get; set; }
        [JsonPropertyName("last_price")] public int LastPrice { get; set; }
        [JsonPropertyName("previous_yes_bid")] public int PreviousYesBid { get; set; }
        [JsonPropertyName("previous_yes_ask")] public int PreviousYesAsk { get; set; }
        [JsonPropertyName("previous_price")] public int PreviousPrice { get; set; }
        [JsonPropertyName("volume")] public long Volume { get; set; }
        [JsonPropertyName("volume_24h")] public int Volume24h { get; set; }
        [JsonPropertyName("liquidity")] public long Liquidity { get; set; }
        [JsonPropertyName("open_interest")] public int OpenInterest { get; set; }
        [JsonPropertyName("result")] public string Result { get; set; } = string.Empty;
        [JsonPropertyName("can_close_early")] public bool CanCloseEarly { get; set; }
        [JsonPropertyName("expiration_value")] public string ExpirationValue { get; set; } = string.Empty;
        [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
        [JsonPropertyName("risk_limit_cents")] public int RiskLimitCents { get; set; }
        [JsonPropertyName("strike_type")] public string StrikeType { get; set; } = string.Empty;
        [JsonPropertyName("floor_strike")] public double? FloorStrike { get; set; }
        [JsonPropertyName("custom_strike")] public Dictionary<string, string>? CustomStrike { get; set; }
        [JsonPropertyName("rules_primary")] public string RulesPrimary { get; set; } = string.Empty;
        [JsonPropertyName("rules_secondary")] public string? RulesSecondary { get; set; } = string.Empty;
    }
}
