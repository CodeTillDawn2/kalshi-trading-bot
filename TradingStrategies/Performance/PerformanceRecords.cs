namespace TradingStrategies.Trading.Overseer
{
    /// <summary>
    /// Represents a single performance record.
    /// </summary>
    public record PerformanceRecord(
        DateTime Timestamp,
        long TotalExecutionTimeMs,
        int TotalItemsProcessed,
        int TotalItemsFound,
        Dictionary<string, long> ItemCheckTimes,
        bool? MetricsEnabled = null
    );

    /// <summary>
    /// Comprehensive performance statistics for operations.
    /// </summary>
    public record PerformanceStats(
        int RecordCount = 0,
        double AverageExecutionTimeMs = 0.0,
        long MinExecutionTimeMs = 0,
        long MaxExecutionTimeMs = 0,
        double AverageItemsProcessed = 0.0,
        long TotalItemsProcessed = 0,
        double AverageItemsFound = 0.0,
        long TotalItemsFound = 0,
        Dictionary<string, (int Count, double AverageMs, long MinMs, long MaxMs)> ItemCheckStats = null
    )
    {
        public PerformanceStats() : this(0, 0.0, 0, 0, 0.0, 0, 0.0, 0, new Dictionary<string, (int, double, long, long)>()) { }
    }

    /// <summary>
    /// Typed performance metrics for trading strategy simulations.
    /// Exposes all metrics from StrategySimulation.GetDetailedPerformanceMetrics() in a strongly-typed format.
    /// </summary>
    public record StrategySimulationPerformanceMetrics(
        TimeSpan TotalExecutionTime,
        double AverageExecutionTimeMs,
        long PeakMemoryUsage,
        int TotalSnapshotsProcessed,
        double PerformanceThresholdMs,
        double MemoryThresholdMB,
        int SlowOperationsCount,
        int HighMemoryOperationsCount,
        int RestingOrdersCount,
        int CurrentPosition,
        double CurrentCash,
        int TotalTradesExecuted,
        double AverageDecisionTimeMs,
        double AverageApplyTimeMs,
        int SlowDecisionsCount,
        double DecisionThresholdMs,
        double BandWidthRatioThreshold,
        int TradeRateLimitPerSnapshot
    );

    /// <summary>
    /// Typed performance metrics for PatternUtils operations.
    /// Provides strongly-typed access to PatternUtils performance data including
    /// calculation counts, cache statistics, throughput, and configuration status.
    /// </summary>
    public record PatternUtilsPerformanceMetrics(
        int TotalCalculations,
        long TotalCalculationTimeMs,
        int CacheHits,
        int CacheMisses,
        double CacheHitRate,
        double AverageCalculationTimeMs,
        double? Throughput,
        double? CpuTimeMs,
        long? MemoryUsage,
        Dictionary<string, bool>? ConfigurationStatus
    );
}
