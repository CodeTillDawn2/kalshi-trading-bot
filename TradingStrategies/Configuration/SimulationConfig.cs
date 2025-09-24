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
        /// Performance threshold for triggering detailed metrics collection.
        /// If ProcessSnapshot takes longer than this threshold (in milliseconds),
        /// additional performance metrics will be collected.
        /// </summary>
        [Required(ErrorMessage = "The 'PerformanceThresholdMs' is missing in the configuration.")]
        public int PerformanceThresholdMs { get; set; }

        /// <summary>
        /// Batch size for processing multiple snapshots asynchronously.
        /// Controls how many snapshots are processed concurrently in async operations.
        /// </summary>
        [Required(ErrorMessage = "The 'AsyncBatchSize' is missing in the configuration.")]
        public int AsyncBatchSize { get; set; }

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
        /// Enable collection of performance metrics in StrategySimulation.
        /// When disabled, skips collection of execution timing, memory usage, trade counts, and apply timing
        /// to improve performance in high-throughput scenarios. Decision timing is controlled separately.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
