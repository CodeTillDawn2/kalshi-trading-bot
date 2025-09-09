namespace BacklashDTOs
{
    public class PerformanceMetrics
    {

        public PerformanceMetrics()
        {
            EventQueueAvg = 0;
            TickerQueueAvg = 0;
            NotificationQueueAvg = 0;
            OrderbookQueueAvg = 0;
            CurrentUsage = 0;
            CurrentCount = 0;
        }

        public double EventQueueAvg { get; set; }
        public double TickerQueueAvg { get; set; }
        public double NotificationQueueAvg { get; set; }
        public double OrderbookQueueAvg { get; set; }
        public double CurrentUsage { get; set; }
        public int CurrentCount { get; set; }

    }
}
