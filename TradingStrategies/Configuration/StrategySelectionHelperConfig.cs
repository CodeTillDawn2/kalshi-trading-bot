using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration class for StrategySelectionHelper parameters.
/// Controls performance metrics collection for strategy instantiation.
/// </summary>
public class StrategySelectionHelperConfig
{
    /// <summary>
    /// Enables or disables performance metrics collection in StrategySelectionHelper.
    /// When enabled, collects strategy instantiation timing and memory allocation metrics for each strategy instance.
    /// Disable for performance optimization in high-throughput scenarios where metrics are not needed.
    /// Default: false (due to performance impact of individual instance tracking)
    /// </summary>
    [Required]
    public bool EnablePerformanceMetrics { get; set; } = false;
}