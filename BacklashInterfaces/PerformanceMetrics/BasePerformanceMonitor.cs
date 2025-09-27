using Microsoft.Extensions.Logging;
using BacklashInterfaces.PerformanceMetrics;

namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Base implementation of IPerformanceMonitor that provides uniform handling of performance metrics.
    /// This class captures metrics from different VisualTypes and stores them for uniform processing.
    /// Derived classes should inherit from this to get consistent metric handling.
    /// </summary>
    public class BasePerformanceMonitor : IPerformanceMonitor
    {
        private readonly ILogger? _logger;
        private readonly List<(string ClassName, PerformanceMetric Metric)> _recordedMetrics = new();

        /// <summary>
        /// Initializes a new instance of the BasePerformanceMonitor class.
        /// </summary>
        /// <param name="logger">Optional logger for recording operations.</param>
        protected BasePerformanceMonitor(ILogger? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the list of recorded metrics.
        /// </summary>
        public IReadOnlyList<(string ClassName, PerformanceMetric Metric)> RecordedMetrics => _recordedMetrics;

        /// <summary>
        /// Records a SpeedDial metric with the specified parameters.
        /// </summary>
        public virtual void RecordSpeedDialMetric(string className, string id, string name, string description, double value, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null, bool metricsEnabled = true)
        {
            if (!metricsEnabled) return;

            var metric = new GeneralPerformanceMetric
            {
                Id = id,
                Name = name,
                Description = description,
                Value = value,
                Unit = unit,
                VisualType = VisualType.SpeedDial,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = minThreshold,
                WarningThreshold = warningThreshold,
                CriticalThreshold = criticalThreshold
            };

            _recordedMetrics.Add((className, metric));
            _logger?.LogDebug("SpeedDial metric recorded from {ClassName}: {Name}={Value}", className, name, value);
        }

        /// <summary>
        /// Records a ProgressBar metric with the specified parameters.
        /// </summary>
        public virtual void RecordProgressBarMetric(string className, string id, string name, string description, double value, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null, bool metricsEnabled = true)
        {
            if (!metricsEnabled) return;

            var metric = new GeneralPerformanceMetric
            {
                Id = id,
                Name = name,
                Description = description,
                Value = value,
                Unit = unit,
                VisualType = VisualType.ProgressBar,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = minThreshold,
                WarningThreshold = warningThreshold,
                CriticalThreshold = criticalThreshold
            };

            _recordedMetrics.Add((className, metric));
            _logger?.LogDebug("ProgressBar metric recorded from {ClassName}: {Name}={Value}", className, name, value);
        }

        /// <summary>
        /// Records a Counter metric with the specified parameters.
        /// </summary>
        public virtual void RecordCounterMetric(string className, string id, string name, string description, double value, string unit, string category, bool metricsEnabled = true)
        {
            if (!metricsEnabled) return;

            var metric = new GeneralPerformanceMetric
            {
                Id = id,
                Name = name,
                Description = description,
                Value = value,
                Unit = unit,
                VisualType = VisualType.Counter,
                Category = category,
                Timestamp = DateTime.UtcNow
            };

            _recordedMetrics.Add((className, metric));
            _logger?.LogDebug("Counter metric recorded from {ClassName}: {Name}={Value}", className, name, value);
        }

        /// <summary>
        /// Records a TrafficLight metric with the specified parameters.
        /// </summary>
        public virtual void RecordTrafficLightMetric(string className, string id, string name, string description, double value, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null, bool metricsEnabled = true)
        {
            if (!metricsEnabled) return;

            var metric = new GeneralPerformanceMetric
            {
                Id = id,
                Name = name,
                Description = description,
                Value = value,
                Unit = unit,
                VisualType = VisualType.TrafficLight,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = minThreshold,
                WarningThreshold = warningThreshold,
                CriticalThreshold = criticalThreshold
            };

            _recordedMetrics.Add((className, metric));
            _logger?.LogDebug("TrafficLight metric recorded from {ClassName}: {Name}={Value}", className, name, value);
        }

        /// <summary>
        /// Records a PieChart metric with the specified parameters.
        /// </summary>
        public virtual void RecordPieChartMetric(string className, string id, string name, string description, double value, double? secondaryValue, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null, bool metricsEnabled = true)
        {
            if (!metricsEnabled) return;

            var metric = new GeneralPerformanceMetric
            {
                Id = id,
                Name = name,
                Description = description,
                Value = value,
                SecondaryValue = secondaryValue,
                Unit = unit,
                VisualType = VisualType.PieChart,
                Category = category,
                Timestamp = DateTime.UtcNow,
                MinThreshold = minThreshold,
                WarningThreshold = warningThreshold,
                CriticalThreshold = criticalThreshold
            };

            _recordedMetrics.Add((className, metric));
            _logger?.LogDebug("PieChart metric recorded from {ClassName}: {Name}={Value}", className, name, value);
        }

        /// <summary>
        /// Records a NumericDisplay metric with the specified parameters.
        /// </summary>
        public virtual void RecordNumericDisplayMetric(string className, string id, string name, string description, double value, string unit, string category, bool metricsEnabled = true)
        {
            if (!metricsEnabled) return;

            var metric = new GeneralPerformanceMetric
            {
                Id = id,
                Name = name,
                Description = description,
                Value = value,
                Unit = unit,
                VisualType = VisualType.NumericDisplay,
                Category = category,
                Timestamp = DateTime.UtcNow
            };

            _recordedMetrics.Add((className, metric));
            _logger?.LogDebug("NumericDisplay metric recorded from {ClassName}: {Name}={Value}", className, name, value);
        }

        /// <summary>
        /// Records a Badge metric with the specified parameters.
        /// </summary>
        public virtual void RecordBadgeMetric(string className, string id, string name, string description, double value, string unit, string category, bool metricsEnabled = true)
        {
            if (!metricsEnabled) return;

            var metric = new GeneralPerformanceMetric
            {
                Id = id,
                Name = name,
                Description = description,
                Value = value,
                Unit = unit,
                VisualType = VisualType.Badge,
                Category = category,
                Timestamp = DateTime.UtcNow
            };

            _recordedMetrics.Add((className, metric));
            _logger?.LogDebug("Badge metric recorded from {ClassName}: {Name}={Value}", className, name, value);
        }

        /// <summary>
        /// Records a LineChart metric.
        /// </summary>
        public virtual void RecordLineChartMetric(string className, PerformanceMetric metric)
        {
            metric.VisualType = VisualType.LineChart;
            _recordedMetrics.Add((className, metric));
            _logger?.LogDebug("LineChart metric recorded from {ClassName}: {Name}", className, metric.Name);
        }

        /// <summary>
        /// Records a DataGrid metric.
        /// </summary>
        public virtual void RecordDataGridMetric(string className, PerformanceMetric metric)
        {
            metric.VisualType = VisualType.DataGrid;
            _recordedMetrics.Add((className, metric));
            _logger?.LogDebug("DataGrid metric recorded from {ClassName}: {Name}", className, metric.Name);
        }

        /// <summary>
        /// Clears all recorded metrics.
        /// </summary>
        public virtual void ClearMetrics()
        {
            _recordedMetrics.Clear();
            _logger?.LogDebug("All metrics cleared");
        }
    }
}