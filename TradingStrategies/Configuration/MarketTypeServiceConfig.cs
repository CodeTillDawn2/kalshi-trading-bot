using System;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration options for the MarketTypeService.
    /// </summary>
    public class MarketTypeServiceConfig
    {
        /// <summary>
        /// The expiration time for cached market type results in minutes.
        /// </summary>
        required
        public int CacheExpirationMinutes { get; set; }

        /// <summary>
        /// Whether to enable performance metrics collection for market type classification.
        /// </summary>
        required
        public bool EnablePerformanceMetrics { get; set; }
    }
}
