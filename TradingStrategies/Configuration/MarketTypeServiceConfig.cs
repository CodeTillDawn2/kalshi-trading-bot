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
        public int CacheExpirationMinutes { get; set; } = 5;

        /// <summary>
        /// Whether to enable performance metrics collection for market type classification.
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = true;
    }
}