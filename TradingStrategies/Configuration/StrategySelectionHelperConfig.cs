namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration options for the StrategySelectionHelper.
    /// </summary>
    public class StrategySelectionHelperConfig
    {
        /// <summary>
        /// Whether to enable performance metrics collection for strategy instantiation.
        /// When disabled, no performance metrics or logging occurs. When enabled, comprehensive
        /// metrics including instantiation time, memory allocation, and instance counts are collected.
        /// Defaults to false for performance reasons (individual instance tracking has overhead).
        /// </summary>
        required
        public bool EnablePerformanceMetrics { get; set; }
    }
}
