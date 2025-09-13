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
    }
}