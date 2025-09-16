using System;

namespace BacklashCommon.Performance
{
    /// <summary>
    /// General performance metric for basic measurements like execution time, counts, and ratios.
    /// Uses only primitive types for uniform GUI handling.
    /// </summary>
    public class GeneralPerformanceMetric : PerformanceMetric
    {
        /// <summary>
        /// Primary numeric value (execution time, count, percentage, etc.)
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Optional secondary value for ratios or comparisons
        /// </summary>
        public double? SecondaryValue { get; set; }

        /// <summary>
        /// Minimum threshold for visual indicators
        /// </summary>
        public double? MinThreshold { get; set; }

        /// <summary>
        /// Warning threshold for visual indicators
        /// </summary>
        public double? WarningThreshold { get; set; }

        /// <summary>
        /// Critical threshold for visual indicators
        /// </summary>
        public double? CriticalThreshold { get; set; }

        /// <summary>
        /// Initializes a new instance of the GeneralPerformanceMetric class.
        /// </summary>
        public GeneralPerformanceMetric()
        {
            Category = "General";
        }

        /// <summary>
        /// Gets the primary value of the performance metric.
        /// </summary>
        /// <returns>The primary value as an object.</returns>
        public override object GetPrimaryValue() => Value;

        /// <summary>
        /// Gets all values of the performance metric as a dictionary.
        /// </summary>
        /// <returns>A dictionary containing all metric values.</returns>
        public override Dictionary<string, object> GetAllValues()
        {
            var values = new Dictionary<string, object>
            {
                ["Value"] = Value,
                ["Unit"] = Unit,
                ["Timestamp"] = Timestamp
            };

            if (SecondaryValue.HasValue)
                values["SecondaryValue"] = SecondaryValue.Value;

            return values;
        }

        /// <summary>
        /// Gets the thresholds for the performance metric.
        /// </summary>
        /// <returns>A tuple containing minimum, warning, and critical thresholds.</returns>
        public override (double? Min, double? Warning, double? Critical) GetThresholds()
        {
            return (MinThreshold, WarningThreshold, CriticalThreshold);
        }

        /// <summary>
        /// Creates a metric for execution time measurement
        /// </summary>
        public static GeneralPerformanceMetric CreateExecutionTime(string name, long milliseconds, string methodName)
        {
            return new GeneralPerformanceMetric
            {
                Name = name,
                Description = $"Execution time for {methodName}",
                Value = milliseconds,
                Unit = "ms",
                VisualType = VisualType.SpeedDial,
                MinThreshold = 0,
                WarningThreshold = 1000, // 1 second
                CriticalThreshold = 5000  // 5 seconds
            };
        }

        /// <summary>
        /// Creates a metric for count measurements
        /// </summary>
        public static GeneralPerformanceMetric CreateCount(string name, int count, string description)
        {
            return new GeneralPerformanceMetric
            {
                Name = name,
                Description = description,
                Value = count,
                Unit = "count",
                VisualType = VisualType.Counter
            };
        }

        /// <summary>
        /// Creates a metric for percentage/ratio measurements
        /// </summary>
        public static GeneralPerformanceMetric CreatePercentage(string name, double percentage, string description)
        {
            return new GeneralPerformanceMetric
            {
                Name = name,
                Description = description,
                Value = percentage,
                Unit = "%",
                VisualType = VisualType.ProgressBar,
                MinThreshold = 0,
                WarningThreshold = 50,
                CriticalThreshold = 90
            };
        }
    }
}