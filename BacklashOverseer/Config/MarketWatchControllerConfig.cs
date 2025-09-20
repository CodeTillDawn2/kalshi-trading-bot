using System.Text.Json.Serialization;

namespace BacklashOverseer.Config
{
    public class MarketWatchControllerConfig
    {
        required
        public bool EnableMarketWatchControllerPerformanceMetrics { get; set; }
        required
        public int MarketsCacheDurationMinutes { get; set; }
        required
        public int LogDataCacheDurationMinutes { get; set; }
    }
}
