namespace BacklashBot.Management.Interfaces
{
    /// <summary>
    /// Defines the contract for a centralized performance monitoring service that tracks
    /// system performance metrics across various components including queues, WebSocket connections,
    /// database operations, and market analysis activities.
    /// </summary>
    public interface ICentralPerformanceMonitor : BacklashInterfaces.PerformanceMetrics.IPerformanceMonitor
    {
        /// <summary>
        /// Records the execution time for a given operation.
        /// </summary>
        /// <param name="operationName">The name of the operation being timed.</param>
        /// <param name="executionTime">The time taken to execute the operation.</param>
        void RecordExecutionTime(string operationName, TimeSpan executionTime);

        // Additional methods specific to central performance monitoring:
        /// <summary>
        /// Starts the performance monitoring timer.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartTimer();

        /// <summary>
        /// Gets the rolling averages for queue counts across different queue types.
        /// </summary>
        /// <returns>A tuple containing the average counts for event, ticker, notification, and order book queues.</returns>
        (double EventQueueAvg, double TickerQueueAvg, double NotificationQueueAvg, double OrderBookQueueAvg) GetQueueCountRollingAverages();

        /// <summary>
        /// Calculates the average number of WebSocket events received for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to calculate averages for.</param>
        /// <returns>The average number of WebSocket events received.</returns>
        double CalculateAverageWebsocketEventsReceived(string marketTicker);

        /// <summary>
        /// Gets the percentage of time queues have high counts.
        /// </summary>
        /// <returns>The percentage of time queues are at high levels.</returns>
        double GetQueueHighCountPercentage();

        /// <summary>
        /// Gets or sets the duration of the last refresh cycle in seconds.
        /// </summary>
        double LastRefreshCycleSeconds { get; set; }

        /// <summary>
        /// Gets or sets the interval between refresh cycles.
        /// </summary>
        double LastRefreshCycleInterval { get; set; }

        /// <summary>
        /// Gets or sets the number of markets processed in the last refresh cycle.
        /// </summary>
        int LastRefreshMarketCount { get; set; }

        /// <summary>
        /// Gets or sets the usage percentage during the last refresh cycle.
        /// </summary>
        double LastRefreshUsagePercentage { get; set; }

        /// <summary>
        /// Gets or sets whether the last refresh cycle time was acceptable.
        /// </summary>
        bool LastRefreshTimeAcceptable { get; set; }

        /// <summary>
        /// Gets or sets whether the system is currently starting up.
        /// </summary>
        bool IsStartingUp { get; set; }

        /// <summary>
        /// Gets or sets whether the system is currently shutting down.
        /// </summary>
        bool IsShuttingDown { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the last performance sample.
        /// </summary>
        DateTime? LastPerformanceSampleDate { get; set; }

        /// <summary>
        /// Gets the brain instance identifier.
        /// </summary>
        string BrainInstance { get; }

        /// <summary>
        /// Gets a value indicating whether performance metrics for the WebSocket connection manager are currently being recorded.
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

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        double GetCurrentCpuUsage();

        /// <summary>
        /// Gets the average event queue depth.
        /// </summary>
        double GetEventQueueAvg();

        /// <summary>
        /// Gets the average ticker queue depth.
        /// </summary>
        double GetTickerQueueAvg();

        /// <summary>
        /// Gets the average notification queue depth.
        /// </summary>
        double GetNotificationQueueAvg();

        /// <summary>
        /// Gets the average order book queue depth.
        /// </summary>
        double GetOrderbookQueueAvg();

        /// <summary>
        /// Gets the duration of the last refresh cycle in seconds.
        /// </summary>
        double GetLastRefreshCycleSeconds();

        /// <summary>
        /// Gets the interval between the last two refresh cycles.
        /// </summary>
        double GetLastRefreshCycleInterval();

        /// <summary>
        /// Gets the number of markets processed in the last refresh cycle.
        /// </summary>
        double GetLastRefreshMarketCount();

        /// <summary>
        /// Gets the CPU usage percentage during the last refresh cycle.
        /// </summary>
        double GetLastRefreshUsagePercentage();

        /// <summary>
        /// Gets whether the last refresh cycle completed within acceptable time limits.
        /// </summary>
        bool GetLastRefreshTimeAcceptable();

        /// <summary>
        /// Gets the timestamp of the last performance sample.
        /// </summary>
        DateTime? GetLastPerformanceSampleDate();

        /// <summary>
        /// Gets a value indicating whether the WebSocket connection is currently active.
        /// </summary>
        bool IsWebSocketConnected { get; }
    }
}
