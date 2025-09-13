/// <summary>
/// Provides helper methods for executing overnight maintenance and data processing tasks
/// in the Kalshi trading bot overseer system. This class orchestrates various background
/// operations including market data refresh, interest score calculations, snapshot imports,
/// cleanup of old data, and generation of snapshot groups for analysis.
/// </summary>
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BacklashDTOs.Configuration;
using BacklashBot.Services.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace KalshiBotOverseer
{
    /// <summary>
    /// Service class that handles overnight maintenance tasks for the Kalshi trading bot overseer.
    /// This includes refreshing market data, calculating interest scores, importing snapshots,
    /// cleaning up old watches, and generating snapshot groups for market analysis.
    /// </summary>
    public class OvernightActivitiesHelper : IOvernightActivitiesHelper
    {
        private readonly ILogger<OvernightActivitiesHelper> _logger;
        private readonly IMarketAnalysisHelper _analysisHelper;
        private readonly ExecutionConfig _executionConfig;
        private readonly ISqlDataService _sqlDataService;

        // Performance metrics
        private readonly Stopwatch _overnightStopwatch = new();
        private int _totalTasks;
        private int _successfulTasks;
        private readonly Dictionary<string, TimeSpan> _taskTimings = new();

        // Circuit breaker for market refresh
        private int _marketRefreshFailureCount;
        private DateTime? _lastMarketRefreshFailure;

        /// <summary>
        /// Initializes a new instance of the <see cref="OvernightActivitiesHelper"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <param name="interestScoreHelper">The interest score service (injected but not used in constructor).</param>
        /// <param name="analysisHelper">The market analysis helper for generating snapshot groups.</param>
        /// <param name="executionConfig">The execution configuration options.</param>
        /// <param name="sqlDataService">The SQL data service for snapshot operations.</param>
        public OvernightActivitiesHelper(ILogger<OvernightActivitiesHelper> logger, IInterestScoreService interestScoreHelper,
            IMarketAnalysisHelper analysisHelper, IOptions<ExecutionConfig> executionConfig, ISqlDataService sqlDataService)
        {
            _logger = logger;
            _analysisHelper = analysisHelper;
            _executionConfig = executionConfig.Value;
            _sqlDataService = sqlDataService;
        }

        /// <summary>
        /// Checks if the market refresh circuit breaker is open.
        /// </summary>
        /// <returns>True if the circuit is open and operations should be skipped.</returns>
        private bool IsMarketRefreshCircuitOpen()
        {
            if (_marketRefreshFailureCount >= _executionConfig.MarketRefreshCircuitBreakerFailureThreshold)
            {
                if (_lastMarketRefreshFailure.HasValue &&
                    (DateTime.UtcNow - _lastMarketRefreshFailure.Value).TotalMinutes < _executionConfig.MarketRefreshCircuitBreakerTimeoutMinutes)
                {
                    return true;
                }
                else
                {
                    _marketRefreshFailureCount = 0; // Reset after timeout
                }
            }
            return false;
        }

        /// <summary>
        /// Executes the complete set of overnight maintenance tasks for the trading bot overseer.
        /// This includes market data refresh, interest score calculations, snapshot imports,
        /// cleanup operations, and snapshot group generation.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scoped service instances.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RunOvernightTasks(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running overnight tasks.");
            _overnightStopwatch.Restart();
            _totalTasks = 0;
            _successfulTasks = 0;
            _taskTimings.Clear();

            cancellationToken.ThrowIfCancellationRequested();

            DateTime cutoff = DateTime.UtcNow;

            // Validate cutoff datetime
            if (cutoff <= DateTime.MinValue || cutoff > DateTime.UtcNow.AddDays(1))
            {
                _logger.LogError("Invalid cutoff datetime: {Cutoff}", cutoff);
                throw new ArgumentException("Invalid cutoff datetime", nameof(cutoff));
            }

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

            // Refresh open markets
            var sw = Stopwatch.StartNew();
            await RefreshMarketsByStatus(scopeFactory, KalshiConstants.Status_Open, cancellationToken);
            sw.Stop();
            _taskTimings["RefreshOpenMarkets"] = sw.Elapsed;
            _totalTasks++;
            _successfulTasks++;
            _logger.LogInformation("Refreshed all open markets. Duration: {Duration}", sw.Elapsed);

            // Refresh likely closed markets
            sw.Restart();
            await RefreshLikelyClosedMarkets(scopeFactory, cutoff, false, cancellationToken);
            sw.Stop();
            _taskTimings["RefreshLikelyClosedMarkets"] = sw.Elapsed;
            _totalTasks++;
            _successfulTasks++;
            _logger.LogInformation("Refreshed all likely closed markets. Duration: {Duration}", sw.Elapsed);

            // Calculate interest scores
            sw.Restart();
            await CalculateOvernightMarketInterestScores(scopeFactory, cancellationToken);
            sw.Stop();
            _taskTimings["CalculateInterestScores"] = sw.Elapsed;
            _totalTasks++;
            _successfulTasks++;
            _logger.LogInformation("Calculated missing market interest scores. Duration: {Duration}", sw.Elapsed);

            // Import snapshots
            sw.Restart();
            await _sqlDataService.ExecuteSnapshotImportJobAsync(cancellationToken);
            await _sqlDataService.ExecuteSnapshotImportJobAsync(cancellationToken); // Execute twice to ensure minimal outstanding snapshots before generating groups
            sw.Stop();
            _taskTimings["ImportSnapshots"] = sw.Elapsed;
            _totalTasks++;
            _successfulTasks++;
            _logger.LogInformation("Imported snapshots from files. Duration: {Duration}", sw.Elapsed);

            // Remove old watches
            sw.Restart();
            await RemoveOldWatches(scopeFactory);
            sw.Stop();
            _taskTimings["RemoveOldWatches"] = sw.Elapsed;
            _totalTasks++;
            _successfulTasks++;
            _logger.LogInformation("Removed old market watches. Duration: {Duration}", sw.Elapsed);

            // Generate snapshot groups
            sw.Restart();
            await _analysisHelper.GenerateSnapshotGroups();
            sw.Stop();
            _taskTimings["GenerateSnapshotGroups"] = sw.Elapsed;
            _totalTasks++;
            _successfulTasks++;
            _logger.LogInformation("Generated snapshot groups. Duration: {Duration}", sw.Elapsed);

            // Delete unrecorded markets
            sw.Restart();
            await DeleteUnrecordedMarkets(scopeFactory, cancellationToken);
            sw.Stop();
            _taskTimings["DeleteUnrecordedMarkets"] = sw.Elapsed;
            _totalTasks++;
            _successfulTasks++;
            _logger.LogInformation("Deleted ended markets which were never recorded. Duration: {Duration}", sw.Elapsed);

            // Log performance metrics
            _overnightStopwatch.Stop();
            _logger.LogInformation("Overnight tasks completed. Total duration: {TotalDuration}, Tasks: {TotalTasks}, Successful: {SuccessfulTasks}, Success rate: {SuccessRate:P}",
                _overnightStopwatch.Elapsed, _totalTasks, _successfulTasks, _successfulTasks / (double)_totalTasks);
            foreach (var timing in _taskTimings)
            {
                _logger.LogInformation("Task {TaskName}: {Duration}", timing.Key, timing.Value);
            }
        }

        /// <summary>
        /// Deletes markets that have ended but were never recorded with snapshots.
        /// This cleanup operation removes inactive markets that don't have any snapshot data.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scoped service instances.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteUnrecordedMarkets(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            HashSet<string> inactiveMarkets = await context.GetInactiveMarketTickersWithoutSnapshots();

            foreach (string marketTicker in inactiveMarkets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await context.DeleteMarket(marketTicker);
            }
        }

        /// <summary>
        /// Removes market watches for markets that have closed.
        /// This cleanup operation ensures that the system doesn't maintain watches for inactive markets.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scoped service instances.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RemoveOldWatches(IServiceScopeFactory scopeFactory)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            await context.RemoveClosedWatches();
        }

        /// <summary>
        /// Refreshes market data for markets that are likely closed but haven't been marked as such yet.
        /// This operation fetches updated market information in batches to ensure data accuracy.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scoped service instances.</param>
        /// <param name="cutoff">The cutoff datetime for determining which markets to refresh.</param>
        /// <param name="isRetry">Indicates whether this is a retry attempt after previous errors.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RefreshLikelyClosedMarkets(IServiceScopeFactory scopeFactory, DateTime cutoff, bool isRetry, CancellationToken cancellationToken)
        {
            // Check circuit breaker
            if (IsMarketRefreshCircuitOpen())
            {
                _logger.LogWarning("Market refresh circuit breaker is open. Skipping market refresh.");
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

            List<MarketDTO> MarketsWhichAreLikelyClosed = await context.GetMarketsFiltered(
                includedStatuses: null,
                excludedStatuses: new HashSet<string> { KalshiConstants.Status_Finalized,
                                                          KalshiConstants.Status_Inactive,
                                                          KalshiConstants.Status_Initialized,
                                                          KalshiConstants.Status_Bad,
                                                          KalshiConstants.Status_Closed,
                                                          KalshiConstants.Status_Settled },
                maxAPILastFetchTime: cutoff
            );

            int batchSize = _executionConfig.OvernightBatchSize;
            int totalBatches = (int)Math.Ceiling(MarketsWhichAreLikelyClosed.Count / (double)batchSize);
            int errors = 0;
            for (int i = 0; i < MarketsWhichAreLikelyClosed.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int currentBatch = i / batchSize + 1;
                _logger.LogInformation("Processing batch {CurrentBatch} of {TotalBatches} for likely closed markets.", currentBatch, totalBatches);
                var batch = MarketsWhichAreLikelyClosed
                    .Skip(i)
                    .Take(batchSize)
                    .Select(m => m.market_ticker)
                    .ToArray();
                (int processed, int error) = await apiService.FetchMarketsAsync(tickers: batch);
                await Task.Delay(5000, cancellationToken);
                errors += error;
            }
            if (errors > 0)
            {
                _marketRefreshFailureCount++;
                _lastMarketRefreshFailure = DateTime.UtcNow;
                if (isRetry)
                {
                    _logger.LogWarning("Logged {0} errors during overnight market retry.", errors);
                }
                else
                {
                    _logger.LogWarning("Logged {0} errors during overnight market refresh. Will attempt again later.", errors);
                    await Task.Delay(_executionConfig.OvernightRetryDelayMinutes * 60 * 1000, cancellationToken);
                    await RefreshLikelyClosedMarkets(scopeFactory, cutoff, true, cancellationToken);
                }
            }
            else
            {
                // Reset on success
                _marketRefreshFailureCount = 0;
                _lastMarketRefreshFailure = null;
            }
        }

        /// <summary>
        /// Refreshes market data for all markets with a specific status.
        /// This operation fetches updated market information from the API for markets in the specified state.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scoped service instances.</param>
        /// <param name="Status">The market status to filter by for refresh operations.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RefreshMarketsByStatus(IServiceScopeFactory scopeFactory, string Status, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
            await apiService.FetchMarketsAsync(status: Status);
        }

        /// <summary>
        /// Deletes processed snapshots that are no longer needed for analysis.
        /// This method removes snapshot data that has been fully processed and is no longer required
        /// for the system's ongoing operations, helping to maintain efficient storage usage.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating database contexts.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteProcessedSnapshots(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            // Implementation would go here - currently a placeholder
            // This method should identify and remove snapshots that have been fully processed
            // and are no longer needed for analysis or backtesting

            _logger.LogInformation("OVERNIGHT-DeleteProcessedSnapshots completed (placeholder implementation).");
        }

        /// <summary>
        /// Calculates interest scores for active markets that haven't been scored recently.
        /// This operation updates market watch data with fresh interest scores for decision making.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scoped service instances.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CalculateOvernightMarketInterestScores(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var interestScoreHelper = scope.ServiceProvider.GetRequiredService<IInterestScoreService>();
            var overnightInterestScoresData = await context.GetMarketsFiltered(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active },
                maxInterestScoreDate: DateTime.UtcNow.AddHours(-_executionConfig.InterestScoreAgeThresholdHours));

            int batchSize = _executionConfig.OvernightBatchSize;
            int totalBatches = (int)Math.Ceiling(overnightInterestScoresData.Count / (double)batchSize);
            int processed = 0;
            int successful = 0;

            for (int i = 0; i < overnightInterestScoresData.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int currentBatch = i / batchSize + 1;
                _logger.LogInformation("Processing batch {CurrentBatch} of {TotalBatches} for interest score calculations.", currentBatch, totalBatches);

                var batch = overnightInterestScoresData.Skip(i).Take(batchSize).ToList();
                var tasks = batch.Select(async market =>
                {
                    try
                    {
                        double score = (await interestScoreHelper.CalculateMarketInterestScoreAsync(context, market.market_ticker)).score;

                        MarketWatchDTO? marketWatch = await context.GetMarketWatch(market.market_ticker);

                        if (marketWatch == null)
                        {
                            marketWatch = new MarketWatchDTO() { market_ticker = market.market_ticker, InterestScore = score, InterestScoreDate = DateTime.UtcNow };
                        }
                        else
                        {
                            marketWatch.InterestScore = score;
                            marketWatch.InterestScoreDate = DateTime.UtcNow;
                        }
                        await context.AddOrUpdateMarketWatch(marketWatch);
                        return true;
                    }
                    catch (Exception)
                    {
                        _logger.LogWarning("Failed to calculate interest score for market {MarketTicker}.", market.market_ticker);
                        return false;
                    }
                });

                var results = await Task.WhenAll(tasks);
                processed += batch.Count;
                successful += results.Count(r => r);
            }

            _logger.LogInformation("Interest score calculation completed. Processed: {Processed}, Successful: {Successful}", processed, successful);
        }
    }
}