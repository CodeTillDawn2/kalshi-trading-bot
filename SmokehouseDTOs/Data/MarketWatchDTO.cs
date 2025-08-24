namespace SmokehouseDTOs.Data
{
    public class MarketWatchDTO
    {
        public string market_ticker { get; set; }
        public Guid? BrainLock { get; set; }
        public double? InterestScore { get; set; }
        public DateTime? InterestScoreDate { get; set; }
        public DateTime? LastWatched { get; set; }
        public double? AverageWebsocketEventsPerMinute { get; set; }
    }


}
