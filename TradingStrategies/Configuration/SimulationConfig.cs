using System.ComponentModel.DataAnnotations;

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
        [Range(0.0, 1.0, ErrorMessage = "TakerFeeRate must be between 0 and 1.")]
        public double TakerFeeRate { get; set; } = 0.07;

        /// <summary>
        /// Minimum valid price for YES/NO contracts in the simulation.
        /// Used for price validation and order book bounds checking.
        /// </summary>
        [Range(1, 99, ErrorMessage = "MinContractPrice must be between 1 and 99.")]
        public int MinContractPrice { get; set; } = 1;

        /// <summary>
        /// Maximum valid price for YES/NO contracts in the simulation.
        /// Used for price validation and order book bounds checking.
        /// </summary>
        [Range(1, 99, ErrorMessage = "MaxContractPrice must be between 1 and 99.")]
        public int MaxContractPrice { get; set; } = 99;

        /// <summary>
        /// Position sizing percentage for combo orders (LongPostAsk/ShortPostYes).
        /// Determines what percentage of current position to rest after market execution.
        /// Expressed as decimal (e.g., 1.0 for 100%).
        /// </summary>
        [Range(0.1, 2.0, ErrorMessage = "ComboPositionSizePercentage must be between 0.1 and 2.0.")]
        public double ComboPositionSizePercentage { get; set; } = 1.0;

        /// <summary>
        /// Maximum number of resting orders allowed per simulation.
        /// Prevents memory issues with excessive order accumulation.
        /// </summary>
        [Range(1, 10000, ErrorMessage = "MaxRestingOrders must be between 1 and 10000.")]
        public int MaxRestingOrders { get; set; } = 1000;

        /// <summary>
        /// Performance threshold for triggering detailed metrics collection.
        /// If ProcessSnapshot takes longer than this threshold (in milliseconds),
        /// additional performance metrics will be collected.
        /// </summary>
        [Range(1, 10000, ErrorMessage = "PerformanceThresholdMs must be between 1 and 10000.")]
        public int PerformanceThresholdMs { get; set; } = 100;

        /// <summary>
        /// Memory usage threshold for triggering garbage collection hints.
        /// If peak memory usage exceeds this threshold (in MB),
        /// the system may suggest optimization measures.
        /// </summary>
        [Range(10, 10000, ErrorMessage = "MemoryThresholdMB must be between 10 and 10000.")]
        public int MemoryThresholdMB { get; set; } = 500;

        /// <summary>
        /// Enable detailed performance logging for high-frequency scenarios.
        /// When enabled, collects timing data for each simulation step.
        /// </summary>
        public bool EnableDetailedPerformanceLogging { get; set; } = false;

        /// <summary>
        /// Batch size for processing multiple snapshots asynchronously.
        /// Controls how many snapshots are processed concurrently in async operations.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "AsyncBatchSize must be between 1 and 1000.")]
        public int AsyncBatchSize { get; set; } = 10;

        /// <summary>
        /// Timeout for async operations in seconds.
        /// Prevents hanging operations during large dataset processing.
        /// </summary>
        [Range(1, 3600, ErrorMessage = "AsyncTimeoutSeconds must be between 1 and 3600.")]
        public int AsyncTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Default quantity for market orders when not specified in ActionDecision.Quantity.
        /// Used as fallback quantity for Long/Short actions. Controls the base contract count
        /// for market executions in simulation scenarios.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "DefaultMarketOrderQuantity must be between 1 and 1000.")]
        public int DefaultMarketOrderQuantity { get; set; } = 1;

        /// <summary>
        /// Maximum number of trades allowed per snapshot processing cycle.
        /// Prevents excessive trading activity and potential performance degradation
        /// in high-frequency trading scenarios. Acts as a circuit breaker for runaway strategies.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "MaxTradesPerSnapshot must be between 1 and 1000.")]
        public int MaxTradesPerSnapshot { get; set; } = 100;

        /// <summary>
        /// Enable detailed timing collection for strategy decision making process.
        /// When enabled, measures execution time spent in Strategy.GetAction calls
        /// and logs warnings when thresholds are exceeded. Useful for performance optimization
        /// and bottleneck identification in high-frequency scenarios.
        /// </summary>
        public bool EnableDecisionTiming { get; set; } = false;

        /// <summary>
        /// Performance threshold for decision timing warnings in milliseconds.
        /// If strategy decision making (Strategy.GetAction) takes longer than this threshold,
        /// detailed performance warnings will be logged to help identify bottlenecks.
        /// Lower values provide more sensitive monitoring for high-performance requirements.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "DecisionThresholdMs must be between 1 and 1000.")]
        public int DecisionThresholdMs { get; set; } = 10;

        /// <summary>
        /// Band width ratio threshold for classification and analysis decisions.
        /// Used in strategies that involve band-based technical analysis (e.g., Bollinger bands).
        /// Represents the minimum band width ratio required to trigger certain trading conditions.
        /// Expressed as decimal (e.g., 0.1 for 10% band width ratio).
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "BandWidthRatioThreshold must be between 0 and 1.")]
        public double BandWidthRatioThreshold { get; set; } = 0.1;

        /// <summary>
        /// Trade rate limit per snapshot for high-frequency trading scenarios.
        /// Limits the number of trading actions that can be executed within a single
        /// snapshot processing cycle. Helps prevent market impact and maintains
        /// realistic trading behavior under high-frequency conditions.
        /// </summary>
        [Range(1, 100, ErrorMessage = "TradeRateLimitPerSnapshot must be between 1 and 100.")]
        public int TradeRateLimitPerSnapshot { get; set; } = 10;
    }
}