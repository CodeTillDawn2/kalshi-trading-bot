using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for KalshiBotScopeManagerService settings.
    /// </summary>
    public class KalshiBotScopeManagerServiceConfig
    {
        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for KalshiBotScopeManagerService operations.
        /// </summary>
        /// <value>Default is true.</value>
        public required bool EnablePerformanceMetrics { get; set; }
    }
}
