using System.ComponentModel.DataAnnotations;

namespace TradingSimulator.Configuration
{
    /// <summary>
    /// Configuration for discrepancy detection parameters.
    /// </summary>
    public class DiscrepancyDetectionConfig
    {
        /// <summary>
        /// Whether to enable velocity discrepancy detection.
        /// </summary>
        [Required]
        public bool Enabled { get; set; }

        /// <summary>
        /// Threshold for detecting velocity discrepancies.
        /// </summary>
        [Required]
        public double VelocityThreshold { get; set; }

        /// <summary>
        /// Minimum number of snapshots required for discrepancy detection.
        /// </summary>
        [Required]
        public int MinSnapshotsForDetection { get; set; }
    }
}