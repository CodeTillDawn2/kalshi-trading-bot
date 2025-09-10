namespace BacklashBot.Management.Interfaces
{
    public interface ICentralPerformanceMonitor
    {
        void RecordExecutionTime(string methodName, long milliseconds);
        Task StartTimer();
        (double EventQueueAvg, double TickerQueueAvg, double NotificationQueueAvg, double OrderBookQueueAvg) GetQueueCountRollingAverages();
        double CalculateAverageWebsocketEventsReceived(string marketTicker);
        double GetQueueHighCountPercentage();

        double LastRefreshCycleSeconds { get; set; }
        double LastRefreshCycleInterval { get; set; }
        int LastRefreshMarketCount { get; set; }
        double LastRefreshUsagePercentage { get; set; }
        bool LastRefreshTimeAcceptable { get; set; }
        bool IsStartingUp { get; set; }
        bool IsShuttingDown { get; set; }
        DateTime? LastPerformanceSampleDate { get; set; }
        string BrainInstance { get; }
    }
}
