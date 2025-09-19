using System.Text.Json.Serialization;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for MarketDataInitializer settings.
    /// </summary>
    public class MarketDataInitializerConfig
    {
        /// <summary>
        /// Gets or sets whether to enable performance metrics collection for MarketDataInitializer operations.
        /// </summary>
        /// <value>Default is false.</value>
        [JsonRequired]
        public bool EnablePerformanceMetrics { get; set; }
    }
}