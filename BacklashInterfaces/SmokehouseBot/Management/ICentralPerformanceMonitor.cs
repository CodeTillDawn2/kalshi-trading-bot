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

        /// <summary>
        /// Records MarketDataInitializer performance metrics.
        /// </summary>
        /// <param name="totalDuration">Total initialization duration.</param>
        /// <param name="marketCount">Number of markets processed.</param>
        /// <param name="averageMarketTime">Average time per market.</param>
        /// <param name="memoryDelta">Memory usage change in bytes.</param>
        /// <param name="cpuTime">CPU time used.</param>
        /// <param name="successfulMarkets">Number of successfully initialized markets.</param>
        /// <param name="failedMarkets">Number of failed market initializations.</param>
        /// <param name="totalWaitTime">Total time spent waiting.</param>
        void RecordMarketDataInitializerMetrics(
            TimeSpan totalDuration,
            int marketCount,
            TimeSpan averageMarketTime,
            long memoryDelta,
            TimeSpan cpuTime,
            int successfulMarkets,
            int failedMarkets,
            TimeSpan totalWaitTime);

        /// <summary>
        /// Records MarketAnalysisHelper performance metrics.
        /// </summary>
        /// <param name="totalMarkets">Total number of markets processed.</param>
        /// <param name="totalTimeMs">Total processing time in milliseconds.</param>
        /// <param name="averageTimeMs">Average time per market in milliseconds.</param>
        /// <param name="errorCount">Number of errors encountered.</param>
        void RecordMarketAnalysisHelperMetrics(int totalMarkets, long totalTimeMs, double averageTimeMs, int errorCount);
    }
}
