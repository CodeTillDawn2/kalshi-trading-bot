using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration class for EquityCalculator parameters.
/// Controls performance metrics collection for equity calculations.
/// </summary>
public class EquityCalculatorConfig
{
    /// <summary>
    /// The configuration section name for EquityCalculatorConfig.
    /// </summary>
    public const string SectionName = "Simulator:EquityCalculator";

    /// <summary>
    /// Enables or disables performance metrics collection in EquityCalculator.
    /// When enabled, measures execution time for each equity calculation and collects timing statistics.
    /// Disable for performance optimization in high-throughput scenarios where metrics are not needed.
    /// Default: true
    /// </summary>
    [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
    public bool EnablePerformanceMetrics { get; set; }
}
