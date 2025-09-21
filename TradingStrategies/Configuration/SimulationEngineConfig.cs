using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration settings for the SimulationEngine.
    /// </summary>
    public class SimulationEngineConfig
    {
        /// <summary>
        /// The configuration section name for SimulationEngineConfig.
        /// </summary>
        public const string SectionName = "Simulator:SimulationEngine";

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled.
        /// </summary>
        /// <value>Default is true.</value>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
