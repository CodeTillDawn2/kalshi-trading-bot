using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashOverseer.Config
{
    public class MarketWatchControllerConfig
    {
        /// <summary>
        /// The configuration section name for MarketWatchControllerConfig.
        /// </summary>
        public const string SectionName = "Endpoints:MarketWatchController";

        [Required(ErrorMessage = "The 'EnableMarketWatchControllerPerformanceMetrics' is missing in the configuration.")]
        public bool EnableMarketWatchControllerPerformanceMetrics { get; set; }

        [Required(ErrorMessage = "The 'MarketsCacheDurationMinutes' is missing in the configuration.")]
        public int MarketsCacheDurationMinutes { get; set; }

        [Required(ErrorMessage = "The 'LogDataCacheDurationMinutes' is missing in the configuration.")]
        public int LogDataCacheDurationMinutes { get; set; }
    }
}
