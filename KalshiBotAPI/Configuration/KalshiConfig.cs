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
    /// Gets or sets the delay in milliseconds before resetting WebSocket connection.
    /// Defaults to 5000 (5 seconds).
    /// </summary>
    public int WebSocketResetDelayMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for acquiring the connection semaphore.
    /// Defaults to 60000 (60 seconds).
    /// </summary>
    public int WebSocketSemaphoreTimeoutMs { get; set; } = 60000;

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

    /// <summary>
    /// Gets or sets the configuration for message processing settings.
    /// Controls message batching, queue sizes, timeouts, and performance metrics.
    /// </summary>
    public MessageProcessorConfig MessageProcessor { get; set; } = new();
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

/// <summary>
/// Configuration settings for the MessageProcessor component.
/// Controls message batching, queue sizes, timeouts, and performance monitoring.
/// </summary>
public class MessageProcessorConfig
{
    /// <summary>
    /// Gets or sets whether message batching is enabled for high-volume scenarios.
    /// When enabled, messages are batched before processing to reduce event overhead.
    /// </summary>
    public bool EnableMessageBatching { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum batch size for message processing.
    /// Messages are processed in batches of this size when batching is enabled.
    /// </summary>
    public int MaxBatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the batch processing interval in milliseconds.
    /// How often batched messages are processed when batching is enabled.
    /// </summary>
    public int BatchProcessingIntervalMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum size of the order book update queue.
    /// When the queue reaches this size, new messages may be dropped or batched differently.
    /// </summary>
    public int MaxOrderBookQueueSize { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for waiting for empty order book queue.
    /// </summary>
    public int OrderBookQueueTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the maximum number of processed sequence numbers to keep in memory.
    /// Used for message deduplication to prevent memory leaks.
    /// </summary>
    public int MaxSequenceNumbersToKeep { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the interval in milliseconds for periodic cleanup of old sequence numbers.
    /// </summary>
    public int SequenceNumberCleanupIntervalMs { get; set; } = 60000;

    /// <summary>
    /// Gets or sets whether performance metrics collection is enabled.
    /// When enabled, collects metrics on message processing rates and queue depths.
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval in milliseconds for logging performance metrics.
    /// </summary>
    public int PerformanceMetricsLogIntervalMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the threshold for duplicate message warnings.
    /// If duplicate messages exceed this count within a time window, warnings are logged.
    /// </summary>
    public int DuplicateMessageWarningThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the time window in milliseconds for duplicate message counting.
    /// </summary>
    public int DuplicateMessageTimeWindowMs { get; set; } = 60000;

    /// <summary>
    /// Gets or sets whether to use sophisticated locking mechanisms for high-concurrency scenarios.
    /// When enabled, uses ReaderWriterLockSlim instead of simple lock for order book queue.
    /// </summary>
    public bool UseAdvancedLocking { get; set; } = true;
}
