namespace BacklashBot.Management.Interfaces
{
    public interface ICentralPerformanceMonitor : BacklashInterfaces.PerformanceMetrics.IPerformanceMonitor
    {
        // Note: RecordExecutionTime methods are inherited from IPerformanceMonitor
        // Additional methods specific to central performance monitoring:
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
        /// Records MarketDataInitializer performance metrics with enablement status.
        /// </summary>
        /// <param name="totalDuration">Total initialization duration.</param>
        /// <param name="marketCount">Number of markets processed.</param>
        /// <param name="averageMarketTime">Average time per market.</param>
        /// <param name="memoryDelta">Memory usage change in bytes.</param>
        /// <param name="cpuTime">CPU time used.</param>
        /// <param name="successfulMarkets">Number of successfully initialized markets.</param>
        /// <param name="failedMarkets">Number of failed market initializations.</param>
        /// <param name="totalWaitTime">Total time spent waiting.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordMarketDataInitializerMetrics(
            TimeSpan totalDuration,
            int marketCount,
            TimeSpan averageMarketTime,
            long memoryDelta,
            TimeSpan cpuTime,
            int successfulMarkets,
            int failedMarkets,
            TimeSpan totalWaitTime,
            bool metricsEnabled);

        /// <summary>
        /// Records MarketAnalysisHelper performance metrics.
        /// </summary>
        /// <param name="totalMarkets">Total number of markets processed.</param>
        /// <param name="totalTimeMs">Total processing time in milliseconds.</param>
        /// <param name="averageTimeMs">Average time per market in milliseconds.</param>
        /// <param name="errorCount">Number of errors encountered.</param>
        void RecordMarketAnalysisHelperMetrics(int totalMarkets, long totalTimeMs, double averageTimeMs, int errorCount);

        /// <summary>
        /// Records WebSocket message processing performance with enablement status.
        /// </summary>
        /// <param name="messageType">The type of WebSocket message being processed.</param>
        /// <param name="processingTimeTicks">The processing time in ticks.</param>
        /// <param name="messageCount">The number of messages processed.</param>
        /// <param name="bufferSizeBytes">The buffer size used in bytes.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordWebSocketMessageProcessing(string messageType, long processingTimeTicks, int messageCount, long bufferSizeBytes, bool metricsEnabled);

        /// <summary>
        /// Records broadcast service performance metrics with enablement status.
        /// </summary>
        /// <param name="successfulBroadcasts">Number of successful broadcasts.</param>
        /// <param name="failedBroadcasts">Number of failed broadcasts.</param>
        /// <param name="totalBroadcastTimeMs">Total time spent on broadcasts in milliseconds.</param>
        /// <param name="averageBroadcastTimeMs">Average broadcast time in milliseconds.</param>
        /// <param name="broadcastSuccessRate">Success rate percentage.</param>
        /// <param name="totalDataSize">Total size of broadcast data in bytes.</param>
        /// <param name="broadcastsPerMinute">Average broadcasts per minute.</param>
        /// <param name="totalMemoryUsed">Total memory used during broadcasts in bytes.</param>
        /// <param name="averageIntervalDeviationMs">Average interval deviation in milliseconds.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordBroadcastMetrics(
            long successfulBroadcasts,
            long failedBroadcasts,
            double totalBroadcastTimeMs,
            double averageBroadcastTimeMs,
            double broadcastSuccessRate,
            long totalDataSize,
            double broadcastsPerMinute,
            long totalMemoryUsed,
            double averageIntervalDeviationMs,
            bool metricsEnabled);

        /// <summary>
        /// Records database performance metrics from the KalshiBotContext with enablement status.
        /// </summary>
        /// <param name="metrics">Dictionary containing database operation metrics.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordDatabaseMetrics(Dictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> metrics, bool metricsEnabled);

        /// <summary>
        /// Records OverseerClientService performance metrics with enablement status.
        /// </summary>
        /// <param name="metrics">Dictionary containing OverseerClientService performance metrics.</param>
        /// <param name="metricsEnabled">Whether performance metrics are enabled for the calling class.</param>
        void RecordOverseerClientServiceMetrics(Dictionary<string, object> metrics, bool metricsEnabled);

        /// <summary>
        /// Gets the OverseerClientService performance metrics.
        /// </summary>
        IReadOnlyDictionary<string, object>? OverseerClientServiceMetrics { get; }
    }
}
