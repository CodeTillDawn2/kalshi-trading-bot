namespace BacklashInterfaces.PerformanceMetrics
{
    /// <summary>
    /// Interface for accessing performance metrics from OvernightActivitiesHelper.
    /// This interface provides a standardized way to access overnight task performance data.
    /// </summary>
    public interface INightActivitiesPerformanceMetrics
    {
        /// <summary>
        /// Gets the current overnight activities performance metrics.
        /// </summary>
        /// <returns>Tuple containing comprehensive performance data.</returns>
        (long TotalExecutionTimeMs, int MarketsProcessed, int ApiCallsMade, int ErrorsEncountered,
         long PeakMemoryUsageMB, DateTime StartTime, DateTime EndTime,
         Dictionary<string, long> TaskDurations) GetOvernightPerformanceMetrics();

        /// <summary>
        /// Records an overnight task execution with performance data.
        /// </summary>
        /// <param name="taskName">The name of the task.</param>
        /// <param name="duration">The execution duration in milliseconds.</param>
        /// <param name="success">Whether the task was successful.</param>
        void RecordOvernightTask(string taskName, long duration, bool success);

        /// <summary>
        /// Records an API call made during overnight processing.
        /// </summary>
        void RecordApiCall();

        /// <summary>
        /// Records an error that occurred during overnight processing.
        /// </summary>
        void RecordError();

        /// <summary>
        /// Records the number of markets processed.
        /// </summary>
        /// <param name="count">The number of markets processed.</param>
        void RecordMarketsProcessed(int count);

        /// <summary>
        /// Records memory usage during overnight processing.
        /// </summary>
        /// <param name="memoryMB">Current memory usage in MB.</param>
        void RecordMemoryUsage(long memoryMB);

        /// <summary>
        /// Gets a formatted performance summary string.
        /// </summary>
        /// <returns>Formatted performance summary.</returns>
        string GetPerformanceSummary();
    }
}
