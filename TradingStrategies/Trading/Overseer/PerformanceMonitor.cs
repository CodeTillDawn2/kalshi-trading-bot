using BacklashInterfaces.PerformanceMetrics;
using System.Collections.Concurrent;

namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Generic performance monitor for tracking various metrics
    /// in the trading simulator. This class collects comprehensive performance data
    /// including execution times, memory usage, trade counts, and detailed metrics.
    /// It supports both general operation metrics and specialized simulation performance metrics
    /// from StrategySimulation, with typed access via StrategySimulationPerformanceMetrics.
    /// All metrics collection can be enabled/disabled via the EnablePerformanceMetrics property.
    /// </summary>
    public class PerformanceMonitor : BasePerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, Dictionary<string, object>> _simulationMetrics;

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled.
        /// When disabled, metric recording methods will return early without storing data.
        /// Default is true. Can be configured from appsettings.json Simulation:Simulation_EnablePerformanceMetrics.
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the PerformanceMonitor.
        /// </summary>
        public PerformanceMonitor()
        {

        }

    }
}