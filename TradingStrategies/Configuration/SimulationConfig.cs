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
        /// The configuration section name for SimulationConfig.
        /// </summary>
        public const string SectionName = "Simulator:Simulation";

        /// <summary>
        /// Minimum valid price for YES/NO contracts in the simulation.
        /// Used for price validation and order book bounds checking.
        /// </summary>
        [Required(ErrorMessage = "The 'MinContractPrice' is missing in the configuration.")]
        public int MinContractPrice { get; set; }

        /// <summary>
        /// Maximum valid price for YES/NO contracts in the simulation.
        /// Used for price validation and order book bounds checking.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxContractPrice' is missing in the configuration.")]
        public int MaxContractPrice { get; set; }

        /// <summary>
        /// Position sizing percentage for combo orders (LongPostAsk/ShortPostYes).
        /// Determines what percentage of current position to rest after market execution.
        /// Expressed as decimal (e.g., 1.0 for 100%).
        /// </summary>
        [Required(ErrorMessage = "The 'ComboPositionSizePercentage' is missing in the configuration.")]
        public double ComboPositionSizePercentage { get; set; }

        /// <summary>
        /// Maximum number of resting orders allowed per simulation.
        /// Prevents memory issues with excessive order accumulation.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxRestingOrders' is missing in the configuration.")]
        public int MaxRestingOrders { get; set; }

        /// <summary>
        /// Performance threshold for triggering detailed metrics collection.
        /// If ProcessSnapshot takes longer than this threshold (in milliseconds),
        /// additional performance metrics will be collected.
        /// </summary>
        [Required(ErrorMessage = "The 'PerformanceThresholdMs' is missing in the configuration.")]
        public int PerformanceThresholdMs { get; set; }

        /// <summary>
        /// Memory usage threshold for triggering garbage collection hints.
        /// If peak memory usage exceeds this threshold (in MB),
        /// the system may suggest optimization measures.
        /// </summary>
        [Required(ErrorMessage = "The 'MemoryThresholdMB' is missing in the configuration.")]
        public int MemoryThresholdMB { get; set; }

        /// <summary>
        /// Enable detailed performance logging for high-frequency scenarios.
        /// When enabled, collects timing data for each simulation step.
        /// </summary>
        [Required(ErrorMessage = "The 'EnableDetailedPerformanceLogging' is missing in the configuration.")]
        public bool EnableDetailedPerformanceLogging { get; set; }

        /// <summary>
        /// Batch size for processing multiple snapshots asynchronously.
        /// Controls how many snapshots are processed concurrently in async operations.
        /// </summary>
        [Required(ErrorMessage = "The 'AsyncBatchSize' is missing in the configuration.")]
        public int AsyncBatchSize { get; set; }

        /// <summary>
        /// Timeout for async operations in seconds.
        /// Prevents hanging operations during large dataset processing.
        /// </summary>
        [Required(ErrorMessage = "The 'AsyncTimeoutSeconds' is missing in the configuration.")]
        public int AsyncTimeoutSeconds { get; set; }

        /// <summary>
        /// Default quantity for market orders when not specified in ActionDecision.Quantity.
        /// Used as fallback quantity for Long/Short actions. Controls the base contract count
        /// for market executions in simulation scenarios.
        /// </summary>
        [Required(ErrorMessage = "The 'DefaultMarketOrderQuantity' is missing in the configuration.")]
        public int DefaultMarketOrderQuantity { get; set; }

        /// <summary>
        /// Maximum number of trades allowed per snapshot processing cycle.
        /// Prevents excessive trading activity and potential performance degradation
        /// in high-frequency trading scenarios. Acts as a circuit breaker for runaway strategies.
        /// </summary>
        [Required(ErrorMessage = "The 'MaxTradesPerSnapshot' is missing in the configuration.")]
        public int MaxTradesPerSnapshot { get; set; }

        /// <summary>
        /// Enable detailed timing collection for strategy decision making process.
        /// When enabled, measures execution time spent in Strategy.GetAction calls
        /// and logs warnings when thresholds are exceeded. Useful for performance optimization
        /// and bottleneck identification in high-frequency scenarios.
        /// </summary>
        [Required(ErrorMessage = "The 'EnableDecisionTiming' is missing in the configuration.")]
        public bool EnableDecisionTiming { get; set; }

        /// <summary>
        /// Performance threshold for decision timing warnings in milliseconds.
        /// If strategy decision making (Strategy.GetAction) takes longer than this threshold,
        /// detailed performance warnings will be logged to help identify bottlenecks.
        /// Lower values provide more sensitive monitoring for high-performance requirements.
        /// </summary>
        [Required(ErrorMessage = "The 'DecisionThresholdMs' is missing in the configuration.")]
        public int DecisionThresholdMs { get; set; }

        /// <summary>
        /// Band width ratio threshold for classification and analysis decisions.
        /// Used in strategies that involve band-based technical analysis (e.g., Bollinger bands).
        /// Represents the minimum band width ratio required to trigger certain trading conditions.
        /// Expressed as decimal (e.g., 0.1 for 10% band width ratio).
        /// </summary>
        [Required(ErrorMessage = "The 'BandWidthRatioThreshold' is missing in the configuration.")]
        public double BandWidthRatioThreshold { get; set; }

        /// <summary>
        /// Trade rate limit per snapshot for high-frequency trading scenarios.
        /// Limits the number of trading actions that can be executed within a single
        /// snapshot processing cycle. Helps prevent market impact and maintains
        /// realistic trading behavior under high-frequency conditions.
        /// </summary>
        [Required(ErrorMessage = "The 'TradeRateLimitPerSnapshot' is missing in the configuration.")]
        public int TradeRateLimitPerSnapshot { get; set; }

        /// <summary>
        /// Enable collection of performance metrics in StrategySimulation.
        /// When disabled, skips collection of execution timing, memory usage, trade counts, and apply timing
        /// to improve performance in high-throughput scenarios. Decision timing is controlled separately.
        /// </summary>
        [Required(ErrorMessage = "The 'Simulation_EnablePerformanceMetrics' is missing in the configuration.")]
        public bool Simulation_EnablePerformanceMetrics { get; set; }
    }
}
