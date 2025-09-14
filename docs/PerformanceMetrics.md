# Performance Metrics Documentation

This document outlines the performance metrics tracked across various classes in the Kalshi Trading Bot project.

## TradingStrategies.Trading.Overseer.PatternDetectionService

### Methods
- `DetectPatterns()`: TimeSpan - Execution time for pattern detection operations
- `DetectPatternsAsync()`: TimeSpan - Execution time for async pattern detection operations

## TradingStrategies.Trading.Overseer.StrategySimulation

### Properties
- `TotalExecutionTime`: TimeSpan - Aggregate execution time of all ProcessSnapshot calls
- `AverageExecutionTimeMs`: double - Average execution time per snapshot in milliseconds
- `PeakMemoryUsage`: long - Maximum memory usage recorded during simulation
- `TotalTradesExecuted`: int - Total number of trades executed during simulation
- `AverageDecisionTimeMs`: double - Average strategy decision time in milliseconds
- `AverageApplyTimeMs`: double - Average action application time in milliseconds

### Methods
- `GetDetailedPerformanceMetrics()`: Dictionary<string, object> - Comprehensive metrics dictionary with execution details
- `ResetPerformanceMetrics()`: void - Resets all performance metrics for new simulation runs

## TradingStrategies.Trading.Overseer.SimulationEngine

### Properties
- `LastExecutionTime`: TimeSpan - Execution time of the last simulation run
- `LastMemoryUsed`: long - Memory usage difference of the last simulation run

## TradingStrategies.Extensions.MarketSnapshotExtensions

### Methods
- `UpdateOrderbookMetricsFromSimulated()`: TimeSpan - Execution time for order book metrics update

## TradingStrategies.Extensions.PseudoCandlestickExtensions

### Methods
- `ToCandleMids()`: TimeSpan - Execution time for candlestick conversion (logged when >100ms)

## TradingStrategies.Trading.Overseer.EquityCalculator

### Methods
- `GetCalculationTimes()`: long[] - Returns array of all recorded calculation times
- `GetCalculationStatistics()`: (int Count, double AverageMs, long MinMs, long MaxMs) - Returns tuple with calculation statistics
- `ClearCalculationTimes()`: void - Clears all recorded performance metrics

## TradingStrategies.Trading.Helpers.StrategySelectionHelper

### Properties
- `EnablePerformanceMetrics`: bool - Configurable flag for performance metrics collection

### Methods
- `GetPerformanceMetrics()`: object - Retrieves all collected performance metrics
- `ClearPerformanceMetrics()`: void - Clears all collected performance metrics

## TradingStrategies.Trading.Overseer.MarketTypeService

### Methods
- `GetCacheStatistics()`: (long Hits, long Misses) - Returns tuple of cache statistics
- `GetAverageClassificationTime()`: TimeSpan - Returns average classification time
- `GetClassificationCount()`: int - Returns total classifications performed

## KalshiBotOverseer.Services.SnapshotService

### Methods
- `GetAggregationTimes()`: long[] - Returns array of all recorded aggregation times
- `GetAggregationStatistics()`: (int Count, double AverageMs, long MinMs, long MaxMs) - Returns tuple with aggregation statistics
- `GetTotalAggregationTime()`: long - Returns total time in milliseconds
- `GetAggregationCount()`: int - Returns count of operations
- `ClearAggregationMetrics()`: void - Clears all recorded performance metrics

## TradingSimulator.CachedMarketData

### Methods
- `SerializeWithMetrics()`: string - Returns serialized string with out TimeSpan serializationTime parameter
- `DeserializeWithMetrics()`: CachedMarketData - Returns deserialized instance with out TimeSpan deserializationTime parameter

## KalshiBotAPI.Websockets.MessageProcessor

### Properties
- `TotalMessagesProcessed`: long - Total number of messages processed since last metrics reset
- `TotalProcessingTimeMs`: long - Total processing time in milliseconds since last metrics reset
- `LastMetricsLogTime`: DateTime - Timestamp of the last performance metrics log
- `MessagesPerSecond`: double - Current messages per second rate based on recent processing
- `AverageProcessingTimeMs`: double - Average processing time per message in milliseconds
- `OrderBookMessageQueueCount`: int - Current count of order book update messages in processing queue
- `DuplicateMessageCount`: int - Count of duplicate messages detected and skipped
- `LastSequenceNumber`: long - Latest sequence number processed from WebSocket messages
- `MessageProcessingLatency`: TimeSpan - Average time between message receipt and processing completion. Helps identify processing bottlenecks and ensures real-time performance requirements are met for high-frequency trading.
- `MessageDropRate`: double - Percentage of messages dropped due to queue overflow or processing errors. Critical for monitoring system reliability and detecting when message throughput exceeds processing capacity.
- `WebSocketReconnectionCount`: int - Number of WebSocket reconnections performed. Indicates connection stability issues that could impact real-time data flow and trading decisions.
- `PeakQueueDepth`: int - Maximum order book queue depth recorded during operation. Helps dimension queue sizes and identify periods of high message volume that may require infrastructure scaling.

### Methods
- `GetMessageTypeCounts()`: IReadOnlyDictionary<string, long> - Returns count of messages by type processed since startup
- `ResetEventCounts()`: void - Resets all message type counters to zero

## KalshiBotContext.Data.SqlDataService

### Properties
- `TotalProcessed`: long - Total number of operations processed successfully
- `TotalFailed`: long - Total number of operations that failed
- `OrderBookQueueDepth`: int - Current depth of the order book queue
- `TradeQueueDepth`: int - Current depth of the trade queue
- `FillQueueDepth`: int - Current depth of the fill queue
- `EventLifecycleQueueDepth`: int - Current depth of the event lifecycle queue
- `MarketLifecycleQueueDepth`: int - Current depth of the market lifecycle queue
- `SuccessRate`: double - Success rate as a percentage (0-100)
- `TotalQueuedOperations`: int - Total number of queued operations across all queues
- `AverageOperationLatency`: TimeSpan - Average time to complete database operations. Essential for identifying database performance bottlenecks that could slow down the entire trading system.
- `DatabaseConnectionPoolUtilization`: double - Percentage of available database connections in use. Prevents connection pool exhaustion which could halt all database operations during peak trading periods.
- `BatchProcessingEfficiency`: double - Average operations processed per batch. Optimizes batch sizes for maximum throughput while minimizing latency.
- `DeadlockDetectionCount`: int - Number of database deadlocks detected and resolved. Critical for monitoring database concurrency issues that could impact system stability.

## KalshiBotOverseer.Services.BrainPersistenceService

### Properties
- `TotalOperations`: long - Total number of operations performed by this service
- `ServiceUptime`: TimeSpan - Service uptime as a TimeSpan
- `BrainCount`: int - Current number of brain instances being managed
- `TotalHistoryEntries`: long - Total number of metric history entries across all brain instances
- `MemoryGrowthRate`: double - Rate of memory usage increase over time. Detects memory leaks in long-running brain instances that could lead to system instability.
- `OperationThroughput`: double - Operations per second processed by the service. Measures service capacity and helps identify when additional instances are needed for load distribution.
- `CacheHitRate`: double - Percentage of brain lookups served from cache vs database. Optimizes performance by ensuring frequently accessed brain data remains in memory.
- `PersistenceFailureRate`: double - Percentage of persistence operations that fail. Critical for ensuring brain state is reliably saved to prevent data loss during system restarts.

### Methods
- `GetHealthStatus()`: (long TotalMemoryBytes, int BrainCount, long TotalHistoryEntries, TimeSpan ServiceUptime) - Returns comprehensive health metrics

## KalshiBotOverseer.Controllers.MarketWatchController

### Properties
- `AverageEndpointLatency`: TimeSpan - Average response time across all endpoints. Monitors overall API performance and user experience for the monitoring dashboard.
- `CacheHitRate`: double - Percentage of requests served from cache. Measures effectiveness of caching strategy and identifies opportunities for performance optimization.
- `ConcurrentRequestCount`: int - Number of concurrent requests being processed. Helps dimension the system for peak usage periods and prevents resource exhaustion.
- `ErrorRateByEndpoint`: Dictionary<string, double> - Error rates for each API endpoint. Identifies problematic endpoints that may need optimization or bug fixes.

### Notes
- Endpoint execution times logged for GetMarketWatchData, GetBrainLocksData, GetPositionsData, GetOrdersData, GetAccountData, LogEvent, GetSnapshotsData, GetBrainsData

## KalshiBotOverseer.Models.BrainPersistence

### Methods
- `SerializeWithMetrics()`: (string Json, long Milliseconds) - Serializes instance with timing metrics
- `DeserializeWithMetrics()`: (BrainPersistence Instance, long Milliseconds) - Deserializes with timing metrics

### Properties
- `CpuUsageHistory`: List<MetricHistory> - Historical CPU usage metrics
- `EventQueueHistory`: List<MetricHistory> - Historical event queue depth metrics
- `TickerQueueHistory`: List<MetricHistory> - Historical ticker queue depth metrics
- `NotificationQueueHistory`: List<MetricHistory> - Historical notification queue depth metrics
- `OrderbookQueueHistory`: List<MetricHistory> - Historical order book queue depth metrics
- `MarketCountHistory`: List<MetricHistory> - Historical market count metrics
- `ErrorHistory`: List<MetricHistory> - Historical error count metrics
- `RefreshCycleSecondsHistory`: List<MetricHistory> - Historical refresh cycle duration metrics
- `RefreshCycleIntervalHistory`: List<MetricHistory> - Historical refresh cycle interval metrics
- `RefreshMarketCountHistory`: List<MetricHistory> - Historical refresh market count metrics
- `RefreshUsagePercentageHistory`: List<MetricHistory> - Historical refresh CPU usage percentage metrics
- `PerformanceSampleDateHistory`: List<MetricHistory> - Historical performance sample dates
- `LastRefreshTimeAcceptable`: bool - Whether last refresh cycle completed within acceptable time limits
- `HistoryRetentionEfficiency`: double - Percentage of configured history entries actually retained. Ensures metrics history is properly maintained for trend analysis and troubleshooting.
- `SerializationCompressionRatio`: double - Ratio of compressed to uncompressed serialization size. Optimizes storage efficiency for large brain state objects during persistence.
- `MetricUpdateFrequency`: TimeSpan - Average time between metric updates. Ensures metrics are collected at appropriate intervals for real-time monitoring without excessive overhead.
- `StateValidationTime`: TimeSpan - Time required to validate brain state integrity. Critical for ensuring data consistency during state transitions and preventing corrupted brain configurations.

## KalshiBotAPI.Websockets.KalshiWebSocketClient

### Properties
- `EventCounts`: ConcurrentDictionary<string, long> - Current event counts for different message types processed by the subscription manager
- `ConnectSemaphoreCount`: int - Current count of the connection semaphore, indicating connection operation status
- `SubscriptionUpdateSemaphoreCount`: int - Current count of the subscription update semaphore
- `ChannelSubscriptionSemaphoreCount`: int - Current count of the channel subscription semaphore
- `QueuedSubscriptionUpdatesCount`: int - Count of queued subscription update requests waiting to be processed
- `OrderBookMessageQueueCount`: int - Count of order book messages currently in the processing queue
- `PendingConfirmsCount`: int - Count of pending subscription confirmations
- `LastSequenceNumber`: long - Last sequence number processed from WebSocket messages

### Methods
- `GetEventCountsByMarket(string marketTicker)`: (int orderbookEvents, int tradeEvents, int tickerEvents) - Gets event counts by market
- `ResetEventCounts()`: void - Resets event counts

## KalshiBotAPI.Websockets.SubscriptionManager

### Properties
- `EventCounts`: ConcurrentDictionary<string, long> - Dictionary containing counts of different message types processed by the subscription manager
- `SubscriptionUpdateSemaphoreCount`: int - Current count of the subscription update synchronization semaphore
- `ChannelSubscriptionSemaphoreCount`: int - Current count of the channel subscription synchronization semaphore
- `QueuedSubscriptionUpdatesCount`: int - Number of queued subscription update requests waiting to be processed

### Methods
- `GetPerformanceMetrics()`: ConcurrentDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)> - Gets performance metrics for subscription operations
- `ResetEventCounts()`: void - Resets all message type event counts to zero
- `GetEventCountsByMarket(string marketTicker)`: (int orderbookEvents, int tradeEvents, int tickerEvents) - Gets the event counts for different message types for a specific market

## BacklashBot.Services.OverseerClientService

### Properties
- `ConnectionAttemptCount`: int - Total number of connection attempts made to overseer servers
- `ConnectionSuccessCount`: int - Number of successful connections to overseer servers
- `TotalDiscoveryTime`: TimeSpan - Total time spent on overseer discovery operations
- `DiscoveryOperationCount`: int - Number of overseer discovery operations performed
- `CircuitBreakerFailureCount`: int - Current count of circuit breaker failures

### Methods
- `GetMetrics()`: Dictionary<string, object> - Gets the current performance metrics for the overseer client service

## BacklashPatterns.PatternSearch

### Classes
- `PatternDetectionMetrics`: class - Service for collecting performance metrics during pattern detection
- `PatternDetectionMetricsSummary`: class - Summary of pattern detection performance metrics

### Methods
- `GetSummary()`: PatternDetectionMetricsSummary - Gets the performance metrics summary

## KalshiBotAPI.KalshiAPI.KalshiAPIService

### Methods
- `GetMethodExecutionDurations()`: ConcurrentDictionary<string, ConcurrentBag<long>> - Gets the method execution durations for performance monitoring
- `GetCalculationExecutionDurations()`: ConcurrentDictionary<string, ConcurrentBag<long>> - Gets the calculation execution durations for performance monitoring
- `GetMethodPerformanceMetrics()`: Dictionary<string, (int Count, double AverageMs, long MinMs, long MaxMs)> - Gets aggregated performance metrics for method executions
- `GetCalculationPerformanceMetrics()`: Dictionary<string, (int Count, double AverageMs, long MinMs, long MaxMs)> - Gets aggregated performance metrics for calculation executions

## BacklashBot.Management.BrainStatusService

### Methods
- `GetPerformanceMetrics()`: (double InitializationTimeMs, int InitializationAttempts) - Gets initialization timing and attempt count for performance monitoring

## BacklashBot.Management.MarketAnalysisHelper

### Methods
- `GetPerformanceMetrics()`: (int TotalMarketsProcessed, long TotalProcessingTimeMs, double AverageTimePerMarketMs, int ErrorCount) - Gets processing statistics and error counts

## BacklashBot.Services.InterestScoreService

### Methods
- `GetPerformanceMetrics()`: (int CacheHits, int CacheMisses, double AverageOperationTimeMs, int TotalOperations) - Gets cache statistics and operation timing
- `GetCacheHitRate()`: double - Gets the cache hit rate as a percentage

## BacklashBot.Services.OrderBookService

### Methods
- `GetQueueCounts()`: (int EventQueueCount, int TickerQueueCount, int NotificationQueueCount) - Gets current queue depths for monitoring
- `GetEventQueueProcessingMetrics()`: (double AverageProcessingTimeMs, int TotalOperations) - Gets event queue processing statistics
- `GetTickerQueueProcessingMetrics()`: (double AverageProcessingTimeMs, int TotalOperations) - Gets ticker queue processing statistics
- `GetNotificationQueueProcessingMetrics()`: (double AverageProcessingTimeMs, int TotalOperations) - Gets notification queue processing statistics
- `GetMarketLockWaitMetrics(string marketTicker)`: (double AverageWaitTimeMs, int TotalOperations) - Gets market-specific lock wait times

## BacklashPatterns.PatternUtils

### Methods
- `GetPerformanceMetrics()`: (int TotalCalculations, int CacheHits, int CacheMisses, double AverageCalculationTimeMs) - Gets calculation and cache statistics
- `GetCacheHitRate()`: double - Gets the cache hit rate as a percentage

## BacklashBot.Services.KaslhiBotScopeManagerService

### Notes
- No performance metrics currently tracked

## BacklashBot.Services.MarketRefreshService

### Properties
- `LastWorkDuration`: TimeSpan - Duration of the last market refresh operation
- `LastWorkMarketCount`: int - Number of markets processed in the last refresh operation
- `TotalRefreshOperations`: long - Total number of refresh operations performed
- `AverageRefreshTimePerMarket`: TimeSpan - Average time spent per market refresh in the last operation
- `LastRefreshCount`: int - Total number of markets refreshed in the last operation

## BacklashBot.Services.MarketDataInitializer

### Properties
- `LastInitializationDuration`: TimeSpan - Duration of the last market data initialization operation
- `LastInitializationMarketCount`: int - Number of markets processed during the last initialization

## BacklashBot.Services.BroadcastService

### Properties
- `SuccessfulBroadcasts`: long - Total number of successful broadcast operations
- `FailedBroadcasts`: long - Total number of failed broadcast operations
- `TotalBroadcastTimeMs`: double - Cumulative total time spent on broadcast operations in milliseconds
- `AverageBroadcastTimeMs`: double - Average broadcast time per operation in milliseconds
- `BroadcastSuccessRate`: double - Broadcast success rate as a percentage (0-100)

## TradingStrategies.TradingOverseer

### Notes
- No explicit public performance metrics properties currently tracked

## TradingSimulator.Simulator.SimulatorReporting

### Methods
- `GetPerformanceMetrics()`: PerformanceMetrics - Returns comprehensive metrics object with timing and counts

### Properties
- `TotalAnalysisTime`: TimeSpan - Total time spent analyzing velocity discrepancies
- `RollingObservationsTime`: TimeSpan - Time spent computing rolling observations
- `ExpectedFlowsTime`: TimeSpan - Time spent computing expected flows
- `SpikeSuppressionTime`: TimeSpan - Time spent on spike suppression logic
- `SnapshotsProcessed`: int - Number of market snapshots processed
- `DiscrepanciesDetected`: int - Number of velocity discrepancies detected

## TradingSimulator.MarketProcessor

### Methods
- `GetPerformanceMetrics()`: PerformanceMetrics - Returns comprehensive metrics object
- `ResetPerformanceMetrics()`: void - Resets all performance counters
- `GetMetricsSummary()`: string - Formatted summary of all metrics

### Properties
- `AverageProcessingTimePerMarket`: TimeSpan - Average time to process a single market
- `AverageBatchProcessingTime`: TimeSpan - Average time to process a batch of markets
- `MarketsPerSecond`: double - Processing rate in markets per second
- `SnapshotsPerSecond`: double - Processing rate in snapshots per second
- `CurrentQueueDepth`: int - Current number of markets in processing queue
- `MaxQueueDepth`: int - Maximum queue depth recorded
- `TotalMarketsProcessed`: int - Total number of markets processed
- `TotalSnapshotsProcessed`: int - Total number of snapshots processed
- `TotalProcessingTime`: TimeSpan - Total time spent processing
- `Uptime`: TimeSpan - Time since metrics were started
- `PeakMemoryUsage`: long - Peak memory usage recorded
- `AverageMemoryUsage`: double - Average memory usage
- `CurrentMemoryUsage`: long - Current memory usage

## KalshiBotOverseer.OverseerHub

### Methods
- `GetHubMetrics()`: dynamic - Returns comprehensive hub metrics object

### Properties
- `Uptime`: TimeSpan - Time since hub started
- `TotalConnections`: long - Total number of client connections
- `ActiveConnections`: long - Currently active connections
- `TotalMessagesProcessed`: long - Total messages processed
- `MessagesPerSecond`: double - Current message processing rate
- `ConnectionHealthCount`: int - Number of healthy connections
- `MessageBatchQueueSize`: int - Size of message batch queue
- `HandshakeRateLimitCount`: int - Number of rate-limited handshakes
- `CheckInRateLimitCount`: int - Number of rate-limited check-ins

## KalshiBotOverseer.OvernightActivitiesHelper

### Methods
- `GetPerformanceMetrics()`: tuple - Returns comprehensive performance metrics (newly added)

### Properties
- `TotalTasks`: int - Total number of overnight tasks executed
- `SuccessfulTasks`: int - Number of tasks that completed successfully
- `SuccessRate`: double - Percentage of tasks that succeeded
- `TotalDuration`: TimeSpan - Total time for all overnight operations
- `TaskTimings`: Dictionary<string, TimeSpan> - Individual task execution times
- `MarketRefreshFailureCount`: int - Number of market refresh failures
- `LastMarketRefreshFailure`: DateTime? - Timestamp of last market refresh failure

## KalshiBotAPI.Websockets.WebSocketConnectionManager

### Properties
- `ConnectionAttempts`: int - Total connection attempts made
- `ConnectionSuccesses`: int - Number of successful connections
- `ConnectionSuccessRate`: double - Percentage of successful connections
- `ReconnectionCount`: int - Number of reconnections performed
- `AverageConnectionLatency`: double - Average connection time in milliseconds
- `MessagesReceived`: int - Total messages received
- `MessageThroughput`: double - Messages per second (sliding window)
- `ConnectionFailures`: int - Total connection failures
- `SignatureCacheHitRate`: double - Percentage of signature cache hits
- `SignatureCacheHits`: int - Number of signature cache hits
- `SignatureCacheMisses`: int - Number of signature cache misses
- `TotalBytesReceived`: long - Total bytes received
- `AverageBufferUtilization`: double - Average buffer usage percentage

### Methods
- `ConnectionFailureReasons`: IReadOnlyDictionary<string, int> - Failure reasons and counts
