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
        /// Gets a formatted performance summary string.
        /// </summary>
        /// <returns>Formatted performance summary.</returns>
        string GetPerformanceSummary();
    }
}
