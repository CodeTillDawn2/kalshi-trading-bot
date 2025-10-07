namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Defines the visual representation type for performance metrics in GUI components.
    /// This allows uniform handling of different metric types across all performance monitors.
    /// </summary>
    public enum VisualType
    {
        /// <summary>
        /// Speed dial or circular gauge (good for time durations, percentages)
        /// </summary>
        SpeedDial,

        /// <summary>
        /// Linear progress bar (good for completion ratios, resource usage)
        /// </summary>
        ProgressBar,

        /// <summary>
        /// Animated counter with rolling numbers (good for counts, totals)
        /// </summary>
        Counter,

        /// <summary>
        /// Traffic light indicator (good for threshold-based metrics)
        /// </summary>
        TrafficLight,

        /// <summary>
        /// Pie or donut chart (good for proportions, cache hit rates)
        /// </summary>
        PieChart,

        /// <summary>
        /// Simple numeric display with optional formatting
        /// </summary>
        NumericDisplay,

        /// <summary>
        /// Badge or notification counter (good for alerts, warnings)
        /// </summary>
        Badge,

        /// <summary>
        /// Time-series line chart (good for trends over time)
        /// </summary>
        LineChart,

        /// <summary>
        /// Data grid or table (good for detailed breakdowns)
        /// </summary>
        DataGrid,

        /// <summary>
        /// Disabled metric indicator (for metrics that are not active)
        /// </summary>
        DisabledMetric
    }
}