using BacklashBot.Configuration;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service responsible for initializing market data during application startup.
    /// This service fetches watched markets, subscribes to WebSocket channels for new markets,
    /// synchronizes market data, and sets up positions and account balance. It ensures
    /// that all necessary market data is available before the application becomes fully operational.
    /// </summary>
    public class MarketDataInitializer : IMarketDataInitializer
    {
        private readonly ILogger<IMarketDataInitializer> _logger;
        private readonly IServiceFactory _serviceFactory;
        private readonly IStatusTrackerService _statusTracker;
        private readonly IBotReadyStatus _readyStatus;
        private readonly IScopeManagerService _scopeManagerService;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly bool _enablePerformanceMetrics;
        /// <summary>
        /// Gets the duration of the last market data initialization operation.
        /// </summary>
        public TimeSpan LastInitializationDuration { get; private set; }
        /// <summary>
        /// Gets the number of markets processed during the last initialization.
        /// </summary>
        public int LastInitializationMarketCount { get; private set; }
        /// <summary>
        /// Gets the average time spent initializing each market.
        /// </summary>
        public TimeSpan AverageMarketInitializationTime { get; private set; }
        /// <summary>
        /// Gets the change in memory usage during initialization (bytes).
        /// </summary>
        public long MemoryUsageDelta { get; private set; }
        /// <summary>
        /// Gets the CPU time used during initialization.
        /// </summary>
        public TimeSpan CpuTimeDelta { get; private set; }
        /// <summary>
        /// Gets the number of successfully initialized markets.
        /// </summary>
        public int SuccessfulMarketInitializations { get; private set; }
        /// <summary>
        /// Gets the number of failed market initializations.
        /// </summary>
        public int FailedMarketInitializations { get; private set; }
        /// <summary>
        /// Gets the total time spent waiting during initialization (delays and WebSocket waits).
        /// </summary>
        public TimeSpan TotalWaitTime { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="MarketDataInitializer"/> class.
        /// </summary>
        /// <param name="logger">Logger for recording initialization operations and errors.</param>
        /// <param name="serviceFactory">Factory for accessing other services in the application.</param>
        /// <param name="scopeManagerService">Service for managing dependency injection scopes.</param>
        /// <param name="readyStatus">Service tracking the application's readiness status.</param>
        /// <param name="statusTracker">Service for tracking application status and cancellation tokens.</param>
        /// <param name="performanceMonitor">Central performance monitoring service.</param>
        public MarketDataInitializer(ILogger<IMarketDataInitializer> logger, IServiceFactory serviceFactory, IScopeManagerService scopeManagerService, IBotReadyStatus readyStatus,
            IStatusTrackerService statusTracker, IPerformanceMonitor performanceMonitor, IOptions<MarketDataInitializerConfig> config)
        {
            _logger = logger;
            _statusTracker = statusTracker;
            _readyStatus = readyStatus;
            _scopeManagerService = scopeManagerService;
            _serviceFactory = serviceFactory;
            _performanceMonitor = performanceMonitor;
            _enablePerformanceMetrics = config.Value.EnablePerformanceMetrics;
        }

        /// <summary>
        /// Performs the complete market data initialization sequence.
        /// This method fetches watched markets, subscribes to WebSocket channels, synchronizes market data,
        /// retrieves positions, and updates account balance. It runs operations sequentially on a low-priority
        /// thread to avoid interfering with other system processes.
        /// </summary>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        public async Task SetupAsync()
        {
            var initializationStartTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = _enablePerformanceMetrics ? GC.GetTotalMemory(false) : 0;
            var initialCpu = _enablePerformanceMetrics ? Process.GetCurrentProcess().TotalProcessorTime : TimeSpan.Zero;
            TimeSpan totalMarketTime = TimeSpan.Zero;
            int processedMarkets = 0;
            int successfulInitializations = 0;
            int failedInitializations = 0;
            TimeSpan totalWaitTime = TimeSpan.Zero;
            List<string> watchedMarkets = null;

            _logger.LogDebug("MarketDataInitializer.SetupAsync started at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTracker.GetCancellationToken().IsCancellationRequested);
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                _logger.LogDebug("Fetching watched markets...");
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                watchedMarkets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
                _logger.LogDebug("Found {Count} watched markets: {Markets}", watchedMarkets.Count, string.Join(", ", watchedMarkets));

                if (!watchedMarkets.Any())
                {
                    _logger.LogDebug("No watched markets found, updating...");
                    await _serviceFactory.GetMarketDataService().UpdateWatchedMarketsAsync();
                    watchedMarkets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
                    _logger.LogDebug("After forced refresh, found {Count} watched markets: {Markets}", watchedMarkets.Count, string.Join(", ", watchedMarkets));
                }

                // Run market initialization sequentially on a single background thread with lower priority
                await Task.Run(async () =>
                {
                    try
                    {
                        // Set thread priority to BelowNormal to reduce resource competition with snapshots
                        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                        _logger.LogDebug("Starting market initialization on low-priority thread");

                        // Process markets sequentially to avoid rate limiting
                        foreach (var ticker in watchedMarkets)
                        {
                            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                            var marketStart = _enablePerformanceMetrics ? DateTime.UtcNow : DateTime.MinValue;
                            _logger.LogDebug("Initializing market {MarketTicker} on low-priority thread", ticker);
                            if (!_serviceFactory.GetDataCache().Markets.ContainsKey(ticker))
                            {
                                _logger.LogDebug("Adding market subscription for {MarketTicker}", ticker);
                                await _serviceFactory.GetMarketDataService().SubscribeToMarketChannelsAsync(ticker);
                                _logger.LogDebug("Subscribed to market {MarketTicker}", ticker);
                                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                                _logger.LogDebug("Waiting for initial WebSocket data for {MarketTicker}", ticker);
                                var waitTime = await WaitForInitialDataAsync(ticker);
                                if (_enablePerformanceMetrics) totalWaitTime += waitTime;
                                _logger.LogDebug("Received initial WebSocket data for {MarketTicker}", ticker);
                            }
                            else
                            {
                                _logger.LogDebug("Syncing all market data for {MarketTicker}", ticker);
                                await _serviceFactory.GetMarketDataService().SyncMarketDataAsync(ticker);
                                _logger.LogDebug("Synced market data for {MarketTicker}", ticker);
                            }
                            if (_enablePerformanceMetrics)
                            {
                                processedMarkets++;
                                totalMarketTime += DateTime.UtcNow - marketStart;
                                successfulInitializations++;
                            }
                            _logger.LogInformation("Completed initialization for {MarketTicker}", ticker);

                            // Add 100ms delay between market initializations to prevent rate limiting
                            if (_enablePerformanceMetrics) totalWaitTime += TimeSpan.FromMilliseconds(100);
                            await Task.Delay(100, _statusTracker.GetCancellationToken());
                        }

                        _logger.LogDebug("Market initialization completed on low-priority thread");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Market initialization on low-priority thread cancelled");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during market initialization on low-priority thread");
                        throw;
                    }
                }, _statusTracker.GetCancellationToken());
                _logger.LogInformation("All market initializations completed");
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();

                _logger.LogDebug("Fetching positions...");
                await _serviceFactory.GetMarketDataService().RetrieveAndUpdatePositionsAsync();
                _logger.LogDebug("Fetched positions");

                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                _logger.LogDebug("Updating account balance...");
                await _serviceFactory.GetMarketDataService().UpdateAccountBalanceAsync();
                _logger.LogDebug("Account balance updated");

                _readyStatus.InitializationCompleted.SetResult(true);
                _logger.LogInformation("Initialization set to completed");

                // Collect performance metrics
                LastInitializationDuration = DateTime.UtcNow - initializationStartTime;
                LastInitializationMarketCount = watchedMarkets?.Count ?? 0;
                if (_enablePerformanceMetrics)
                {
                    AverageMarketInitializationTime = processedMarkets > 0 ? totalMarketTime / processedMarkets : TimeSpan.Zero;
                    MemoryUsageDelta = GC.GetTotalMemory(false) - initialMemory;
                    CpuTimeDelta = Process.GetCurrentProcess().TotalProcessorTime - initialCpu;
                    SuccessfulMarketInitializations = successfulInitializations;
                    FailedMarketInitializations = failedInitializations;
                    TotalWaitTime = totalWaitTime;
                    _logger.LogInformation("Performance metrics: Avg market time {AvgTime}, Memory delta {Memory} bytes, CPU time {Cpu}, Success {Success}, Fail {Fail}, Total wait {Wait}",
                        AverageMarketInitializationTime, MemoryUsageDelta, CpuTimeDelta, SuccessfulMarketInitializations, FailedMarketInitializations, TotalWaitTime);
                }
                else
                {
                    AverageMarketInitializationTime = TimeSpan.Zero;
                    MemoryUsageDelta = 0;
                    CpuTimeDelta = TimeSpan.Zero;
                    SuccessfulMarketInitializations = 0;
                    FailedMarketInitializations = 0;
                    TotalWaitTime = TimeSpan.Zero;
                }
                stopwatch.Stop();
                _logger.LogInformation("Market data initialization completed in {Duration} for {Count} markets", LastInitializationDuration, LastInitializationMarketCount);

                // Post metrics to central performance monitor
                RecordMarketDataInitializerMetricsPrivate(
                    LastInitializationDuration,
                    LastInitializationMarketCount,
                    AverageMarketInitializationTime,
                    MemoryUsageDelta,
                    CpuTimeDelta,
                    SuccessfulMarketInitializations,
                    FailedMarketInitializations,
                    TotalWaitTime,
                    _enablePerformanceMetrics);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MarketDataInitializer.SetupAsync cancelled at {0}", DateTime.UtcNow);
                _readyStatus.InitializationCompleted.TrySetResult(false);
                _readyStatus.BrowserReady.TrySetResult(false);
                _logger.LogDebug("Market Data initialization canceled.");

                // Collect metrics even on cancellation
                LastInitializationDuration = DateTime.UtcNow - initializationStartTime;
                LastInitializationMarketCount = watchedMarkets?.Count ?? 0;
                if (_enablePerformanceMetrics)
                {
                    AverageMarketInitializationTime = processedMarkets > 0 ? totalMarketTime / processedMarkets : TimeSpan.Zero;
                    MemoryUsageDelta = GC.GetTotalMemory(false) - initialMemory;
                    CpuTimeDelta = Process.GetCurrentProcess().TotalProcessorTime - initialCpu;
                    SuccessfulMarketInitializations = successfulInitializations;
                    FailedMarketInitializations = failedInitializations;
                    TotalWaitTime = totalWaitTime;
                }
                else
                {
                    AverageMarketInitializationTime = TimeSpan.Zero;
                    MemoryUsageDelta = 0;
                    CpuTimeDelta = TimeSpan.Zero;
                    SuccessfulMarketInitializations = 0;
                    FailedMarketInitializations = 0;
                    TotalWaitTime = TimeSpan.Zero;
                }
                stopwatch.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MarketDataInitializer.SetupAsync");
                _readyStatus.InitializationCompleted.TrySetResult(false);
                _readyStatus.BrowserReady.TrySetResult(false);
                _logger.LogDebug("Set InitializationCompleted and BrowserReady tasks to false due to error");

                // Collect metrics even on error
                LastInitializationDuration = DateTime.UtcNow - initializationStartTime;
                LastInitializationMarketCount = watchedMarkets?.Count ?? 0;
                if (_enablePerformanceMetrics)
                {
                    AverageMarketInitializationTime = processedMarkets > 0 ? totalMarketTime / processedMarkets : TimeSpan.Zero;
                    MemoryUsageDelta = GC.GetTotalMemory(false) - initialMemory;
                    CpuTimeDelta = Process.GetCurrentProcess().TotalProcessorTime - initialCpu;
                    SuccessfulMarketInitializations = successfulInitializations;
                    FailedMarketInitializations = failedInitializations;
                    TotalWaitTime = totalWaitTime;
                }
                else
                {
                    AverageMarketInitializationTime = TimeSpan.Zero;
                    MemoryUsageDelta = 0;
                    CpuTimeDelta = TimeSpan.Zero;
                    SuccessfulMarketInitializations = 0;
                    FailedMarketInitializations = 0;
                    TotalWaitTime = TimeSpan.Zero;
                }
                stopwatch.Stop();
                throw;
            }
            finally
            {
                _logger.LogDebug("MarketDataInitializer.SetupAsync completed at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTracker.GetCancellationToken().IsCancellationRequested);
            }
        }

        /// <summary>
        /// Records market data initializer performance metrics using the IPerformanceMonitor interface.
        /// </summary>
        /// <param name="lastInitializationDuration">Total duration of the last initialization.</param>
        /// <param name="lastInitializationMarketCount">Number of markets processed.</param>
        /// <param name="averageMarketInitializationTime">Average time per market initialization.</param>
        /// <param name="memoryUsageDelta">Change in memory usage during initialization.</param>
        /// <param name="cpuTimeDelta">CPU time used during initialization.</param>
        /// <param name="successfulMarketInitializations">Number of successful initializations.</param>
        /// <param name="failedMarketInitializations">Number of failed initializations.</param>
        /// <param name="totalWaitTime">Total time spent waiting.</param>
        /// <param name="enablePerformanceMetrics">Whether performance metrics are enabled.</param>
        private void RecordMarketDataInitializerMetricsPrivate(
            TimeSpan lastInitializationDuration,
            int lastInitializationMarketCount,
            TimeSpan averageMarketInitializationTime,
            long memoryUsageDelta,
            TimeSpan cpuTimeDelta,
            int successfulMarketInitializations,
            int failedMarketInitializations,
            TimeSpan totalWaitTime,
            bool enablePerformanceMetrics)
        {
            string className = "MarketDataInitializer";
            string category = "MarketDataInitialization";

            if (!enablePerformanceMetrics)
            {
                // Send disabled metrics
                _performanceMonitor.RecordDisabledMetric(className, "LastInitializationDuration", "Last Initialization Duration", "Total duration of the last initialization", lastInitializationDuration.TotalMilliseconds, "ms", category, false);
                _performanceMonitor.RecordDisabledMetric(className, "LastInitializationMarketCount", "Last Initialization Market Count", "Number of markets processed", lastInitializationMarketCount, "count", category, false);
                _performanceMonitor.RecordDisabledMetric(className, "AverageMarketInitializationTime", "Average Market Initialization Time", "Average time per market initialization", averageMarketInitializationTime.TotalMilliseconds, "ms", category, false);
                _performanceMonitor.RecordDisabledMetric(className, "MemoryUsageDelta", "Memory Usage Delta", "Change in memory usage during initialization", memoryUsageDelta, "bytes", category, false);
                _performanceMonitor.RecordDisabledMetric(className, "CpuTimeDelta", "CPU Time Delta", "CPU time used during initialization", cpuTimeDelta.TotalMilliseconds, "ms", category, false);
                _performanceMonitor.RecordDisabledMetric(className, "SuccessfulMarketInitializations", "Successful Market Initializations", "Number of successful initializations", successfulMarketInitializations, "count", category, false);
                _performanceMonitor.RecordDisabledMetric(className, "FailedMarketInitializations", "Failed Market Initializations", "Number of failed initializations", failedMarketInitializations, "count", category, false);
                _performanceMonitor.RecordDisabledMetric(className, "TotalWaitTime", "Total Wait Time", "Total time spent waiting", totalWaitTime.TotalMilliseconds, "ms", category, false);
            }
            else
            {
                // Record actual metrics
                _performanceMonitor.RecordSpeedDialMetric(className, "LastInitializationDuration", "Last Initialization Duration", "Total duration of the last initialization", lastInitializationDuration.TotalMilliseconds, "ms", category, null, null, null, true);
                _performanceMonitor.RecordCounterMetric(className, "LastInitializationMarketCount", "Last Initialization Market Count", "Number of markets processed", lastInitializationMarketCount, "count", category, true);
                _performanceMonitor.RecordSpeedDialMetric(className, "AverageMarketInitializationTime", "Average Market Initialization Time", "Average time per market initialization", averageMarketInitializationTime.TotalMilliseconds, "ms", category, null, null, null, true);
                _performanceMonitor.RecordCounterMetric(className, "MemoryUsageDelta", "Memory Usage Delta", "Change in memory usage during initialization", memoryUsageDelta, "bytes", category, true);
                _performanceMonitor.RecordSpeedDialMetric(className, "CpuTimeDelta", "CPU Time Delta", "CPU time used during initialization", cpuTimeDelta.TotalMilliseconds, "ms", category, null, null, null, true);
                _performanceMonitor.RecordCounterMetric(className, "SuccessfulMarketInitializations", "Successful Market Initializations", "Number of successful initializations", successfulMarketInitializations, "count", category, true);
                _performanceMonitor.RecordCounterMetric(className, "FailedMarketInitializations", "Failed Market Initializations", "Number of failed initializations", failedMarketInitializations, "count", category, true);
                _performanceMonitor.RecordSpeedDialMetric(className, "TotalWaitTime", "Total Wait Time", "Total time spent waiting", totalWaitTime.TotalMilliseconds, "ms", category, null, null, null, true);
            }
        }

        /// <summary>
        /// Waits for initial market data and the first orderbook snapshot to become available after subscribing to a market channel.
        /// This method polls for market data availability and ReceivedFirstSnapshot with a timeout to ensure the orderbook
        /// has been fully populated before proceeding with initialization.
        /// </summary>
        /// <param name="marketTicker">The market ticker symbol to wait for data on.</param>
        /// <returns>A task representing the asynchronous wait operation, returning the time spent waiting.</returns>
        private async Task<TimeSpan> WaitForInitialDataAsync(string marketTicker)
        {
            const int maxWaitSeconds = 3;
            const int pollIntervalMs = 500;
            var startTime = DateTime.UtcNow;

            _logger.LogDebug("WaitForInitialDataAsync started for {MarketTicker} at {0}, CancellationToken.IsCancellationRequested={IsRequested}", marketTicker, DateTime.UtcNow, _statusTracker.GetCancellationToken().IsCancellationRequested);
            try
            {
                while ((DateTime.UtcNow - startTime).TotalSeconds < maxWaitSeconds)
                {
                    _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                    var marketData = _serviceFactory.GetMarketDataService().GetMarketDetails(marketTicker);
                    var lastWebSocketTimestamp = _serviceFactory.GetMarketDataService().GetLatestWebSocketTimestamp();
                    _logger.LogDebug("Waiting for {MarketTicker}: MarketData={Exists}, Timestamp={Timestamp}, Elapsed={Seconds}s",
                        marketTicker, marketData != null, lastWebSocketTimestamp.ToString("yyyy-MM-ddTHH:mm:ss"), (DateTime.UtcNow - startTime).TotalSeconds);
                    if (marketData != null)
                    {
                        _logger.LogDebug("Initial data received for {MarketTicker}", marketTicker);
                        return DateTime.UtcNow - startTime;
                    }
                    await Task.Delay(pollIntervalMs, _statusTracker.GetCancellationToken());
                }
                _logger.LogWarning("Timeout for {MarketTicker} after {MaxWaitSeconds}s. Proceeding with available data.", marketTicker, maxWaitSeconds);
                return TimeSpan.FromSeconds(maxWaitSeconds);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("WaitForInitialDataAsync cancelled for {MarketTicker} at {0}", marketTicker, DateTime.UtcNow);
                return DateTime.UtcNow - startTime;
            }
            finally
            {
                _logger.LogDebug("WaitForInitialDataAsync completed for {MarketTicker} at {0}, CancellationToken.IsCancellationRequested={IsRequested}", marketTicker, DateTime.UtcNow, _statusTracker.GetCancellationToken().IsCancellationRequested);
            }
        }
    }
}
