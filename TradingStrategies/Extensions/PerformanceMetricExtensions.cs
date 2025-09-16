using System;
using System.Collections.Generic;
using System.Linq;
using BacklashCommon.Performance;

namespace TradingStrategies.Extensions
{
    /// <summary>
    /// Extension methods to convert complex performance objects into simple PerformanceMetric instances
    /// for uniform GUI handling across all performance monitors.
    /// </summary>
    public static class PerformanceMetricExtensions
    {
        /// <summary>
        /// Converts StrategySimulationPerformanceMetrics to a collection of simple PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToPerformanceMetrics(
            this global::TradingStrategies.Trading.Overseer.StrategySimulationPerformanceMetrics simulationMetrics,
            string simulationName)
        {
            var metrics = new List<PerformanceMetric>();

            // Execution Time - Speed Dial
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{simulationName}_ExecutionTime",
                Name = "Execution Time",
                Description = "Total time spent executing the simulation",
                Value = simulationMetrics.TotalExecutionTime.TotalMilliseconds,
                Unit = "ms",
                VisualType = VisualType.SpeedDial,
                Category = "Strategy Simulation",
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 5000, // 5 seconds
                CriticalThreshold = 30000  // 30 seconds
            });

            // Average Execution Time - Speed Dial
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{simulationName}_AvgExecutionTime",
                Name = "Avg Execution Time",
                Description = "Average execution time per operation",
                Value = simulationMetrics.AverageExecutionTimeMs,
                Unit = "ms",
                VisualType = VisualType.SpeedDial,
                Category = "Strategy Simulation",
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 100, // 100ms
                CriticalThreshold = 1000 // 1 second
            });

            // Peak Memory Usage - Progress Bar
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{simulationName}_PeakMemory",
                Name = "Peak Memory",
                Description = "Maximum memory usage during simulation",
                Value = simulationMetrics.PeakMemoryUsage / (1024.0 * 1024.0), // Convert to MB
                Unit = "MB",
                VisualType = VisualType.ProgressBar,
                Category = "Strategy Simulation",
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 500, // 500MB
                CriticalThreshold = 1000 // 1GB
            });

            // Memory Threshold Ratio - Traffic Light
            if (simulationMetrics.MemoryThresholdMB > 0)
            {
                var memoryRatio = (simulationMetrics.PeakMemoryUsage / (1024.0 * 1024.0)) / simulationMetrics.MemoryThresholdMB * 100;
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"{simulationName}_MemoryThreshold",
                    Name = "Memory Usage %",
                    Description = "Memory usage as percentage of threshold",
                    Value = memoryRatio,
                    Unit = "%",
                    VisualType = VisualType.TrafficLight,
                    Category = "Strategy Simulation",
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 75,
                    CriticalThreshold = 90
                });
            }

            // Total Trades Executed - Counter
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{simulationName}_TotalTrades",
                Name = "Total Trades",
                Description = "Number of trades executed during simulation",
                Value = simulationMetrics.TotalTradesExecuted,
                Unit = "trades",
                VisualType = VisualType.Counter,
                Category = "Strategy Simulation",
                Timestamp = DateTime.UtcNow
            });

            // Current Position - Numeric Display
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{simulationName}_CurrentPosition",
                Name = "Current Position",
                Description = "Current trading position",
                Value = simulationMetrics.CurrentPosition,
                Unit = "units",
                VisualType = VisualType.NumericDisplay,
                Category = "Strategy Simulation",
                Timestamp = DateTime.UtcNow
            });

            // Current Cash - Numeric Display
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{simulationName}_CurrentCash",
                Name = "Current Cash",
                Description = "Current cash balance",
                Value = simulationMetrics.CurrentCash,
                Unit = "USD",
                VisualType = VisualType.NumericDisplay,
                Category = "Strategy Simulation",
                Timestamp = DateTime.UtcNow
            });

            return metrics;
        }

        /// <summary>
        /// Converts PatternUtilsPerformanceMetrics to a collection of simple PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToPerformanceMetrics(
            this global::TradingStrategies.Trading.Overseer.PatternUtilsPerformanceMetrics patternMetrics,
            string patternName)
        {
            var metrics = new List<PerformanceMetric>();

            // Total Calculations - Counter
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{patternName}_TotalCalculations",
                Name = "Total Calculations",
                Description = "Number of pattern calculations performed",
                Value = patternMetrics.TotalCalculations,
                Unit = "calculations",
                VisualType = VisualType.Counter,
                Category = "Pattern Utils",
                Timestamp = DateTime.UtcNow
            });

            // Cache Hit Rate - Pie Chart
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{patternName}_CacheHitRate",
                Name = "Cache Hit Rate",
                Description = "Percentage of cache hits vs misses",
                Value = patternMetrics.CacheHitRate,
                SecondaryValue = 100 - patternMetrics.CacheHitRate, // Miss rate
                Unit = "%",
                VisualType = VisualType.PieChart,
                Category = "Pattern Utils",
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 50,
                CriticalThreshold = 80
            });

            // Average Calculation Time - Speed Dial
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{patternName}_AvgCalcTime",
                Name = "Avg Calc Time",
                Description = "Average time per calculation",
                Value = patternMetrics.AverageCalculationTimeMs,
                Unit = "ms",
                VisualType = VisualType.SpeedDial,
                Category = "Pattern Utils",
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 10, // 10ms
                CriticalThreshold = 100 // 100ms
            });

            // Throughput - Speed Dial
            if (patternMetrics.Throughput.HasValue)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"{patternName}_Throughput",
                    Name = "Throughput",
                    Description = "Calculations per second",
                    Value = patternMetrics.Throughput.Value,
                    Unit = "calc/sec",
                    VisualType = VisualType.SpeedDial,
                    Category = "Pattern Utils",
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100,
                    CriticalThreshold = 1000
                });
            }

            // Memory Usage - Progress Bar
            if (patternMetrics.MemoryUsage.HasValue)
            {
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"{patternName}_MemoryUsage",
                    Name = "Memory Usage",
                    Description = "Current memory usage",
                    Value = patternMetrics.MemoryUsage.Value / (1024.0 * 1024.0), // Convert to MB
                    Unit = "MB",
                    VisualType = VisualType.ProgressBar,
                    Category = "Pattern Utils",
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 100, // 100MB
                    CriticalThreshold = 500 // 500MB
                });
            }

            return metrics;
        }

        /// <summary>
        /// Converts PerformanceStats to a collection of simple PerformanceMetrics
        /// </summary>
        public static IEnumerable<PerformanceMetric> ToPerformanceMetrics(
            this global::TradingStrategies.Trading.Overseer.PerformanceStats stats,
            string methodName)
        {
            var metrics = new List<PerformanceMetric>();

            // Record Count - Badge
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{methodName}_RecordCount",
                Name = "Record Count",
                Description = "Number of performance records collected",
                Value = stats.RecordCount,
                Unit = "records",
                VisualType = VisualType.Badge,
                Category = "Performance Stats",
                Timestamp = DateTime.UtcNow
            });

            // Average Execution Time - Speed Dial
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{methodName}_AvgExecutionTime",
                Name = "Avg Execution Time",
                Description = "Average execution time across all records",
                Value = stats.AverageExecutionTimeMs,
                Unit = "ms",
                VisualType = VisualType.SpeedDial,
                Category = "Performance Stats",
                Timestamp = DateTime.UtcNow,
                MinThreshold = 0,
                WarningThreshold = 100,
                CriticalThreshold = 1000
            });

            // Items Processed - Counter
            metrics.Add(new GeneralPerformanceMetric
            {
                Id = $"{methodName}_TotalItemsProcessed",
                Name = "Items Processed",
                Description = "Total number of items processed",
                Value = stats.TotalItemsProcessed,
                Unit = "items",
                VisualType = VisualType.Counter,
                Category = "Performance Stats",
                Timestamp = DateTime.UtcNow
            });

            // Success Rate - Progress Bar
            if (stats.TotalItemsFound > 0 && stats.TotalItemsProcessed > 0)
            {
                var successRate = (double)stats.TotalItemsFound / stats.TotalItemsProcessed * 100;
                metrics.Add(new GeneralPerformanceMetric
                {
                    Id = $"{methodName}_SuccessRate",
                    Name = "Success Rate",
                    Description = "Percentage of items successfully found",
                    Value = successRate,
                    Unit = "%",
                    VisualType = VisualType.ProgressBar,
                    Category = "Performance Stats",
                    Timestamp = DateTime.UtcNow,
                    MinThreshold = 0,
                    WarningThreshold = 50,
                    CriticalThreshold = 80
                });
            }

            return metrics;
        }
    }
}