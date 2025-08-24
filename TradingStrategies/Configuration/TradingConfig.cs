namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration settings for trading
/// </summary>
public class TradingConfig
{
    public int DecisionFrequencySeconds { get; set; }
    public double ChangeWindowDurationMinutes { get; set; }
    public double TradeMatchingWindowSeconds { get; set; }
    public double OrderbookCancelWindowSeconds { get; set; }
    public TimeSpan ChangeWindowDuration => TimeSpan.FromMinutes(ChangeWindowDurationMinutes);
    public TimeSpan TradeMatchingWindow => TimeSpan.FromSeconds(TradeMatchingWindowSeconds);
    public TimeSpan OrderbookCancelWindow => TimeSpan.FromSeconds(OrderbookCancelWindowSeconds);
    public int RefreshIntervalMinutes { get; set; }

}