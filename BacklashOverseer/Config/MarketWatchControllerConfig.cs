using System.Text.Json.Serialization;

namespace BacklashOverseer.Config
{
    public class MarketWatchControllerConfig
    {
        [JsonRequired]
        public bool EnableMarketWatchControllerPerformanceMetrics { get; set; }
        [JsonRequired]
        public int MarketsCacheDurationMinutes { get; set; }
        [JsonRequired]
        public int LogDataCacheDurationMinutes { get; set; }
    }
}