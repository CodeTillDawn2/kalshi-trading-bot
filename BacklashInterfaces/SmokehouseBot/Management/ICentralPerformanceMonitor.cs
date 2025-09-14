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

        /// <summary>
        /// Gets whether WebSocketConnectionManager performance metrics are being recorded.
        /// </summary>
        bool WebSocketConnectionManagerMetricsRecording { get; }

        /// <summary>
        /// Updates the WebSocket metrics recording status.
        /// </summary>
        /// <param name="isRecording">True if WebSocket metrics are being recorded, false otherwise.</param>
        void UpdateWebSocketMetricsRecordingStatus(bool isRecording);
    }
}
