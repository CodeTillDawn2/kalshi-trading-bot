using BacklashInterfaces.PerformanceMetrics;

namespace BacklashBot.Management.Interfaces
{
    /// <summary>
    /// Defines the contract for a centralized performance monitoring service that tracks
    /// system performance metrics across various components including queues, WebSocket connections,
    /// database operations, and market analysis activities.
    /// </summary>
    public interface ICentralPerformanceMonitor : IPerformanceMonitor
    {

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
