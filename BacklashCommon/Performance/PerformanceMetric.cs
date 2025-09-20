using System;

namespace BacklashCommon.Performance
{
    /// <summary>
    /// Base class for all performance metrics with uniform structure for GUI visualization.
    /// All metrics contain only primitive types or simple containers of primitives.
    /// </summary>
    public abstract class PerformanceMetric
    {
        /// <summary>
        /// Unique identifier for this metric instance
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Human-readable name for display
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this metric measures
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The visual representation type for GUI components
        /// </summary>
        public VisualType VisualType { get; set; }

        /// <summary>
        /// Unit of measurement (ms, MB, %, count, etc.)
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when this metric was recorded
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Category for grouping metrics in the UI
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets the primary value for display (implemented by derived classes)
        /// </summary>
        public abstract object GetPrimaryValue();

        /// <summary>
        /// Gets all values as a dictionary for uniform processing
        /// </summary>
        public abstract Dictionary<string, object> GetAllValues();

        /// <summary>
        /// Gets threshold values for visual indicators (min, warning, critical)
        /// </summary>
        public virtual (double? Min, double? Warning, double? Critical) GetThresholds()
        {
            return (null, null, null);
        }
    }
}
