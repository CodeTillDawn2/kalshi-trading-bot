using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OverseerBotShared
{
    /// <summary>
    /// Data structure containing comprehensive information about a brain instance's current state.
    /// Used for periodic check-ins to report status, performance metrics, and configuration to the Overseer.
    /// </summary>
    public class CheckInData
    {
        // Basic brain info
        [JsonPropertyName("brainInstanceName")]
        public string? BrainInstanceName { get; set; }

        // Basic market data
        [JsonPropertyName("markets")]
        public List<string>? Markets { get; set; }
        public long ErrorCount { get; set; }
        public DateTime? LastSnapshot { get; set; }
        public bool IsStartingUp { get; set; }
        public bool IsShuttingDown { get; set; }

        // Brain configuration
        public bool WatchPositions { get; set; }
        public bool WatchOrders { get; set; }
        public bool ManagedWatchList { get; set; }
        public bool CaptureSnapshots { get; set; }
        public int TargetWatches { get; set; }
        public double MinimumInterest { get; set; }
        public double UsageMin { get; set; }
        public double UsageMax { get; set; }

        // Performance metrics
        public double CurrentCpuUsage { get; set; }
        public double EventQueueAvg { get; set; }
        public double TickerQueueAvg { get; set; }
        public double NotificationQueueAvg { get; set; }
        public double OrderbookQueueAvg { get; set; }
        public double LastRefreshCycleSeconds { get; set; }
        public double LastRefreshCycleInterval { get; set; }
        public double LastRefreshMarketCount { get; set; }
        public double LastRefreshUsagePercentage { get; set; }
        public bool LastRefreshTimeAcceptable { get; set; }
        public DateTime? LastPerformanceSampleDate { get; set; }

        // Connection status
        public bool IsWebSocketConnected { get; set; }

        // Market watch data
        [JsonPropertyName("watchedMarkets")]
        public List<MarketWatchData>? WatchedMarkets { get; set; }
    }

    /// <summary>
    /// Response structure for check-in acknowledgments from the Overseer.
    /// Contains success status, optional message, and target tickers for the brain to monitor.
    /// </summary>
    public class CheckInResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string[] TargetTickers { get; set; } = Array.Empty<string>();
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Response structure for target tickers confirmation acknowledgments.
    /// Used to confirm that the brain has successfully received and processed target market assignments.
    /// </summary>
    public class TargetTickersConfirmationResponse
    {
        public bool Success { get; set; }
        public string BrainInstanceName { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Data structure containing comprehensive performance metrics from the CentralPerformanceMonitor.
    /// Used for detailed performance monitoring and analytics, including database operations,
    /// WebSocket metrics, queue depths, and system resource utilization.
    /// </summary>
    public class PerformanceMetricsData
    {
        /// <summary>
        /// Gets or sets the name of the brain instance providing the performance metrics.
        /// </summary>
        [JsonPropertyName("brainInstanceName")]
        public string? BrainInstanceName { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when these performance metrics were collected.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the database performance metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)>? DatabaseMetrics { get; set; }

        /// <summary>
        /// Gets or sets the OverseerClientService performance metrics.
        /// </summary>
        public IReadOnlyDictionary<string, object>? OverseerClientServiceMetrics { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket processing time metrics in ticks.
        /// </summary>
        public ConcurrentDictionary<string, long>? WebSocketProcessingTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket processing count metrics.
        /// </summary>
        public ConcurrentDictionary<string, int>? WebSocketProcessingCount { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket buffer usage metrics in bytes.
        /// </summary>
        public ConcurrentDictionary<string, long>? WebSocketBufferUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket operation times.
        /// </summary>
        public ConcurrentDictionary<string, TimeSpan>? WebSocketOperationTimes { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket semaphore wait counts.
        /// </summary>
        public ConcurrentDictionary<string, int>? WebSocketSemaphoreWaitCount { get; set; }

        /// <summary>
        /// Gets or sets the SubscriptionManager operation metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)>? SubscriptionManagerOperationMetrics { get; set; }

        /// <summary>
        /// Gets or sets the SubscriptionManager lock contention metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)>? SubscriptionManagerLockMetrics { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor total messages processed.
        /// </summary>
        public long MessageProcessorTotalMessagesProcessed { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor total processing time in milliseconds.
        /// </summary>
        public long MessageProcessorTotalProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor average processing time in milliseconds.
        /// </summary>
        public double MessageProcessorAverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor messages per second rate.
        /// </summary>
        public double MessageProcessorMessagesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor order book queue depth.
        /// </summary>
        public int MessageProcessorOrderBookQueueDepth { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor duplicate message count.
        /// </summary>
        public int MessageProcessorDuplicateMessageCount { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor duplicates in window.
        /// </summary>
        public int MessageProcessorDuplicatesInWindow { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor last duplicate warning time.
        /// </summary>
        public DateTime MessageProcessorLastDuplicateWarningTime { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor message type counts.
        /// </summary>
        public IReadOnlyDictionary<string, long>? MessageProcessorMessageTypeCounts { get; set; }

        /// <summary>
        /// Gets or sets the API execution times.
        /// </summary>
        public ConcurrentDictionary<string, List<(DateTime Timestamp, long Milliseconds)>>? ApiExecutionTimes { get; set; }

        /// <summary>
        /// Gets or sets the configurable metrics for GUI consumption.
        /// </summary>
        public Dictionary<string, object> ConfigurableMetrics { get; set; } = new();
    }

    /// <summary>
    /// Data transfer object representing the current status and configuration of a brain instance.
    /// Used for communication between the brain and overseer systems, containing real-time
    /// operational data, performance metrics, and market watch information.
    /// </summary>
    public class BrainStatusData
    {
        /// <summary>
        /// Gets or sets the unique name identifier for this brain instance.
        /// Used to correlate status data with the specific brain in the overseer system.
        /// </summary>
        [JsonPropertyName("brainInstanceName")]
        public string? BrainInstanceName { get; set; }

        /// <summary>
        /// Gets or sets the list of market tickers currently being monitored by this brain.
        /// Represents the active market watch list at the time of status reporting.
        /// </summary>
        [JsonPropertyName("markets")]
        public List<string>? Markets { get; set; }

        /// <summary>
        /// Gets or sets the total count of errors encountered since the last status report.
        /// Used by the overseer to monitor system health and trigger recovery procedures.
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the most recent snapshot taken by this brain.
        /// Indicates the freshness of market data and analysis capabilities.
        /// </summary>
        public DateTime? LastSnapshot { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last check-in with the overseer system.
        /// Used to determine if the brain is still active and communicating properly.
        /// </summary>
        public DateTime? LastCheckIn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this brain is currently in the startup phase.
        /// Helps the overseer understand the brain's operational state and readiness.
        /// </summary>
        public bool IsStartingUp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this brain is currently shutting down.
        /// Allows the overseer to coordinate graceful shutdown procedures.
        /// </summary>
        public bool IsShuttingDown { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this brain should monitor position changes.
        /// Determines if position tracking features are enabled for this instance.
        /// </summary>
        public bool WatchPositions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this brain should monitor order changes.
        /// Determines if order tracking and analysis features are enabled.
        /// </summary>
        public bool WatchOrders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the market watch list is managed automatically.
        /// When true, the brain will dynamically adjust its market coverage based on performance.
        /// </summary>
        public bool ManagedWatchList { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether market snapshots should be captured.
        /// Controls whether the brain performs periodic market state captures for analysis.
        /// </summary>
        public bool CaptureSnapshots { get; set; }

        /// <summary>
        /// Gets or sets the target number of markets this brain should actively monitor.
        /// Used by the overseer to optimize resource allocation and market coverage.
        /// </summary>
        [Range(0, int.MaxValue)]
        [JsonPropertyName("targetWatches")]
        public int TargetWatches { get; set; }

        /// <summary>
        /// Gets or sets the minimum interest score required for market inclusion.
        /// Markets below this threshold will not be considered for watching.
        /// </summary>
        [Range(0.0, double.MaxValue)]
        [JsonPropertyName("minimumInterest")]
        public double MinimumInterest { get; set; }

        /// <summary>
        /// Gets or sets the minimum CPU usage threshold for performance monitoring.
        /// Used to establish baseline resource consumption expectations.
        /// </summary>
        [Range(0.0, 100.0)]
        [JsonPropertyName("usageMin")]
        public double UsageMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum CPU usage threshold for performance monitoring.
        /// Used to detect resource exhaustion and trigger optimization measures.
        /// </summary>
        [Range(0.0, 100.0)]
        [JsonPropertyName("usageMax")]
        public double UsageMax { get; set; }

        /// <summary>
        /// Gets or sets the current CPU usage percentage of this brain instance.
        /// Real-time performance metric used for resource monitoring and scaling decisions.
        /// </summary>
        public double CurrentCpuUsage { get; set; }

        /// <summary>
        /// Gets or sets the average depth of the event processing queue.
        /// Indicates system load and potential processing bottlenecks.
        /// </summary>
        public double EventQueueAvg { get; set; }

        /// <summary>
        /// Gets or sets the average depth of the ticker data processing queue.
        /// Monitors the efficiency of market data ingestion and processing.
        /// </summary>
        public double TickerQueueAvg { get; set; }

        /// <summary>
        /// Gets or sets the average depth of the notification processing queue.
        /// Tracks the volume of alerts and notifications being generated.
        /// </summary>
        public double NotificationQueueAvg { get; set; }

        /// <summary>
        /// Gets or sets the average depth of the order book update queue.
        /// Critical metric for monitoring order book processing performance.
        /// </summary>
        public double OrderbookQueueAvg { get; set; }

        /// <summary>
        /// Gets or sets the duration in seconds of the last market data refresh cycle.
        /// Used to monitor refresh performance and identify optimization opportunities.
        /// </summary>
        public double LastRefreshCycleSeconds { get; set; }

        /// <summary>
        /// Gets or sets the time interval between the last two refresh cycles.
        /// Ensures consistent and timely market data updates.
        /// </summary>
        public double LastRefreshCycleInterval { get; set; }

        /// <summary>
        /// Gets or sets the number of markets processed in the last refresh cycle.
        /// Indicates the scope and efficiency of market data updates.
        /// </summary>
        public double LastRefreshMarketCount { get; set; }

        /// <summary>
        /// Gets or sets the CPU usage percentage during the last refresh cycle.
        /// Correlates resource consumption with system performance.
        /// </summary>
        public double LastRefreshUsagePercentage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the last refresh cycle completed within acceptable time limits.
        /// Used to trigger performance optimization measures when refresh times degrade.
        /// </summary>
        public bool LastRefreshTimeAcceptable { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last performance metrics sample.
        /// Provides temporal context for performance data analysis.
        /// </summary>
        public DateTime? LastPerformanceSampleDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the WebSocket connection is currently active.
        /// Critical for real-time data streaming and immediate system health assessment.
        /// </summary>
        public bool IsWebSocketConnected { get; set; }

        /// <summary>
        /// Gets or sets the list of markets currently being watched with detailed watch data.
        /// Provides comprehensive information about each watched market's status and metrics.
        /// </summary>
        [JsonPropertyName("watchedMarkets")]
        public List<MarketWatchData>? WatchedMarkets { get; set; }

        /// <summary>
        /// Gets or sets the latest comprehensive performance metrics received from the brain.
        /// This object contains detailed performance data that is stored as-is for monitoring purposes.
        /// </summary>
        public object? LatestPerformanceMetrics { get; set; }
    }

    /// <summary>
    /// Contains detailed information about a market being watched by a brain instance.
    /// Includes interest scoring data, watch history, and performance metrics for the specific market.
    /// </summary>
    public class MarketWatchData
    {
        /// <summary>
        /// Gets or sets the unique ticker symbol identifying the market.
        /// Serves as the primary identifier for market operations and data retrieval.
        /// </summary>
        [Required]
        [JsonPropertyName("marketTicker")]
        public string MarketTicker { get; set; } = "";

        /// <summary>
        /// Gets or sets the GUID of the brain instance watching this market.
        /// Used to correlate market watch data with specific brain instances.
        /// </summary>
        public Guid? Brain { get; set; }

        /// <summary>
        /// Gets or sets the calculated interest score for this market.
        /// Higher scores indicate greater trading interest and potential for inclusion in watch lists.
        /// </summary>
        public double? InterestScore { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the interest score was last calculated.
        /// Used to determine if the score needs to be recalculated based on data freshness.
        /// </summary>
        public DateTime? InterestScoreDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this market was last actively watched.
        /// Used to track market engagement and determine watch list optimization.
        /// </summary>
        public DateTime? LastWatched { get; set; }

        /// <summary>
        /// Gets or sets the average number of WebSocket events received per minute for this market.
        /// Indicates market activity level and data stream density for performance monitoring.
        /// </summary>
        public double? AverageWebsocketEventsPerMinute { get; set; }
    }

    /// <summary>
    /// Request structure for handshake operations.
    /// Contains client identification and authentication information.
    /// </summary>
    public class HandshakeRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier for the client.
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Gets or sets the name of the client (typically the brain instance name).
        /// </summary>
        public string? ClientName { get; set; }

        /// <summary>
        /// Gets or sets the type of client (e.g., brain, dashboard).
        /// </summary>
        public string? ClientType { get; set; }

        /// <summary>
        /// Gets or sets the authentication token provided by the client.
        /// </summary>
        public string? AuthToken { get; set; }
    }

    /// <summary>
    /// Response structure for handshake operations.
    /// Contains authentication token and success status for client registration.
    /// </summary>
    public class HandshakeResponse
    {
        public bool Success { get; set; }
        public string AuthToken { get; set; } = "";
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Response structure for performance metrics operations.
    /// Contains success status and processing confirmation.
    /// </summary>
    public class PerformanceMetricsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Response structure for general message operations.
    /// Contains success status and message processing confirmation.
    /// </summary>
    public class MessageResponse
    {
        public bool Success { get; set; }
        public string MessageType { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = "";
    }
}
