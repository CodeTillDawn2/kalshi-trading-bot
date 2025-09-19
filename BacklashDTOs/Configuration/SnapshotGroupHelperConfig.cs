using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration
{
    /// <summary>
    /// Configuration class for MarketAnalysisHelper-specific settings.
    /// </summary>
    public class SnapshotGroupHelperConfig
    {
        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for MarketAnalysisHelper operations.
        /// </summary>
        [JsonRequired]
        public bool EnablePerformanceMetrics { get; set; }
    }
}