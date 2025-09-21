using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BacklashOverseer.Models
{
    /// <summary>
    /// Configuration class for default values used in BrainPersistence initialization.
    /// Provides centralized configuration options that can be loaded from appsettings.json.
    /// </summary>
    public static class BrainPersistenceDefaults
    {
        private static string _defaultMode = "Autonomous";
        private static int _defaultTargetWatches = 10;
        private static double _defaultMinimumInterest = 0.5;
        private static double _defaultUsageMin = 10.0;
        private static double _defaultUsageMax = 80.0;

        /// <summary>
        /// Gets the default operational mode for brain instances.
        /// </summary>
        public static string DefaultMode => _defaultMode;

        /// <summary>
        /// Gets the default target number of markets to watch.
        /// </summary>
        public static int DefaultTargetWatches => _defaultTargetWatches;

        /// <summary>
        /// Gets the default minimum interest score for market watching.
        /// </summary>
        public static double DefaultMinimumInterest => _defaultMinimumInterest;

        /// <summary>
        /// Gets the default minimum CPU usage threshold.
        /// </summary>
        public static double DefaultUsageMin => _defaultUsageMin;

        /// <summary>
        /// Gets the default maximum CPU usage threshold.
        /// </summary>
        public static double DefaultUsageMax => _defaultUsageMax;

        /// <summary>
        /// Initializes the default values from configuration.
        /// Call this method during application startup to load values from appsettings.json.
        /// </summary>
        /// <param name="defaultMode">The default mode from configuration.</param>
        /// <param name="defaultTargetWatches">The default target watches from configuration.</param>
        /// <param name="defaultMinimumInterest">The default minimum interest from configuration.</param>
        /// <param name="defaultUsageMin">The default minimum usage from configuration.</param>
        /// <param name="defaultUsageMax">The default maximum usage from configuration.</param>
        public static void InitializeFromConfig(
            string defaultMode,
            int defaultTargetWatches,
            double defaultMinimumInterest,
            double defaultUsageMin,
            double defaultUsageMax)
        {
            _defaultMode = defaultMode;
            _defaultTargetWatches = defaultTargetWatches;
            _defaultMinimumInterest = defaultMinimumInterest;
            _defaultUsageMin = defaultUsageMin;
            _defaultUsageMax = defaultUsageMax;
        }
    }
    /// <summary>
    /// Represents the persistent state of a brain instance in the Kalshi trading bot overseer system.
    /// This class encapsulates all the configuration, status, and historical performance data needed
    /// to maintain and restore a brain's operational state across application restarts.
    /// </summary>
    public class BrainPersistence
    {
        /// <summary>
        /// Gets or sets the unique name identifier for this brain instance.
        /// Used as the primary key for brain management and persistence operations.
        /// </summary>
        [Required]
        [JsonPropertyName("brainInstanceName")]
        public required string BrainInstanceName { get; set; }

        /// <summary>
        /// Gets or sets the unique GUID identifier for the brain.
        /// May be null if the brain hasn't been assigned an ID yet.
        /// </summary>
        public Guid? Brain { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this brain should monitor position changes.
        /// When enabled, the brain will track and respond to position updates in watched markets.
        /// </summary>
        public bool WatchPositions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this brain should monitor order changes.
        /// When enabled, the brain will track order placements, fills, and cancellations.
        /// </summary>
        public bool WatchOrders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the market watch list should be managed automatically.
        /// When enabled, the brain will dynamically add/remove markets based on interest scores and performance.
        /// </summary>
        public bool ManagedWatchList { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether market snapshots should be captured.
        /// Snapshots provide point-in-time views of market state for analysis and backtesting.
        /// </summary>
        public bool CaptureSnapshots { get; set; }

        /// <summary>
        /// Gets or sets the target number of markets this brain should actively watch.
        /// Used by the market management system to determine optimal watch list size.
        /// </summary>
        [Range(0, int.MaxValue)]
        [JsonPropertyName("targetWatches")]
        public int TargetWatches { get; set; } = BrainPersistenceDefaults.DefaultTargetWatches;

        /// <summary>
        /// Gets or sets the minimum interest score required for a market to be considered for watching.
        /// Markets below this threshold will not be added to the watch list.
        /// </summary>
        [Range(0.0, double.MaxValue)]
        [JsonPropertyName("minimumInterest")]
        public double MinimumInterest { get; set; } = BrainPersistenceDefaults.DefaultMinimumInterest;

        /// <summary>
        /// Gets or sets the minimum CPU usage threshold for this brain instance.
        /// Used for performance monitoring and resource allocation decisions.
        /// </summary>
        [Range(0.0, 100.0)]
        [JsonPropertyName("usageMin")]
        public double UsageMin { get; set; } = BrainPersistenceDefaults.DefaultUsageMin;

        /// <summary>
        /// Gets or sets the maximum CPU usage threshold for this brain instance.
        /// Used for performance monitoring and resource allocation decisions.
        /// </summary>
        [Range(0.0, 100.0)]
        [JsonPropertyName("usageMax")]
        public double UsageMax { get; set; } = BrainPersistenceDefaults.DefaultUsageMax;

        /// <summary>
        /// Gets or sets the timestamp of the last time this brain instance was seen or checked in.
        /// Used to determine if the brain is still active and responsive.
        /// </summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// Gets or sets the list of market tickers currently being watched by this brain.
        /// Represents the active market watch list at any given time.
        /// </summary>
        [Required]
        [JsonPropertyName("currentMarketTickers")]
        public List<string> CurrentMarketTickers { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of market tickers targeted for watching by this brain.
        /// Represents the desired market watch list, which may differ from current during transitions.
        /// </summary>
        [Required]
        [JsonPropertyName("targetMarketTickers")]
        public List<string> TargetMarketTickers { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the operational mode of this brain instance.
        /// Defaults to "Autonomous" but can be set to other modes like "Manual" or "Simulation".
        /// </summary>
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = BrainPersistenceDefaults.DefaultMode;

        /// <summary>
        /// Gets or sets a value indicating whether this brain instance is currently starting up.
        /// Used to track initialization state and prevent premature operations.
        /// </summary>
        public bool IsStartingUp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this brain instance is currently shutting down.
        /// Used to coordinate graceful shutdown and prevent new operations during teardown.
        /// </summary>
        public bool IsShuttingDown { get; set; }

        /// <summary>
        /// Gets or sets the total count of errors encountered by this brain instance.
        /// Used for monitoring system health and triggering error recovery procedures.
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last snapshot taken by this brain instance.
        /// Used to track snapshot frequency and ensure regular data capture.
        /// </summary>
        public DateTime? LastSnapshot { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this brain's WebSocket connection is currently active.
        /// Critical for real-time data streaming and market monitoring capabilities.
        /// </summary>
        public bool IsWebSocketConnected { get; set; }

        /// <summary>
        /// Historical performance metrics tracking CPU usage over time.
        /// Used for performance analysis and resource optimization decisions.
        /// </summary>
        [Required]
        [JsonPropertyName("cpuUsageHistory")]
        public List<MetricHistory> CpuUsageHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking event queue depth over time.
        /// Used to monitor system load and identify potential bottlenecks.
        /// </summary>
        [Required]
        [JsonPropertyName("eventQueueHistory")]
        public List<MetricHistory> EventQueueHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking ticker queue depth over time.
        /// Used to monitor market data processing efficiency.
        /// </summary>
        [Required]
        [JsonPropertyName("tickerQueueHistory")]
        public List<MetricHistory> TickerQueueHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking notification queue depth over time.
        /// Used to monitor alert and notification processing.
        /// </summary>
        [Required]
        [JsonPropertyName("notificationQueueHistory")]
        public List<MetricHistory> NotificationQueueHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking order book queue depth over time.
        /// Used to monitor order book update processing efficiency.
        /// </summary>
        [Required]
        [JsonPropertyName("orderbookQueueHistory")]
        public List<MetricHistory> OrderbookQueueHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking the number of markets being watched over time.
        /// Used to analyze market coverage trends and optimization effectiveness.
        /// </summary>
        [Required]
        [JsonPropertyName("marketCountHistory")]
        public List<MetricHistory> MarketCountHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking error counts over time.
        /// Used to identify patterns in system instability and recovery effectiveness.
        /// </summary>
        [Required]
        [JsonPropertyName("errorHistory")]
        public List<MetricHistory> ErrorHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking refresh cycle duration in seconds over time.
        /// Used to monitor and optimize market data refresh performance.
        /// </summary>
        [Required]
        [JsonPropertyName("refreshCycleSecondsHistory")]
        public List<MetricHistory> RefreshCycleSecondsHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking refresh cycle intervals over time.
        /// Used to ensure consistent and timely market data updates.
        /// </summary>
        [Required]
        [JsonPropertyName("refreshCycleIntervalHistory")]
        public List<MetricHistory> RefreshCycleIntervalHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking the number of markets refreshed per cycle over time.
        /// Used to monitor refresh efficiency and system throughput.
        /// </summary>
        [Required]
        [JsonPropertyName("refreshMarketCountHistory")]
        public List<MetricHistory> RefreshMarketCountHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking CPU usage percentage during refresh cycles over time.
        /// Used to correlate resource usage with system performance.
        /// </summary>
        [Required]
        [JsonPropertyName("refreshUsagePercentageHistory")]
        public List<MetricHistory> RefreshUsagePercentageHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Historical performance metrics tracking the dates of performance samples over time.
        /// Used to maintain chronological context for performance analysis.
        /// </summary>
        [Required]
        [JsonPropertyName("performanceSampleDateHistory")]
        public List<MetricHistory> PerformanceSampleDateHistory { get; set; } = new List<MetricHistory>();

        /// <summary>
        /// Gets or sets a value indicating whether the last refresh cycle completed within acceptable time limits.
        /// Used to monitor system performance and trigger optimization measures when needed.
        /// </summary>
        public bool LastRefreshTimeAcceptable { get; set; }

        /// <summary>
        /// Gets or sets the latest comprehensive performance metrics received from the brain.
        /// This object contains detailed performance data that is stored as-is for monitoring purposes.
        /// </summary>
        public object? LatestPerformanceMetrics { get; set; }

        /// <summary>
        /// Creates a deep clone of this BrainPersistence instance.
        /// Uses JSON serialization for safe copying of complex nested objects.
        /// </summary>
        /// <returns>A new BrainPersistence instance with all properties deeply cloned.</returns>
        public BrainPersistence Clone()
        {
            var json = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<BrainPersistence>(json)!;
        }

        /// <summary>
        /// Serializes this BrainPersistence instance to JSON with performance metrics.
        /// </summary>
        /// <returns>A tuple containing the JSON string and the serialization time in milliseconds.</returns>
        public (string Json, long Milliseconds) SerializeWithMetrics()
        {
            var stopwatch = Stopwatch.StartNew();
            var json = JsonSerializer.Serialize(this);
            stopwatch.Stop();
            return (json, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Deserializes a JSON string to BrainPersistence with performance metrics.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A tuple containing the deserialized instance and the deserialization time in milliseconds.</returns>
        public static (BrainPersistence Instance, long Milliseconds) DeserializeWithMetrics(string json)
        {
            var stopwatch = Stopwatch.StartNew();
            var instance = JsonSerializer.Deserialize<BrainPersistence>(json)!;
            stopwatch.Stop();
            return (instance, stopwatch.ElapsedMilliseconds);
        }
    }



    /// <summary>
    /// Represents a single historical data point for a performance metric.
    /// Used to track time-series data for various system performance indicators.
    /// </summary>
    public class MetricHistory
    {
        /// <summary>
        /// Gets or sets the timestamp when this metric value was recorded.
        /// Used for chronological ordering and time-based analysis of performance trends.
        /// </summary>
        [Required]
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the numerical value of the performance metric at the recorded timestamp.
        /// The interpretation depends on the specific metric being tracked (e.g., CPU %, queue depth, count).
        /// </summary>
        [JsonPropertyName("value")]
        public double Value { get; set; }
    }

}
