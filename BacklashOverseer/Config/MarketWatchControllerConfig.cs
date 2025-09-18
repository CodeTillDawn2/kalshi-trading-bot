namespace BacklashOverseer.Config
{
    public class MarketWatchControllerConfig
    {
        public bool EnableMarketWatchControllerPerformanceMetrics { get; set; } = true;
        public int MarketsCacheDurationMinutes { get; set; } = 15;
        public int LogDataCacheDurationMinutes { get; set; } = 5;
    }
}