using System;
using System.Text.Json.Serialization;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration settings for the SimulationEngine.
    /// </summary>
    public class SimulationEngineConfig
    {
        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled.
        /// </summary>
        /// <value>Default is true.</value>
        required
        public bool EnablePerformanceMetrics { get; set; } = true;
    }
}
