namespace BacklashBot.Management.Interfaces
{
/// <summary>ICentralPerformanceMonitor</summary>
/// <summary>ICentralPerformanceMonitor</summary>
    public interface ICentralPerformanceMonitor
/// <summary>StartTimer</summary>
/// <summary>RecordExecutionTime</summary>
    {
/// <summary>GetQueueHighCountPercentage</summary>
        void RecordExecutionTime(string methodName, long milliseconds);
/// <summary>Gets or sets the LastRefreshCycleSeconds.</summary>
/// <summary>CalculateAverageWebsocketEventsReceived</summary>
        Task StartTimer();
/// <summary>Gets or sets the LastRefreshUsagePercentage.</summary>
        (double EventQueueAvg, double TickerQueueAvg, double NotificationQueueAvg, double OrderBookQueueAvg) GetQueueCountRollingAverages();
/// <summary>Gets or sets the IsStartingUp.</summary>
/// <summary>Gets or sets the LastRefreshCycleSeconds.</summary>
        double CalculateAverageWebsocketEventsReceived(string marketTicker);
/// <summary>Gets or sets the BrainInstance.</summary>
/// <summary>Gets or sets the LastRefreshMarketCount.</summary>
        double GetQueueHighCountPercentage();
/// <summary>Gets or sets the LastRefreshTimeAcceptable.</summary>

/// <summary>Gets or sets the IsShuttingDown.</summary>
        double LastRefreshCycleSeconds { get; set; }
/// <summary>Gets or sets the BrainInstance.</summary>
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

        /// <summary>
        /// Records OverseerClientService performance metrics.
        /// </summary>
        /// <param name="metrics">Dictionary containing OverseerClientService performance metrics.</param>
        void RecordOverseerClientServiceMetrics(Dictionary<string, object> metrics);

        /// <summary>
        /// Gets the OverseerClientService performance metrics.
        /// </summary>
        IReadOnlyDictionary<string, object>? OverseerClientServiceMetrics { get; }
    }
}
