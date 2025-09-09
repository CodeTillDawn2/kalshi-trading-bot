namespace BacklashBot.Management.Interfaces
{
    public interface ICentralPerformanceMonitor
    {
        void RecordExecutionTime(string methodName, long milliseconds);
        Task StartTimer();
        (double EventQueueAvg, double TickerQueueAvg, double NotificationQueueAvg, double OrderBookQueueAvg) GetQueueCountRollingAverages();
        double CalculateAverageWebsocketEventsReceived(string marketTicker);
        double GetQueueHighCountPercentage();

        double LastWorkDuration { get; set; }
        double LastRefreshInterval { get; set; }
        int LastMarketCount { get; set; }
        double LastUsagePercentage { get; set; }
        double LastExecutionSeconds { get; set; }
        bool LastExecutionAcceptable { get; set; }
        DateTime? LastPerformanceSampleDate { get; set; }
    }
}
