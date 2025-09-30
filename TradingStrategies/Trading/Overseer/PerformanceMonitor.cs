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

        public void RecordSimulationMetrics(string className, Dictionary<string, object> metrics, bool enabled)
        {
            string category = "Simulation";
            if (!enabled)
            {
                foreach (var kvp in metrics)
                {
                    RecordDisabledMetric(className, kvp.Key, kvp.Key, $"Disabled metric: {kvp.Key}", 0.0, "", category);
                }
                return;
            }
            foreach (var kvp in metrics)
            {
                string id = kvp.Key;
                string name = kvp.Key;
                string description = GetDescriptionForMetric(id);
                double value = GetValueAsDouble(kvp.Value);
                string unit = GetUnitForMetric(id);
                switch (id)
                {
                    case "TotalExecutionTime":
                        RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                    case "AverageExecutionTimeMs":
                        RecordSpeedDialMetric(className, id, name, description, value, unit, category, null, null, null);
                        break;
                    case "PeakMemoryUsage":
                        RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                    case "TotalSnapshotsProcessed":
                        RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "PerformanceThresholdMs":
                        RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                    case "SlowOperationsCount":
                        RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "RestingOrdersCount":
                        RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "CurrentPosition":
                        RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                    case "CurrentCash":
                        RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                    case "TotalTradesExecuted":
                        RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "AverageDecisionTimeMs":
                        RecordSpeedDialMetric(className, id, name, description, value, unit, category, null, null, null);
                        break;
                    case "AverageApplyTimeMs":
                        RecordSpeedDialMetric(className, id, name, description, value, unit, category, null, null, null);
                        break;
                    case "SlowDecisionsCount":
                        RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "MethodName":
                        RecordNumericDisplayMetric(className, id, name, description, 0, unit, category); // or something
                        break;
                    case "TotalExecutionTimeMs":
                        RecordSpeedDialMetric(className, id, name, description, value, unit, category, null, null, null);
                        break;
                    case "TotalItemsProcessed":
                        RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "TotalItemsFound":
                        RecordCounterMetric(className, id, name, description, value, unit, category);
                        break;
                    case "ItemCheckTimes":
                        // Skip or handle differently
                        break;
                    case "Timestamp":
                        // Skip
                        break;
                    default:
                        RecordNumericDisplayMetric(className, id, name, description, value, unit, category);
                        break;
                }
            }
        }

        private string GetDescriptionForMetric(string id)
        {
            switch (id)
            {
                case "TotalExecutionTime": return "Total time spent processing all snapshots";
                case "AverageExecutionTimeMs": return "Average time to process one snapshot";
                case "PeakMemoryUsage": return "Maximum memory usage during simulation";
                case "TotalSnapshotsProcessed": return "Number of snapshots processed";
                case "PerformanceThresholdMs": return "Threshold for slow operations";
                case "SlowOperationsCount": return "Number of operations exceeding performance threshold";
                case "RestingOrdersCount": return "Current number of resting orders";
                case "CurrentPosition": return "Current position size";
                case "CurrentCash": return "Current cash balance";
                case "TotalTradesExecuted": return "Total number of trades executed";
                case "AverageDecisionTimeMs": return "Average time for strategy decisions";
                case "AverageApplyTimeMs": return "Average time to apply trading actions";
                case "SlowDecisionsCount": return "Number of slow strategy decisions";
                case "MethodName": return "Name of the method being monitored";
                case "TotalExecutionTimeMs": return "Total execution time in milliseconds";
                case "TotalItemsProcessed": return "Total number of items processed";
                case "TotalItemsFound": return "Total number of items found";
                default: return $"Metric: {id}";
            }
        }

        private string GetUnitForMetric(string id)
        {
            switch (id)
            {
                case "TotalExecutionTime": return "ms";
                case "AverageExecutionTimeMs": return "ms";
                case "PeakMemoryUsage": return "bytes";
                case "TotalSnapshotsProcessed": return "count";
                case "PerformanceThresholdMs": return "ms";
                case "SlowOperationsCount": return "count";
                case "RestingOrdersCount": return "count";
                case "CurrentPosition": return "contracts";
                case "CurrentCash": return "USD";
                case "TotalTradesExecuted": return "count";
                case "AverageDecisionTimeMs": return "ms";
                case "AverageApplyTimeMs": return "ms";
                case "SlowDecisionsCount": return "count";
                case "MethodName": return "";
                case "TotalExecutionTimeMs": return "ms";
                case "TotalItemsProcessed": return "count";
                case "TotalItemsFound": return "count";
                default: return "";
            }
        }

        private double GetValueAsDouble(object value)
        {
            if (value is TimeSpan ts) return ts.TotalMilliseconds;
            if (value is double d) return d;
            if (value is int i) return (double)i;
            if (value is long l) return (double)l;
            if (value is Dictionary<string, long> dict) return dict.Values.Sum(); // for ItemCheckTimes
            return 0.0;
        }

    }
}
