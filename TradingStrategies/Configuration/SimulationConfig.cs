using System.Text.Json.Serialization;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration class containing simulation-related parameters used throughout the trading bot system.
    /// These settings control simulation behavior, fees, position sizing, and performance thresholds
    /// for strategy backtesting and evaluation. Values are injected via dependency injection
    /// and used by StrategySimulation class for accurate market simulation.
    /// Includes comprehensive data validation to prevent invalid parameter combinations.
    /// </summary>
    public class SimulationConfig
    {
        /// <summary>
        /// Trading fee rate applied to all market executions in simulation.
        /// Represents the taker fee percentage charged by the exchange.
        /// Expressed as decimal (e.g., 0.07 for 7%).
        /// </summary>
        [JsonRequired]
        public double TakerFeeRate { get; set; }

        /// <summary>
        /// Minimum valid price for YES/NO contracts in the simulation.
        /// Used for price validation and order book bounds checking.
        /// </summary>
        [JsonRequired]
        public int MinContractPrice { get; set; }

        /// <summary>
        /// Maximum valid price for YES/NO contracts in the simulation.
        /// Used for price validation and order book bounds checking.
        /// </summary>
        [JsonRequired]
        public int MaxContractPrice { get; set; }

        /// <summary>
        /// Position sizing percentage for combo orders (LongPostAsk/ShortPostYes).
        /// Determines what percentage of current position to rest after market execution.
        /// Expressed as decimal (e.g., 1.0 for 100%).
        /// </summary>
        [JsonRequired]
        public double ComboPositionSizePercentage { get; set; }

        /// <summary>
        /// Maximum number of resting orders allowed per simulation.
        /// Prevents memory issues with excessive order accumulation.
        /// </summary>
        [JsonRequired]
        public int MaxRestingOrders { get; set; }

        /// <summary>
        /// Performance threshold for triggering detailed metrics collection.
        /// If ProcessSnapshot takes longer than this threshold (in milliseconds),
        /// additional performance metrics will be collected.
        /// </summary>
        [JsonRequired]
        public int PerformanceThresholdMs { get; set; }

        /// <summary>
        /// Memory usage threshold for triggering garbage collection hints.
        /// If peak memory usage exceeds this threshold (in MB),
        /// the system may suggest optimization measures.
        /// </summary>
        [JsonRequired]
        public int MemoryThresholdMB { get; set; }

        /// <summary>
        /// Enable detailed performance logging for high-frequency scenarios.
        /// When enabled, collects timing data for each simulation step.
        /// </summary>
        [JsonRequired]
        public bool EnableDetailedPerformanceLogging { get; set; }

        /// <summary>
        /// Batch size for processing multiple snapshots asynchronously.
        /// Controls how many snapshots are processed concurrently in async operations.
        /// </summary>
        [JsonRequired]
        public int AsyncBatchSize { get; set; }

        /// <summary>
        /// Timeout for async operations in seconds.
        /// Prevents hanging operations during large dataset processing.
        /// </summary>
        [JsonRequired]
        public int AsyncTimeoutSeconds { get; set; }

        /// <summary>
        /// Default quantity for market orders when not specified in ActionDecision.Quantity.
        /// Used as fallback quantity for Long/Short actions. Controls the base contract count
        /// for market executions in simulation scenarios.
        /// </summary>
        [JsonRequired]
        public int DefaultMarketOrderQuantity { get; set; }

        /// <summary>
        /// Maximum number of trades allowed per snapshot processing cycle.
        /// Prevents excessive trading activity and potential performance degradation
        /// in high-frequency trading scenarios. Acts as a circuit breaker for runaway strategies.
        /// </summary>
        [JsonRequired]
        public int MaxTradesPerSnapshot { get; set; }

        /// <summary>
        /// Enable detailed timing collection for strategy decision making process.
        /// When enabled, measures execution time spent in Strategy.GetAction calls
        /// and logs warnings when thresholds are exceeded. Useful for performance optimization
        /// and bottleneck identification in high-frequency scenarios.
        /// </summary>
        [JsonRequired]
        public bool EnableDecisionTiming { get; set; }

        /// <summary>
        /// Performance threshold for decision timing warnings in milliseconds.
        /// If strategy decision making (Strategy.GetAction) takes longer than this threshold,
        /// detailed performance warnings will be logged to help identify bottlenecks.
        /// Lower values provide more sensitive monitoring for high-performance requirements.
        /// </summary>
        [JsonRequired]
        public int DecisionThresholdMs { get; set; }

        /// <summary>
        /// Band width ratio threshold for classification and analysis decisions.
        /// Used in strategies that involve band-based technical analysis (e.g., Bollinger bands).
        /// Represents the minimum band width ratio required to trigger certain trading conditions.
        /// Expressed as decimal (e.g., 0.1 for 10% band width ratio).
        /// </summary>
        [JsonRequired]
        public double BandWidthRatioThreshold { get; set; }

        /// <summary>
        /// Trade rate limit per snapshot for high-frequency trading scenarios.
        /// Limits the number of trading actions that can be executed within a single
        /// snapshot processing cycle. Helps prevent market impact and maintains
        /// realistic trading behavior under high-frequency conditions.
        /// </summary>
        [JsonRequired]
        public int TradeRateLimitPerSnapshot { get; set; }

        /// <summary>
        /// Enable collection of performance metrics in StrategySimulation.
        /// When disabled, skips collection of execution timing, memory usage, trade counts, and apply timing
        /// to improve performance in high-throughput scenarios. Decision timing is controlled separately.
        /// </summary>
        [JsonRequired]
        public bool Simulation_EnablePerformanceMetrics { get; set; }
    }
}