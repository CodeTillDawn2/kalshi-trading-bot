using BacklashInterfaces.PerformanceMetrics;

namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Interface for recording performance metrics across the KalshiBot system.
    /// Provides methods for each VisualType to ensure uniform handling of different metric types.
    /// </summary>
    /// <remarks>
    /// All methods require the className to identify the source of the metric.
    /// Metrics are recorded through specific methods for each visual type to ensure proper categorization.
    /// </remarks>
    public interface IPerformanceMonitor
    {

        /// <summary>
        /// Records a SpeedDial metric with the specified parameters.
        /// </summary>
        /// <param name="className">The name of the class recording the metric.</param>
        /// <param name="id">Unique identifier for the metric.</param>
        /// <param name="name">Display name for the metric.</param>
        /// <param name="description">Description of what the metric measures.</param>
        /// <param name="value">The primary numeric value.</param>
        /// <param name="unit">Unit of measurement (e.g., "ms", "%", "count").</param>
        /// <param name="category">Category for grouping metrics.</param>
        /// <param name="minThreshold">Optional minimum threshold for visual indicators.</param>
        /// <param name="warningThreshold">Optional warning threshold for visual indicators.</param>
        /// <param name="criticalThreshold">Optional critical threshold for visual indicators.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordSpeedDialMetric(string className, string id, string name, string description, double value, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null, bool metricsEnabled = true);

        /// <summary>
        /// Records a ProgressBar metric with the specified parameters.
        /// </summary>
        /// <param name="className">The name of the class recording the metric.</param>
        /// <param name="id">Unique identifier for the metric.</param>
        /// <param name="name">Display name for the metric.</param>
        /// <param name="description">Description of what the metric measures.</param>
        /// <param name="value">The primary numeric value.</param>
        /// <param name="unit">Unit of measurement (e.g., "ms", "%", "count").</param>
        /// <param name="category">Category for grouping metrics.</param>
        /// <param name="minThreshold">Optional minimum threshold for visual indicators.</param>
        /// <param name="warningThreshold">Optional warning threshold for visual indicators.</param>
        /// <param name="criticalThreshold">Optional critical threshold for visual indicators.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordProgressBarMetric(string className, string id, string name, string description, double value, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null, bool metricsEnabled = true);

        /// <summary>
        /// Records a Counter metric with the specified parameters.
        /// </summary>
        /// <param name="className">The name of the class recording the metric.</param>
        /// <param name="id">Unique identifier for the metric.</param>
        /// <param name="name">Display name for the metric.</param>
        /// <param name="description">Description of what the metric measures.</param>
        /// <param name="value">The primary numeric value.</param>
        /// <param name="unit">Unit of measurement (e.g., "ms", "%", "count").</param>
        /// <param name="category">Category for grouping metrics.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordCounterMetric(string className, string id, string name, string description, double value, string unit, string category, bool metricsEnabled = true);

        /// <summary>
        /// Records a TrafficLight metric with the specified parameters.
        /// </summary>
        /// <param name="className">The name of the class recording the metric.</param>
        /// <param name="id">Unique identifier for the metric.</param>
        /// <param name="name">Display name for the metric.</param>
        /// <param name="description">Description of what the metric measures.</param>
        /// <param name="value">The primary numeric value.</param>
        /// <param name="unit">Unit of measurement (e.g., "ms", "%", "count").</param>
        /// <param name="category">Category for grouping metrics.</param>
        /// <param name="minThreshold">Optional minimum threshold for visual indicators.</param>
        /// <param name="warningThreshold">Optional warning threshold for visual indicators.</param>
        /// <param name="criticalThreshold">Optional critical threshold for visual indicators.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordTrafficLightMetric(string className, string id, string name, string description, double value, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null, bool metricsEnabled = true);

        /// <summary>
        /// Records a PieChart metric with the specified parameters.
        /// </summary>
        /// <param name="className">The name of the class recording the metric.</param>
        /// <param name="id">Unique identifier for the metric.</param>
        /// <param name="name">Display name for the metric.</param>
        /// <param name="description">Description of what the metric measures.</param>
        /// <param name="value">The primary numeric value.</param>
        /// <param name="secondaryValue">Optional secondary value for comparison.</param>
        /// <param name="unit">Unit of measurement (e.g., "ms", "%", "count").</param>
        /// <param name="category">Category for grouping metrics.</param>
        /// <param name="minThreshold">Optional minimum threshold for visual indicators.</param>
        /// <param name="warningThreshold">Optional warning threshold for visual indicators.</param>
        /// <param name="criticalThreshold">Optional critical threshold for visual indicators.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordPieChartMetric(string className, string id, string name, string description, double value, double? secondaryValue, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null, bool metricsEnabled = true);

        /// <summary>
        /// Records a NumericDisplay metric with the specified parameters.
        /// </summary>
        /// <param name="className">The name of the class recording the metric.</param>
        /// <param name="id">Unique identifier for the metric.</param>
        /// <param name="name">Display name for the metric.</param>
        /// <param name="description">Description of what the metric measures.</param>
        /// <param name="value">The primary numeric value.</param>
        /// <param name="unit">Unit of measurement (e.g., "ms", "%", "count").</param>
        /// <param name="category">Category for grouping metrics.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordNumericDisplayMetric(string className, string id, string name, string description, double value, string unit, string category, bool metricsEnabled = true);

        /// <summary>
        /// Records a Badge metric with the specified parameters.
        /// </summary>
        /// <param name="className">The name of the class recording the metric.</param>
        /// <param name="id">Unique identifier for the metric.</param>
        /// <param name="name">Display name for the metric.</param>
        /// <param name="description">Description of what the metric measures.</param>
        /// <param name="value">The primary numeric value.</param>
        /// <param name="unit">Unit of measurement (e.g., "ms", "%", "count").</param>
        /// <param name="category">Category for grouping metrics.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordBadgeMetric(string className, string id, string name, string description, double value, string unit, string category, bool metricsEnabled = true);

        /// <summary>
        /// Records a LineChart metric.
        /// </summary>
        /// <param name="className">The name of the class recording the metric.</param>
        /// <param name="metric">The performance metric data.</param>
        void RecordLineChartMetric(string className, PerformanceMetric metric);

        /// <summary>
        /// Records a DataGrid metric.
        /// </summary>
        /// <param name="className">The name of the class recording the metric.</param>
        /// <param name="metric">The performance metric data.</param>
        void RecordDataGridMetric(string className, PerformanceMetric metric);

    }
}
