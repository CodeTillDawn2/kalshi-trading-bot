namespace KalshiBotAPI.Configuration;

/// <summary>
/// Configuration settings for the Kalshi API integration.
/// </summary>
public class KalshiConfig
{
    /// <summary>
    /// Gets or sets the environment (e.g., "prod" or "dev"). Defaults to "prod".
    /// </summary>
    public string Environment { get; set; } = "QA";

    /// <summary>
    /// Gets or sets the API key ID.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the API key file.
    /// </summary>
    public string KeyFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the WebSocket buffer size in bytes. Defaults to 16384 (16KB).
    /// </summary>
    public int WebSocketBufferSize { get; set; } = 16384;

    /// <summary>
    /// Gets or sets the maximum number of WebSocket connection retry attempts. Defaults to 5.
    /// </summary>
    public int WebSocketMaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the array of retry delay intervals in milliseconds for WebSocket connections.
    /// Defaults to [1000, 2000, 4000, 8000, 16000].
    /// </summary>
    public int[] WebSocketRetryDelays { get; set; } = { 1000, 2000, 4000, 8000, 16000 };

    /// <summary>
    /// Gets or sets the duration in minutes for caching WebSocket authentication signatures.
    /// Defaults to 5 minutes.
    /// </summary>
    public int WebSocketSignatureCacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the configuration for candlestick lookback periods in days.
    /// Controls how far back in time to fetch candlestick data for different intervals.
    /// </summary>
    public CandlestickLookbackConfig CandlestickLookback { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration for candlestick cushion seconds.
    /// Defines the overlap period to avoid gaps when fetching historical data in chunks.
    /// </summary>
    public CandlestickCushionConfig CandlestickCushion { get; set; } = new();
}

/// <summary>
/// Configuration for candlestick lookback periods in days.
/// Defines the maximum historical data range to fetch for each time interval.
/// </summary>
public class CandlestickLookbackConfig
{
    /// <summary>
    /// Gets or sets the lookback period in days for minute-interval candlesticks.
    /// </summary>
    public int Minute { get; set; } = 3;

    /// <summary>
    /// Gets or sets the lookback period in days for hour-interval candlesticks.
    /// </summary>
    public int Hour { get; set; } = 7;

    /// <summary>
    /// Gets or sets the lookback period in days for day-interval candlesticks.
    /// </summary>
    public int Day { get; set; } = 15;
}

/// <summary>
/// Configuration for candlestick cushion seconds.
/// Defines the overlap period in seconds to prevent data gaps when fetching historical data in chunks.
/// </summary>
public class CandlestickCushionConfig
{
    /// <summary>
    /// Gets or sets the cushion seconds for minute-interval candlesticks.
    /// </summary>
    public int Minute { get; set; } = 60;

    /// <summary>
    /// Gets or sets the cushion seconds for hour-interval candlesticks.
    /// </summary>
    public int Hour { get; set; } = 3600;

    /// <summary>
    /// Gets or sets the cushion seconds for day-interval candlesticks.
    /// </summary>
    public int Day { get; set; } = 86400;
}
