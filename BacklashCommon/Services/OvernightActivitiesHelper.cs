using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BacklashDTOs.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using System.Diagnostics;

namespace BacklashCommon.Services
{
    public class PerformanceMetrics
    {
        public long TotalExecutionTimeMs { get; set; }
        public int MarketsProcessed { get; set; }
        public int ApiCallsMade { get; set; }
        public int ErrorsEncountered { get; set; }
        public long PeakMemoryUsageMB { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, long> TaskDurations { get; set; } = new();
    }

    public class OvernightActivitiesHelper : IOvernightActivitiesHelper
    {
        private readonly ILogger<IOvernightActivitiesHelper> _logger;
        private readonly IMarketAnalysisHelper _analysisHelper;
        private readonly ExecutionConfig _executionConfig;
        private readonly ISqlDataService _sqlDataService;
        private readonly PerformanceMetrics _performanceMetrics = new();

        public OvernightActivitiesHelper(ILogger<IOvernightActivitiesHelper> logger, IInterestScoreService interestScoreHelper,
            IMarketAnalysisHelper analysisHelper, IOptions<ExecutionConfig> executionConfig, ISqlDataService sqlDataService)
        {
            _logger = logger;
            _analysisHelper = analysisHelper;
            _executionConfig = executionConfig.Value;
            _sqlDataService = sqlDataService;
        }

        public async Task RunOvernightTasks(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken = default)
        {
            var totalStopwatch = Stopwatch.StartNew();
            _performanceMetrics.StartTime = DateTime.UtcNow;
            LogResourceUsage("OVERNIGHT-Start");

            _logger.LogInformation("OVERNIGHT-Running overnight tasks started at {StartTime}", _performanceMetrics.StartTime);

            cancellationToken.ThrowIfCancellationRequested();

            DateTime cutoff = DateTime.UtcNow;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

                // Track individual task performance
                await TrackTaskPerformance("RefreshOpenMarkets", () => RefreshMarketsByStatus(scopeFactory, KalshiConstants.Status_Open, cancellationToken));
                _logger.LogInformation("OVERNIGHT-Refreshed all open markets.");

                await TrackTaskPerformance("RefreshLikelyClosedMarkets", () => RefreshLikelyClosedMarkets(scopeFactory, cutoff, false, cancellationToken));
                _logger.LogInformation("OVERNIGHT-Refreshed all likely closed markets.");

                await TrackTaskPerformance("CalculateInterestScores", () => CalculateOvernightMarketInterestScores(scopeFactory, cancellationToken));
                _logger.LogInformation("OVERNIGHT-Calculated missing market interest scores.");

                await TrackTaskPerformance("ImportSnapshots", async () =>
                {
                    await _sqlDataService.ExecuteSnapshotImportJobAsync(cancellationToken);
                    await _sqlDataService.ExecuteSnapshotImportJobAsync(cancellationToken); //Do twice to make sure we have as few outstanding snapshots as possible when we generate groups
                });
                _logger.LogInformation("OVERNIGHT-Imported snapshots from files.");

                await TrackTaskPerformance("RemoveOldWatches", () => RemoveOldWatches(scopeFactory));
                _logger.LogInformation("OVERNIGHT-Removed old market watches.");

                await TrackTaskPerformance("GenerateSnapshotGroups", () => _analysisHelper.GenerateSnapshotGroups());
                _logger.LogInformation("OVERNIGHT-Generated snapshot groups.");

                await TrackTaskPerformance("DeleteUnrecordedMarkets", () => DeleteUnrecordedMarkets(scopeFactory, cancellationToken));
                _logger.LogInformation("OVERNIGHT-Deleted ended markets which were never recorded.");

                totalStopwatch.Stop();
                _performanceMetrics.EndTime = DateTime.UtcNow;
                _performanceMetrics.TotalExecutionTimeMs = totalStopwatch.ElapsedMilliseconds;
                LogResourceUsage("OVERNIGHT-End");

                _logger.LogInformation("OVERNIGHT-All tasks completed successfully in {TotalDuration}ms. Performance metrics: {Metrics}",
                    _performanceMetrics.TotalExecutionTimeMs, FormatPerformanceSummary());
            }
            catch (Exception ex)
            {
                totalStopwatch.Stop();
                _performanceMetrics.EndTime = DateTime.UtcNow;
                _performanceMetrics.TotalExecutionTimeMs = totalStopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "OVERNIGHT-Tasks failed after {Duration}ms. Performance metrics: {Metrics}",
                    _performanceMetrics.TotalExecutionTimeMs, FormatPerformanceSummary());
                throw;
            }
        }

        public async Task DeleteUnrecordedMarkets(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            HashSet<string> inactiveMarkets = await context.GetInactiveMarketsWithNoSnapshots();

            foreach (string marketTicker in inactiveMarkets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await context.DeleteMarket(marketTicker);
            }
        }

        public async Task DeleteProcessedSnapshots(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            HashSet<string> inactiveMarkets = await context.GetProcessedMarkets();

            string hardDataStorageLocation = _executionConfig.HardDataStorageLocation;

            foreach (string marketTicker in inactiveMarkets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string filepath = Path.Combine(hardDataStorageLocation, "Candlesticks", marketTicker);
                if (Directory.Exists(filepath))
                    Directory.Delete(filepath);
            }
        }

        private async Task RemoveOldWatches(IServiceScopeFactory scopeFactory)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            await context.RemoveClosedWatches();
        }

        private async Task RefreshLikelyClosedMarkets(IServiceScopeFactory scopeFactory, DateTime cutoff, bool isRetry, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

            List<MarketDTO> MarketsWhichAreLikelyClosed = await context.GetMarkets(
                includedStatuses: null,
                excludedStatuses: new HashSet<string> { KalshiConstants.Status_Finalized,
                                                         KalshiConstants.Status_Inactive,
                                                         KalshiConstants.Status_Initialized,
                                                         KalshiConstants.Status_Bad,
                                                         KalshiConstants.Status_Closed,
                                                         KalshiConstants.Status_Settled },
                maxAPILastFetchTime: cutoff
            );

            _logger.LogInformation("Found {Count} markets to refresh", MarketsWhichAreLikelyClosed.Count);

            // Dynamic batch processing with performance optimization
            await ProcessMarketBatchOptimized(MarketsWhichAreLikelyClosed, scopeFactory, cutoff, isRetry, cancellationToken);
        }

        private async Task ProcessMarketBatchOptimized(List<MarketDTO> markets, IServiceScopeFactory scopeFactory, DateTime cutoff, bool isRetry, CancellationToken cancellationToken)
        {
            const int initialBatchSize = 20;
            const int minBatchSize = 5;
            const int maxBatchSize = 50;
            const int performanceWindowSize = 5;
            const long fastThresholdMs = 2000;
            const long slowThresholdMs = 8000;

            int currentBatchSize = initialBatchSize;
            var recentPerformance = new Queue<long>(performanceWindowSize);
            int totalErrors = 0;
            int totalProcessed = 0;

            using var scope = scopeFactory.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

            for (int i = 0; i < markets.Count; i += currentBatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = markets
                    .Skip(i)
                    .Take(currentBatchSize)
                    .Select(m => m.market_ticker)
                    .ToArray();

                var batchStopwatch = Stopwatch.StartNew();
                (int processed, int error) = await apiService.FetchMarketsAsync(tickers: batch);
                batchStopwatch.Stop();

                totalProcessed += processed;
                totalErrors += error;
                _performanceMetrics.ApiCallsMade++;

                var batchTime = batchStopwatch.ElapsedMilliseconds;
                recentPerformance.Enqueue(batchTime);

                // Maintain performance window
                if (recentPerformance.Count > performanceWindowSize)
                {
                    recentPerformance.Dequeue();
                }

                _logger.LogInformation("Processed batch {BatchNumber}/{TotalBatches} ({CurrentSize} markets) in {Duration}ms. Processed: {Processed}, Errors: {Errors}",
                    (i / currentBatchSize) + 1,
                    (int)Math.Ceiling((double)markets.Count / currentBatchSize),
                    batch.Length,
                    batchTime,
                    processed,
                    error);

                // Adjust batch size based on recent performance
                if (recentPerformance.Count >= 3) // Need some data points
                {
                    var avgTime = recentPerformance.Average();

                    if (avgTime < fastThresholdMs && currentBatchSize < maxBatchSize)
                    {
                        currentBatchSize = Math.Min(currentBatchSize + 5, maxBatchSize);
                        _logger.LogInformation("Increasing batch size to {NewSize} due to fast performance (avg: {AvgTime}ms)",
                            currentBatchSize, avgTime);
                    }
                    else if (avgTime > slowThresholdMs && currentBatchSize > minBatchSize)
                    {
                        currentBatchSize = Math.Max(currentBatchSize - 5, minBatchSize);
                        _logger.LogInformation("Decreasing batch size to {NewSize} due to slow performance (avg: {AvgTime}ms)",
                            currentBatchSize, avgTime);
                    }
                }

                // Rate limiting delay
                await Task.Delay(5000, cancellationToken);
            }

            _logger.LogInformation("Batch processing completed. Total processed: {TotalProcessed}, Total errors: {TotalErrors}, Final batch size: {FinalBatchSize}",
                totalProcessed, totalErrors, currentBatchSize);

            if (totalErrors > 0)
            {
                _performanceMetrics.ErrorsEncountered += totalErrors;
                if (isRetry)
                {
                    _logger.LogWarning("Logged {ErrorCount} errors during overnight market retry.", totalErrors);
                }
                else
                {
                    _logger.LogWarning("Logged {ErrorCount} errors during overnight market refresh. Will attempt again later.", totalErrors);
                    await Task.Delay(1800000, cancellationToken);
                    await RefreshLikelyClosedMarkets(scopeFactory, cutoff, true, cancellationToken);
                }
            }
        }

        private async Task RefreshMarketsByStatus(IServiceScopeFactory scopeFactory, string Status, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
            await apiService.FetchMarketsAsync(status: Status);
        }

        private async Task CalculateOvernightMarketInterestScores(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var interestScoreHelper = scope.ServiceProvider.GetRequiredService<IInterestScoreService>();

            // Track database query performance
            var dbStopwatch = Stopwatch.StartNew();
            var overnightInterestScoresData = await context.GetMarkets(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active },
                maxInterestScoreDate: DateTime.Now.AddHours(-12));
            dbStopwatch.Stop();

            _logger.LogInformation("Database query returned {Count} markets requiring interest score updates in {Duration}ms",
                overnightInterestScoresData.Count, dbStopwatch.ElapsedMilliseconds);

            int processed = 0;
            int successful = 0;
            int failed = 0;
            var processingStopwatch = Stopwatch.StartNew();

            foreach (MarketDTO market in overnightInterestScoresData)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var marketStopwatch = Stopwatch.StartNew();
                try
                {
                    double score = (await interestScoreHelper.CalculateMarketInterestScoreAsync(context, market.market_ticker)).score;
                    marketStopwatch.Stop();

                    MarketWatchDTO? marketWatch = await context.GetMarketWatch(market.market_ticker);

                    if (marketWatch == null)
                    {
                        marketWatch = new MarketWatchDTO() { market_ticker = market.market_ticker, InterestScore = score, InterestScoreDate = DateTime.Now };
                    }
                    else
                    {
                        marketWatch.InterestScore = score;
                        marketWatch.InterestScoreDate = DateTime.Now;
                    }
                    await context.AddOrUpdateMarketWatch(marketWatch);

                    processed++;
                    successful++;
                    _performanceMetrics.MarketsProcessed++;

                    // Log performance for slow markets
                    if (marketStopwatch.ElapsedMilliseconds > 5000)
                    {
                        _logger.LogWarning("Slow interest score calculation for market {MarketTicker}: {Duration}ms",
                            market.market_ticker, marketStopwatch.ElapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    marketStopwatch.Stop();
                    failed++;
                    _performanceMetrics.ErrorsEncountered++;
                    _logger.LogWarning(ex, "Failed to calculate interest score for market {MarketTicker} after {Duration}ms",
                        market.market_ticker, marketStopwatch.ElapsedMilliseconds);
                }

                // Progress logging every 50 markets
                if ((processed + failed) % 50 == 0)
                {
                    _logger.LogInformation("Interest score progress: {Processed}/{Total} markets processed ({SuccessRate:P1} success rate)",
                        processed + failed, overnightInterestScoresData.Count, (double)successful / (processed + failed));
                }
            }

            processingStopwatch.Stop();
            _logger.LogInformation("Interest score calculation completed. Processed: {Processed}, Successful: {Successful}, Failed: {Failed}, Total time: {TotalDuration}ms, Avg per market: {AvgTime}ms",
                processed, successful, failed, processingStopwatch.ElapsedMilliseconds,
                overnightInterestScoresData.Count > 0 ? processingStopwatch.ElapsedMilliseconds / overnightInterestScoresData.Count : 0);
        }

        private async Task TrackTaskPerformance(string taskName, Func<Task> taskFunc)
        {
            var taskStopwatch = Stopwatch.StartNew();
            try
            {
                await taskFunc();
                taskStopwatch.Stop();
                _performanceMetrics.TaskDurations[taskName] = taskStopwatch.ElapsedMilliseconds;
                _logger.LogInformation("Task '{TaskName}' completed in {Duration}ms", taskName, taskStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                taskStopwatch.Stop();
                _performanceMetrics.TaskDurations[taskName] = taskStopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Task '{TaskName}' failed after {Duration}ms", taskName, taskStopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private void LogResourceUsage(string operationName)
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / 1024 / 1024;
                var threadCount = process.Threads.Count;

                _performanceMetrics.PeakMemoryUsageMB = Math.Max(_performanceMetrics.PeakMemoryUsageMB, memoryMB);

                _logger.LogInformation("{Operation} - Memory: {MemoryMB}MB, Threads: {ThreadCount}",
                    operationName, memoryMB, threadCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log resource usage for {Operation}", operationName);
            }
        }

        private string FormatPerformanceSummary()
        {
            var summary = $"Total: {_performanceMetrics.TotalExecutionTimeMs}ms, Memory: {_performanceMetrics.PeakMemoryUsageMB}MB";

            if (_performanceMetrics.TaskDurations.Any())
            {
                var taskSummaries = _performanceMetrics.TaskDurations.Select(kvp =>
                    $"{kvp.Key}: {kvp.Value}ms");
                summary += $", Tasks: [{string.Join(", ", taskSummaries)}]";
            }

            return summary;
        }
    }
}