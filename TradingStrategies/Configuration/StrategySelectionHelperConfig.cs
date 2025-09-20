using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration options for the StrategySelectionHelper.
    /// </summary>
    public class StrategySelectionHelperConfig
    {
        /// <summary>
        /// The configuration section name for StrategySelectionHelperConfig.
        /// </summary>
        public const string SectionName = "Simulator:StrategySelectionHelper";

        /// <summary>
        /// Whether to enable performance metrics collection for strategy instantiation.
        /// When disabled, no performance metrics or logging occurs. When enabled, comprehensive
        /// metrics including instantiation time, memory allocation, and instance counts are collected.
        /// Defaults to false for performance reasons (individual instance tracking has overhead).
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
        public bool EnablePerformanceMetrics { get; set; }
    }
}
